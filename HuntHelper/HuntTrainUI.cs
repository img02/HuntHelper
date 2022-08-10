using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;
using HuntHelper.Managers.Hunts;
using HuntHelper.Managers.Hunts.Models;
using HuntHelper.Utilities;
using ImGuiNET;
using ImGuiScene;
using Lumina.Excel.GeneratedSheets;

namespace HuntHelper;

public class HuntTrainUI : IDisposable
{
    private readonly HuntManager _huntManager;
    private readonly Configuration _config;


    private Vector2 _huntTrainWindowSize = new Vector2(250, 400);
    private Vector2 _huntTrainWindowPos = new Vector2(150, 150);

    private bool _huntTrainWindowVisible = false;
    public bool HuntTrainWindowVisible
    {
        get => _huntTrainWindowVisible;
        set => _huntTrainWindowVisible = value;
    }

    private readonly List<HuntTrainMob> _mobList;

    public HuntTrainUI(HuntManager huntManager, Configuration config)
    {
        _huntManager = huntManager;
        _mobList = _huntManager.HuntTrain;
        _config = config;
        LoadSettings();
    }

    public void Draw()
    {
        DrawHuntTrainWindow();
    }

    public void Dispose()
    {
        SaveSettings();
    }

    public void LoadSettings()
    {
        //HuntTrainWindowVisible = _config.HuntTrainWindowVisible;
        _huntTrainWindowSize = _config.HuntTrainWindowSize;
        _huntTrainWindowPos = _config.HuntTrainWindowPos;
    }

    public void SaveSettings()
    {
        //_config.HuntTrainWindowVisible = HuntTrainWindowVisible;
        _config.HuntTrainWindowSize = _huntTrainWindowSize;
        _config.HuntTrainWindowPos = _huntTrainWindowPos;
    }

    private Vector4 bg = new Vector4(.3f, .3f, .3f, 1f);
    public void DrawHuntTrainWindow()
    {
        if (!HuntTrainWindowVisible) return;

        var numOfColumns = 2;
        if (_showPos) numOfColumns++;
        if (_showLastSeen) numOfColumns++;

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
        ImGui.SetNextWindowSize(_huntTrainWindowSize, ImGuiCond.FirstUseEver);
        ImGui.SetWindowPos(_huntTrainWindowPos);
        if (ImGui.Begin("Hunt Train##Window", ref _huntTrainWindowVisible, ImGuiWindowFlags.NoScrollbar))
        {
            //var childSize = new Vector2(ImGui.GetWindowSize().X / numOfColumns, _mobList.Count * 23);
            var childSizeX = ImGui.GetWindowSize().X / numOfColumns;
            var childSizeY = _mobList.Count * 23;
            var lastSeenWidth = childSizeX; //math is hard, resizing is hard owie :(
            var posWidth = childSizeX;
            if (_showPos && _showLastSeen)
            {
                lastSeenWidth = childSizeX * .75f;
                posWidth = childSizeX * 1.25f;
            }

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));

            #region hunt train main

            ImGui.BeginChild("HUNT TRAIN MAIN CHILD WINDOW ALL ENCOMPASSING LAYOUT FIXING CHILD OF WINDOW",
                new Vector2(ImGui.GetWindowSize().X, (_mobList.Count + 1) * 23f), _useBorder); //23f is the size of a selectable from SelectableFromList();

            #region Headers

