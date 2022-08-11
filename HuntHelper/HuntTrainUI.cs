using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface;
using Dalamud.Interface.Components;
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
    private readonly TrainManager _trainManager;
    private readonly Configuration _config;

    private bool _huntTrainWindowVisible = false;

    private string _copyText = "Copy export code clipboard.\n" +
                               "Warning: The code will be too long to be pasted into in-game chat. Share via Discord.";
    private Task _copyTextTask = Task.CompletedTask;
    private int _tooltipChangeTime = 400;

    #region user customisable - config
    private Vector2 _huntTrainWindowSize = new Vector2(310, 270);
    private Vector2 _huntTrainWindowPos = new Vector2(150, 150);

    private bool _showPos = true;
    private bool _showLastSeen = true;
    private bool _useBorder = false;
    #endregion

    private Vector4 _deadTextColour = new Vector4(.6f, .7f, .6f, 1f);

    private int _selectedIndex = 0;
    private readonly List<HuntTrainMob> _mobList;
    private readonly List<HuntTrainMob> _importedTrain;

    //import new is basically the same as import all but doesn't delete anything, think it's fine to leave as default
    private bool _importAll = false;
    private bool _importNew = true;
    private bool _importUpdateTime = true;

    public bool HuntTrainWindowVisible
    {
        get => _huntTrainWindowVisible;
        set => _huntTrainWindowVisible = value;
    }

    public HuntTrainUI(TrainManager trainManager, Configuration config)
    {
        _trainManager = trainManager;
        _mobList = _trainManager.HuntTrain;
        _config = config;
        _importedTrain = _trainManager.ImportedTrain;
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
        _showPos = _config.HuntTrainShowPos;
        _showLastSeen = _config.HuntTrainShowLastSeen;
        _useBorder = _config.HuntTrainUseBorder;
    }

    public void SaveSettings()
    {
        //_config.HuntTrainWindowVisible = HuntTrainWindowVisible;
        _config.HuntTrainWindowSize = _huntTrainWindowSize;
        _config.HuntTrainWindowPos = _huntTrainWindowPos;
        _config.HuntTrainShowPos = _showPos;
        _config.HuntTrainShowLastSeen = _showLastSeen;
        _config.HuntTrainUseBorder = _useBorder;
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
                new Vector2(ImGui.GetWindowSize().X, (_mobList.Count + 1) * 24f), _useBorder); //23f is the size of a selectable from SelectableFromList();

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
                                      "Background\n" +
                                      "Click on any of the rows to send the relevant Map Link into chat. (Drag to reorder the list)\n\n" +
                                      "Click on the checkbox to mark a mob as dead.\n\n" +
                                      "Right-click -- Select - change the selected mob\n" +
                                      "            -- Remove - Removes the mob from the list (This permanently deletes data on that mob)\n\n" +
                                      "Use the command \"/hhn\" to automatically mark the current selected as dead, and send the next Map link into chat.\n" +
                                      "(you will have to click the first one manually)\n\n" +
                                      "*Does not allow duplicates (currently does not handle instances or different worlds) - Only records A ranks.\n" +
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

            ImGui.Checkbox("pos", ref _showPos);
            ImGui.SameLine(); ImGui.Checkbox("last seen", ref _showLastSeen);
            ImGui.SameLine(); ImGui.Checkbox("border", ref _useBorder);
            ImGui.Dummy(new Vector2(0, 12f));


            #region Buttons
            
            if (ImGuiComponents.IconButton(FontAwesomeIcon.History)) _trainManager.TrainRemoveDead();
            ImGui_HoveredToolTip("Remove Dead");
            ImGui.SameLine(); ImGui.Dummy(new Vector2(4, 0)); ImGui.SameLine();

            if (ImGuiComponents.IconButton(FontAwesomeIcon.Syringe)) _trainManager.TrainUnkillAll();
            ImGui_HoveredToolTip("Reset Dead Status");
            ImGui.SameLine(); ImGui.Dummy(new Vector2(4, 0)); ImGui.SameLine();

            //position record button on far right
            ImGui.SetCursorPosX(ImGui.GetWindowSize().X-26);
            if (!_trainManager.RecordTrain)
            {
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Play)) _trainManager.RecordTrain = true;
                ImGui_HoveredToolTip("Start Recording");
            }
            else
            {
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Pause)) _trainManager.RecordTrain = false;
                ImGui_HoveredToolTip("Stop Recording");
            }

            ImGui.Dummy(new Vector2(0, 20f));

            if (ImGuiComponents.IconButton(FontAwesomeIcon.Skull)) ImGui.OpenPopup("Delete##modal");
            ImGui_HoveredToolTip("Delete Train");
            ImGui.SameLine();
           
            //gosh these buttons don't line up, off by like 1 pixel :(
            ImGui.SetCursorPosX(ImGui.GetWindowSize().X-54);
            if (ImGuiComponents.IconButton(FontAwesomeIcon.SignOutAlt))
            {
                //get export code
                var exportCode = ExportImport.Export(_mobList);
                //copy to clipboard
                ImGui.SetClipboardText(exportCode);
                ChangeCopyText();
            }
            ImGui_HoveredToolTip(_copyText);
            ImGui.SameLine(); ImGui.Dummy(new Vector2(4, 0)); ImGui.SameLine();

            if (ImGuiComponents.IconButton(FontAwesomeIcon.SignInAlt))
            {
                var importCode = ImGui.GetClipboardText();
                //ExportImport.Import(importCode, _importedTrain);
                _trainManager.Import(importCode);
                ImGui.OpenPopup("Import##popup");
            }
            ImGui_HoveredToolTip("Import");

            DrawDeleteModal();
            DrawImportWindowModal();

            //ImGui.TextUnformatted($"{ImGui.GetWindowSize()}");
            #endregion

            //ImGui.TextUnformatted($"{ImGui.GetWindowSize()}");

            ImGui.PopStyleVar(); //pop itemspacing
            ImGui.End();
        }
        ImGui.PopStyleVar();//pop window padding
    }

    private void DrawDeleteModal()
    {
        var center = ImGui.GetWindowPos() + ImGui.GetWindowSize() / 2;
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new Vector2(400, 200));


        if (ImGui.BeginPopupModal("Delete##modal"))
        {
            ImGui.TextWrapped("Are you sure you want to DELETE train data?");

            ImGui.Dummy(new Vector2(0, 25));
            ImGui.Dummy(new Vector2(6, 0)); ImGui.SameLine();
            if (ImGui.Button("Delete"))
            {
                _trainManager.TrainDelete();
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine(); ImGui.Dummy(new Vector2(16, 0));
            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
    }
    private void DrawImportWindowModal()
    {
        var center = ImGui.GetWindowPos() + ImGui.GetWindowSize() / 2;
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new Vector2(400, 700));

        if (ImGui.BeginPopupModal("Import##popup"))
        {

            //if count is zero -> show message
            if (_importedTrain.Count == 0)
            {
                ImGui.TextWrapped("Nothing to import - or incorrect code\n\n" +
                           "The export code is too long to be shared via in-game chat. Please share through Discord or something.");
                ImGui.Dummy(new Vector2(0, 25));
                ImGui.Dummy(new Vector2(6, 0)); ImGui.SameLine();
                if (ImGui.Button("Close", new Vector2(80, 0))) ImGui.CloseCurrentPopup();
                return;
            }


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
                    {   //green text if new mob, grey text if exists.
                        ImGui.PushStyleColor(ImGuiCol.Text,
                            _mobList.All(mob => mob.Name != m.Name)
                                ? new Vector4(0.1647f, 1f, 0.647f, 1f) //greenish
                                : new Vector4(0.51f, 0.51f, 0.51f, 1f)); //grey
                                                                         // : new Vector4(1f, 0.345f, 0.345f, 1f)); //redish
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
            _trainManager.ImportTrainAll();
            return;
        }
        _trainManager.ImportTrainNew(_importUpdateTime);
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
        if (!_mobList[_selectedIndex].Dead) _trainManager.SendTrainFlag(_selectedIndex);
        else _trainManager.SendTrainFlag(-1);
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
            if (ImGui.IsItemActive() && ImGui.IsMouseClicked(ImGuiMouseButton.Left)) _trainManager.SendTrainFlag(n);

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

    private void ImGui_HoveredToolTip(string msg)
    {
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text(msg);
            ImGui.EndTooltip();
        }
    }
}