            ImGui.SameLine();
            ImGui.BeginChild("NameHeader", new Vector2(childSizeX * 1.75f, 20), _useBorder,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
            ImGui.Text("Name");
            ImGui.EndChild();
            if (_showPos)
            {
                ImGui.SameLine();
                ImGui.BeginChild("PositionHeader", new Vector2(posWidth, 20),
                    _useBorder, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
                ImGui.Text("Position");
                ImGui.EndChild();
            }

            if (_showLastSeen)
            {
                ImGui.SameLine();
                ImGui.BeginChild("LastSeenHeader", new Vector2(lastSeenWidth, 20),
                    _useBorder, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
                ImGui.Text("Last Seen");
                ImGui.EndChild();
            }

            ImGui.SameLine();
            PluginUI.ImGui_HelpMarker("HOW TO:\n\n" +
                                      "Click on any of the rows to send the relevant Map Link into chat. (Drag to reorder the list)\n\n" +
                                      "Click on the checkbox to mark a mob as dead.\n\n" +
                                      "Right-click -- Select - change the selected mob\n" +
                                      "            -- Remove - Removes the mob from the list (This permanently deletes data on that mob)\n\n" +
                                      "Use the command \"/hhn\" to automatically mark the current selected as dead, and send the next Map link into chat.\n" +
                                      "(you will have to click the first one manually)\n\n" +
                                      "*Does not allow duplicates (currently does not handle instances or different worlds)\n" +
                                      "*Drag reordering can glitch a bit if you drag from near the top of an item. ");
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(1f, .3f, .3f, 1f), "  <<<");

            #endregion

            ImGui.Separator();

            ImGui.BeginChild("Name", new Vector2(childSizeX * 1.75f, childSizeY), _useBorder,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
            SelectableFromList(HuntTrainMobAttribute.Name);
            ImGui.EndChild();

            if (_showPos)
            {
                ImGui.SameLine();
                ImGui.BeginChild("Position", new Vector2(posWidth, childSizeY), _useBorder,
                    ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
                SelectableFromList(HuntTrainMobAttribute.Position);
                ImGui.EndChild();
            }

            if (_showLastSeen)
            {
                ImGui.SameLine();
                ImGui.BeginChild("Last seen", new Vector2(lastSeenWidth, childSizeY), _useBorder,
                    ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
                SelectableFromList(HuntTrainMobAttribute.LastSeen);
                ImGui.EndChild();
            }

            ImGui.SameLine();
            ImGui.BeginChild("DeadButtons", new Vector2(childSizeX * 0.25f, childSizeY), _useBorder,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
            _mobList.ForEach(m =>
            {
                var d = m.Dead;
                ImGui.Checkbox($"##DeadCheckBox{m.Name}", ref d);
                ImGui.Separator();
                m.Dead = d;
                if (m.Dead && _mobList.IndexOf(m) == _selectedIndex) SelectNext();//infinite if all dead
            });
            ImGui.EndChild();

            ImGui.Separator();

            ImGui.EndChild(); //main hunt train data section
            #endregion

            ImGui.Text("BOTTOM" + $" {_selectedIndex}");

            ImGui.Checkbox("pos", ref _showPos);
            ImGui.SameLine(); ImGui.Checkbox("last seen", ref _showLastSeen);
            ImGui.SameLine(); ImGui.Checkbox("border", ref _useBorder);

            ImGui.Button("Remove Dead Hunts");
            ImGui.Button("Unkill All Hunts");

            if (ImGui.Button("Export"))
            {
                //get export code
                var exportCode = ExportImport.Export(_mobList);
                //copy to clipboard
                ImGui.SetClipboardText(exportCode);
                ChangeCopyText();
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text(_copyText);
                ImGui.EndTooltip();
            }

            if (ImGui.Button("Import"))
            {
                var importCode = ImGui.GetClipboardText();
                ExportImport.Import(importCode, _importedTrain);

                ImGui.OpenPopup("Import##popup");

                //show 2 buttons, overwrite old data, only import new data.
            }

            DrawImportWindow();

            ImGui.PopStyleVar(); //pop itemspacing
            ImGui.End();
        }
        ImGui.PopStyleVar();//pop window padding
    }

    private string _copyText = "Copy export code clipboard.";
    private Task _copyTextTask = Task.CompletedTask;
    private int _tooltipChangeTime = 400;
    private bool _showImportWindow = false;

    private int _selectedIndex = 0;
    #region user customisable - config
    private bool _showPos = true;
    private bool _showLastSeen = true;
    private bool _useBorder = false;
    #endregion
    private Vector4 _deadTextColour = new Vector4(.6f, .7f, .6f, 1f);
    private readonly List<HuntTrainMob> _importedTrain = new List<HuntTrainMob>();

    private bool _importAll = false;
    private bool _importNew = true;
    private bool _importUpdateTime = true;
    private void DrawImportWindow()
    {
        var center = ImGui.GetWindowPos() + ImGui.GetWindowSize() / 2;
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new Vector2(400, 700));

        if (ImGui.BeginPopupModal("Import##popup"))
        {

            //if count is zero -> show message

            ImGui.Text($"Imported data for {_importedTrain.Count} mobs.");

            if (ImGui.BeginChild("tablechild", new Vector2(ImGui.GetWindowSize().X, (ImGui.GetTextLineHeightWithSpacing() + 5) * (_importedTrain.Count + 1))))
            {

                if (ImGui.BeginTable("ImportTable", 2, ImGuiTableFlags.BordersH))
                {
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"Name");
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"Last Seen");
                    foreach (var m in _importedTrain)
                    {   //green text if new mob, red text if exists.
                        ImGui.PushStyleColor(ImGuiCol.Text,
                            _mobList.All(mob => mob.Name != m.Name)
                                ? new Vector4(0.1647f, 1f, 0.647f, 1f) //greenish
                                : new Vector4(1f, 0.345f, 0.345f, 1f)); //redish
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"{m.Name}");
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"{(DateTime.Now.ToUniversalTime() - m.LastSeenUTC).TotalMinutes:0}m");
                        ImGui.PopStyleColor();
                    }
                    ImGui.EndTable();
                }
                ImGui.EndChild();
            }

            if (ImGui.Checkbox("Import All ", ref _importAll))
            {
                _importNew = false;
                _importUpdateTime = false;
            }
            ImGui.SameLine(); PluginUI.ImGui_HelpMarker("Overwrites current data with imported data");
            ImGui.Dummy(new Vector2(0, 6f));
            ImGui.Separator();
            ImGui.Dummy(new Vector2(0, 6f));

            if (ImGui.Checkbox("Import New ", ref _importNew))
            {
                _importAll = false;
                if (!_importNew) _importUpdateTime = false;
            }
            ImGui.SameLine(); PluginUI.ImGui_HelpMarker("Only imports new mobs");


            ImGui.SameLine(); ImGui.Dummy(new Vector2(16, 0));
            ImGui.SameLine();

            if (ImGui.Checkbox("Update Last Seen Time ", ref _importUpdateTime))
            {
                _importNew = true;
                _importAll = false;
            };
            ImGui.SameLine(); PluginUI.ImGui_HelpMarker("Imports new mobs and updates old mobs Last Seen times, if applicable");



            ImGui.Dummy(new Vector2(0, 100));


            ImGui.Dummy(new Vector2(6, 0));
            ImGui.SameLine();
            if (ImGui.Button("Import", new Vector2(80, 0)))
            {
                if (!_importNew && !_importUpdateTime && !_importAll) return;
                ImportTrainData();
                _importedTrain.Clear();
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine(); ImGui.Dummy(new Vector2(6, 0));
            ImGui.SameLine();
            if (ImGui.Button("Cancel", new Vector2(80, 0)))
            {
                _importedTrain.Clear();
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }


    }

    private void ImportTrainData()
    {
        if (_importAll)
        {
            _mobList.Clear();
            _mobList.AddRange(_importedTrain);
            return;
        }

        var tempList = new List<HuntTrainMob>();

        foreach (var m in _importedTrain)
        {
            if (_mobList.All(mob => mob.Name != m.Name)) _mobList.Add(m);

            if (_importUpdateTime)
            {   //inefficient?
                var toUpdate = _mobList.FirstOrDefault(mob => mob.Name == m.Name);
                if (toUpdate == null) continue;
                if (m.LastSeenUTC > toUpdate.LastSeenUTC) toUpdate.LastSeenUTC = m.LastSeenUTC;
            }
        }
    }

    //changes tooltip temporarily when clicked. ^^)b
    private void ChangeCopyText()
    {
        if (!_copyTextTask.IsCompleted) return;
        _copyTextTask = Task.Run(() =>
        {
            var temp = _copyText;
            _copyText = "Copied!";
            Thread.Sleep(_tooltipChangeTime);
            _copyText = temp;
        });
    }

    //called from command, sets current mob as dead, selects next, sends flag.
    public void GetNextMobCommand()
    {
        if (_selectedIndex < _mobList.Count) _mobList[_selectedIndex].Dead = true;
        SelectNext();
        if (!_mobList[_selectedIndex].Dead) _huntManager.SendTrainFlag(_selectedIndex);
        else _huntManager.SendTrainFlag(-1);
    }

    //based off of https://github.com/ocornut/imgui/blob/docking/imgui_demo.cpp#L2337
    private void SelectableFromList(HuntTrainMobAttribute attributeToDisplay)
    {
        HuntTrainMob? toRemove = null;
        for (int n = 0; n < _mobList.Count; n++)
        {
            var mob = _mobList[n];
            var label = GetAttributeFromMob(attributeToDisplay, mob);

            if (mob.Dead) ImGui.PushStyleColor(ImGuiCol.Text, _deadTextColour);
            else ImGui.PushStyleColor(ImGuiCol.Text, Vector4.One); //white

            if (n == _selectedIndex) ImGui.Selectable($"{label}", true, ImGuiSelectableFlags.None, new Vector2(ImGui.GetContentRegionAvail().X, 23f));
            else ImGui.Selectable($"{label}", false, ImGuiSelectableFlags.None, new Vector2(ImGui.GetContentRegionAvail().X, 23f));


            /*if (ImGui.IsItemActive() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)) //useless
            {
                mob.Dead = !mob.Dead;
            }*/
            //how to check if mouse held?
            if (ImGui.IsItemActive() && ImGui.IsMouseClicked(ImGuiMouseButton.Left)) _huntManager.SendTrainFlag(n);

            if (ImGui.BeginPopupContextItem($"ContextMenu##{mob.Name}", ImGuiPopupFlags.MouseButtonRight))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Vector4.One); //white
                if (ImGui.MenuItem("   Select", true)) _selectedIndex = n;
                ImGui.MenuItem("   ---", false);
                if (ImGui.MenuItem("   Remove", true)) toRemove = mob;
                ImGui.EndPopup();
                ImGui.PopStyleColor();
            }

            if (ImGui.IsItemActive() && !ImGui.IsItemHovered())
            {
                int n_next = n + (ImGui.GetMouseDragDelta(0).Y < 0f ? -1 : 1);
                if (n_next >= 0 && n_next < _mobList.Count)
                {
                    _mobList[n] = _mobList[n_next];
                    _mobList[n_next] = mob;
                    ImGui.ResetMouseDragDelta();
                }
            }

            ImGui.Separator();
            ImGui.PopStyleColor(); // pop style text colour
        }
        if (toRemove != null) _mobList.Remove(toRemove);
    }

    private string GetAttributeFromMob(HuntTrainMobAttribute attribute, HuntTrainMob mob) =>
        attribute switch
        {
            HuntTrainMobAttribute.Name => mob.Name,
            HuntTrainMobAttribute.LastSeen => $"{(DateTime.Now.ToUniversalTime() - mob.LastSeenUTC).TotalMinutes:0.}m",
            HuntTrainMobAttribute.Position => $"({mob.Position.X:0.0}, {mob.Position.Y:0.0})",
        };

    private void SelectNext()
    {
        for (int i = 0; _selectedIndex < _mobList.Count; _selectedIndex++)
        {
            if (!_mobList[_selectedIndex].Dead) return;

            //this way, if there is a preceding hunt that was skipped (still alive) for some reason, 
            //the list will continue downwards first, before looping back to the top 
            if (i != 0 || _selectedIndex != _mobList.Count - 1) continue;
            _selectedIndex = -1;
            i++;
        }
        _selectedIndex = 0; //if all mobs are dead, set to index 0.
    }
    private enum HuntTrainMobAttribute
    {
        Name, Position, LastSeen
    }
}