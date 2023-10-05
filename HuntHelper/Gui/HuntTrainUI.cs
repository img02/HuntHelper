using Dalamud.Interface;
using Dalamud.Interface.Components;
using HuntHelper.Gui.Resource;
using HuntHelper.Managers;
using HuntHelper.Managers.Hunts;
using HuntHelper.Managers.Hunts.Models;
using HuntHelper.Utilities;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface.Utility;

namespace HuntHelper.Gui;

public class HuntTrainUI : IDisposable
{
    private readonly TrainManager _trainManager;
    private readonly Configuration _config;

    private bool _huntTrainWindowVisible = false;

    private string _copyText = GuiResources.HuntTrainGuiText["CopyText"];
    private Task _copyTextTask = Task.CompletedTask;
    private int _tooltipChangeTime = 400;

    #region user customisable - config
    private Vector2 _huntTrainWindowSize = new Vector2(310, 270);
    private Vector2 _huntTrainWindowPos = new Vector2(150, 150);

    private bool _showLastSeen = true;
    private bool _useBorder = false;
    private bool _openMap = false;
    private bool _teleportMe = false;
    private bool _teleportMeOnCommand = false;
    private bool _showTeleButtons = false;
    #endregion

    private Vector4 _deadTextColour = new Vector4(.6f, .7f, .6f, 1f);

    private int _selectedIndex = 0;
    private readonly List<HuntTrainMob> _mobList;
    private readonly List<HuntTrainMob> _importedTrain;

    //import new is basically the same as import all but doesn't delete anything, think it's fine to leave as default
    private bool _importAll = false;
    private bool _importNew = true;
    private bool _importUpdateTime = true;

    private TeleportManager _teleportManager;

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
        _teleportManager = new TeleportManager();
    }

    public void Draw()
    {
        DrawHuntTrainWindow();
    }

    public void Dispose()
    {
        //SaveSettings();
    }

    private void LoadSettings()
    {
        _huntTrainWindowSize = _config.HuntTrainWindowSize;
        _huntTrainWindowPos = _config.HuntTrainWindowPos;
        _showLastSeen = _config.HuntTrainShowLastSeen;
        _useBorder = _config.HuntTrainUseBorder;
        _openMap = _config.HuntTrainNextOpensMap;
        _teleportMe = _config.HuntTrainNextTeleportMe;
        _teleportMeOnCommand = _config.HuntTrainNextTeleportMeOnCommand;
        _showTeleButtons = _config.HuntTrainShowTeleportButtons;
    }

    public void SaveSettings()
    {
        //_config.HuntTrainWindowVisible = HuntTrainWindowVisible;
        _config.HuntTrainWindowSize = _huntTrainWindowSize;
        _config.HuntTrainWindowPos = _huntTrainWindowPos;
        _config.HuntTrainShowLastSeen = _showLastSeen;
        _config.HuntTrainUseBorder = _useBorder;
        _config.HuntTrainNextOpensMap = _openMap;
        _config.HuntTrainNextTeleportMe = _teleportMe;
        _config.HuntTrainNextTeleportMeOnCommand = _teleportMeOnCommand;
        _config.HuntTrainShowTeleportButtons = _showTeleButtons;
    }


    private Vector4 bg = new Vector4(.3f, .3f, .3f, 1f);
    public void DrawHuntTrainWindow()
    {
        if (!HuntTrainWindowVisible) return;

        var numOfColumns = 2;
        if (_showLastSeen) numOfColumns++;
        if (_showTeleButtons) numOfColumns++;

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
        ImGui.SetNextWindowSize(_huntTrainWindowSize, ImGuiCond.FirstUseEver);
        ImGui.SetWindowPos(_huntTrainWindowPos);
        if (ImGui.Begin($"{GuiResources.HuntTrainGuiText["MainWindowTitle"]}##Window", ref _huntTrainWindowVisible, ImGuiWindowFlags.NoScrollbar))
        {
            var childSizeX = ImGui.GetWindowSize().X / numOfColumns;
            var childSizeY = _mobList.Count * 23 * ImGuiHelpers.GlobalScale;
            var lastSeenWidth = childSizeX * ImGuiHelpers.GlobalScale; //math is hard, resizing is hard owie :(
            var teleWidth = childSizeX * ImGuiHelpers.GlobalScale;
            var nameWidth = childSizeX * 1.75f * ImGuiHelpers.GlobalScale;
            if (_showTeleButtons && _showLastSeen)
            {
                nameWidth = childSizeX * 2.25f;
                lastSeenWidth = childSizeX * .75f;
                teleWidth = childSizeX * .75f;
            }

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));

            #region hunt train main

            ImGui.BeginChild("HUNT TRAIN MAIN CHILD WINDOW ALL ENCOMPASSING LAYOUT FIXING CHILD OF WINDOW",
                new Vector2(ImGui.GetWindowSize().X, (_mobList.Count + 1) * 24f * ImGuiHelpers.GlobalScale), _useBorder); //23f is the size of a selectable from SelectableFromList();

            #region Headers

            ImGui.SameLine();
            ImGui.BeginChild("NameHeader", new Vector2(nameWidth, 20 * ImGuiHelpers.GlobalScale), _useBorder,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
            ImGui.Text(GuiResources.HuntTrainGuiText["NameHeader"]);
            ImGui.EndChild();
            if (_showTeleButtons)
            {
                ImGui.SameLine();
                ImGui.BeginChild("TeleButtonHeader", new Vector2(teleWidth, 20 * ImGuiHelpers.GlobalScale),
                    _useBorder, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
                ImGui.Text(GuiResources.HuntTrainGuiText["TeleportHeader"]);
                ImGuiUtil.ImGui_HoveredToolTip($"{GuiResources.HuntTrainGuiText["TeleportToolTip"]}");
                ImGui.EndChild();
            }

            if (_showLastSeen)
            {
                ImGui.SameLine();
                ImGui.BeginChild("LastSeenHeader", new Vector2(lastSeenWidth, 20 * ImGuiHelpers.GlobalScale),
                    _useBorder, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
                ImGui.Text(GuiResources.HuntTrainGuiText["LastSeenHeader"]);
                ImGui.EndChild();
            }

            ImGui.SameLine();
            ImGuiUtil.ImGui_HelpMarker(GuiResources.HuntTrainGuiText["HowTo"]);
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(1f, .3f, .3f, 1f), "  <<<");

            #endregion

            ImGui.Separator();

            ImGui.BeginChild("Name", new Vector2(nameWidth, childSizeY), _useBorder,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
            SelectableFromList(HuntTrainMobAttribute.Name);
            ImGui.EndChild();

            if (_showTeleButtons)
            {
                ImGui.SameLine();
                ImGui.BeginChild("Tele", new Vector2(teleWidth, childSizeY), _useBorder,
                    ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
                foreach (var m in _mobList)
                {
                    if (ImGui.Button($"{GuiResources.HuntTrainGuiText["TeleportButton"]}##{m.MapID}{m.Position}"))
                    {
                        _teleportManager.TeleportToHunt(m);
                        _trainManager.OpenMap(m, _openMap);
                    }
                    if (ImGui.IsItemHovered()) ImGuiUtil.ImGui_HoveredToolTip($"{m.Name}");
                }
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

            if (ImGui.TreeNode($"options##treeeee", $"{GuiResources.HuntTrainGuiText["Settings"]}"))
            {
                if (ImGui.BeginTable("settingsalignment", 2))
                {
                    ImGui.TableNextColumn();
                    ImGui.Checkbox(GuiResources.HuntTrainGuiText["Border"], ref _useBorder);
                    ImGui.TableNextColumn();
                    ImGui.Checkbox(GuiResources.HuntTrainGuiText["LastSeen"], ref _showLastSeen);
                    ImGuiUtil.ImGui_HoveredToolTip(GuiResources.HuntTrainGuiText["LastSeenToolTip"]);
                    ImGui.TableNextColumn();
                    ImGui.Checkbox(GuiResources.HuntTrainGuiText["OpenMap"], ref _openMap); //assuming this is allowed as it requires user interaction, but opening map on S rank spawn would be passive and so 'automated'
                    ImGuiUtil.ImGui_HoveredToolTip(GuiResources.HuntTrainGuiText["OpenMapToolTip"]);
                    ImGui.TableNextColumn();
                    ImGui.Checkbox(GuiResources.HuntTrainGuiText["TeleButtons"], ref _showTeleButtons);
                    ImGuiUtil.ImGui_HoveredToolTip(GuiResources.HuntTrainGuiText["TeleButtonsToolTip"]);
                    ImGui.TableNextColumn();
                    ImGui.Checkbox(GuiResources.HuntTrainGuiText["TeleportOnClick"], ref _teleportMe);
                    ImGuiUtil.ImGui_HoveredToolTip(GuiResources.HuntTrainGuiText["TeleportOnClickToolTip"]);
                    ImGui.TableNextColumn();
                    ImGui.Checkbox(GuiResources.HuntTrainGuiText["TeleportOnCommand"], ref _teleportMeOnCommand);
                    ImGuiUtil.ImGui_HoveredToolTip(GuiResources.HuntTrainGuiText["TeleportOnCommand"]);
                    ImGui.EndTable();
                }

                ImGui.TreePop();
                ImGui.Separator();
            }
            ImGui.Dummy(new Vector2(0, 12f));


            #region Buttons

            if (ImGuiComponents.IconButton(FontAwesomeIcon.History)) _trainManager.TrainRemoveDead();
            ImGuiUtil.ImGui_HoveredToolTip(GuiResources.HuntTrainGuiText["RemoveDeadButton"]);
            ImGui.SameLine(); ImGui.Dummy(new Vector2(4, 0)); ImGui.SameLine();

            if (ImGuiComponents.IconButton(FontAwesomeIcon.Syringe)) _trainManager.TrainUnkillAll();
            ImGuiUtil.ImGui_HoveredToolTip(GuiResources.HuntTrainGuiText["ResetDeadButton"]);
            ImGui.SameLine(); ImGui.Dummy(new Vector2(4, 0)); ImGui.SameLine();

            //position record button on far right
            ImGui.SetCursorPosX(ImGui.GetWindowSize().X - (26 * ImGuiHelpers.GlobalScale));
            if (!_trainManager.RecordTrain)
            {
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Play)) _trainManager.RecordTrain = true;
                ImGuiUtil.ImGui_HoveredToolTip(GuiResources.HuntTrainGuiText["StartRecordingButton"]);
            }
            else
            {
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Pause)) _trainManager.RecordTrain = false;
                ImGuiUtil.ImGui_HoveredToolTip(GuiResources.HuntTrainGuiText["StopRecordingButton"]);
            }

            ImGui.Dummy(new Vector2(0, 20f));

            if (ImGuiComponents.IconButton(FontAwesomeIcon.Skull)) ImGui.OpenPopup($"{GuiResources.HuntTrainGuiText["DeleteWindowTitle"]}##modal");
            ImGuiUtil.ImGui_HoveredToolTip(GuiResources.HuntTrainGuiText["DeleteTrainButton"]);
            ImGui.SameLine();

            //gosh these buttons don't line up, off by like 1 pixel :(
            ImGui.SetCursorPosX(ImGui.GetWindowSize().X - (54 * ImGuiHelpers.GlobalScale));
            if (ImGuiComponents.IconButton(FontAwesomeIcon.SignOutAlt))
            {
                //get export code
                var exportCode = ExportImport.Export(_mobList);
                //copy to clipboard
                ImGui.SetClipboardText(exportCode);
                ChangeCopyText();
            }

            ImGuiUtil.ImGui_HoveredToolTip(_copyText);
            ImGui.SameLine(); ImGui.Dummy(new Vector2(4, 0)); ImGui.SameLine();

            if (ImGuiComponents.IconButton(FontAwesomeIcon.SignInAlt))
            {
                var importCode = string.Empty;
                try
                {
                    importCode = ImGui.GetClipboardText();
                }
                catch { } // if clipboard doesn't have string throws object null error. i.e. a file
                _trainManager.Import(importCode);
                ImGui.OpenPopup($"{GuiResources.HuntTrainGuiText["ImportWindowTitle"]}##popup");
            }

            ImGuiUtil.ImGui_HoveredToolTip(GuiResources.HuntTrainGuiText["ImportButton"]);

            DrawDeleteModal();
            DrawImportWindowModal();
            #endregion

            if (_teleportManager.TeleportPluginNotFound) ImGui.OpenPopup($"{GuiResources.HuntTrainGuiText["TeleportWindowTitle"]}##ModalPopupWindowThingymajig");
            DrawTeleportPluginNotFoundModal();
            ImGui.PopStyleVar(); //pop itemspacing
            ImGui.End();
        }
        ImGui.PopStyleVar();//pop window padding
    }

    private void DrawTeleportPluginNotFoundModal()
    {
        var center = ImGui.GetWindowPos() + ImGui.GetWindowSize() / 2;
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new Vector2(315 * ImGuiHelpers.GlobalScale, 120 * ImGuiHelpers.GlobalScale), ImGuiCond.Always);
        ImGui.PushStyleColor(ImGuiCol.ResizeGrip, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 8));
        if (ImGui.BeginPopupModal($"{GuiResources.HuntTrainGuiText["TeleportWindowTitle"]}##ModalPopupWindowThingymajig"))
        {
            ImGui.TextWrapped(GuiResources.HuntTrainGuiText["TeleportWindowMessage"]);
            ImGui.Dummy(Vector2.One);
            ImGui.SetCursorPosX(ImGui.GetWindowSize().X - 80f * ImGuiHelpers.GlobalScale);
            if (ImGui.Button(GuiResources.HuntTrainGuiText["TeleportWindowButton"]))
            {
                _teleportManager.TeleportPluginNotFound = false;
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
        ImGui.PopStyleVar();
        ImGui.PopStyleColor();
    }

    private void DrawDeleteModal()
    {
        var center = ImGui.GetWindowPos() + ImGui.GetWindowSize() / 2;
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new Vector2(400, 200));

        if (ImGui.BeginPopupModal($"{GuiResources.HuntTrainGuiText["DeleteWindowTitle"]}##modal"))
        {
            ImGui.TextWrapped(GuiResources.HuntTrainGuiText["DeleteWindowMessage"]);

            ImGui.Dummy(new Vector2(0, 25));
            ImGui.Dummy(new Vector2(6, 0)); ImGui.SameLine();
            if (ImGui.Button(GuiResources.HuntTrainGuiText["DeleteWindowDeleteButton"]))
            {
                _trainManager.TrainDelete();
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine(); ImGui.Dummy(new Vector2(16, 0));
            ImGui.SameLine();
            if (ImGui.Button(GuiResources.HuntTrainGuiText["CancelButton"]))
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
        ImGui.SetNextWindowSize(new Vector2(400, 700), ImGuiCond.FirstUseEver);

        if (ImGui.BeginPopupModal($"{GuiResources.HuntTrainGuiText["ImportWindowTitle"]}##popup"))
        {

            //if count is zero -> show message
            if (_importedTrain.Count == 0)
            {
                ImGui.TextWrapped(GuiResources.HuntTrainGuiText["ImportWindowErrorMessage"]);
                ImGui.Dummy(new Vector2(0, 25));
                ImGui.Dummy(new Vector2(6, 0)); ImGui.SameLine();
                if (ImGui.Button(GuiResources.HuntTrainGuiText["ImportWindowCloseButton"], new Vector2(80, 0))) ImGui.CloseCurrentPopup();
                return;
            }

            ImGui.Text($"{GuiResources.HuntTrainGuiText["ImportWindowImportedMessage"]}{_importedTrain.Count}");

            if (ImGui.BeginChild("tablechild", new Vector2(ImGui.GetWindowSize().X, (ImGui.GetTextLineHeightWithSpacing() + 5) * (_importedTrain.Count + 1))))
            {

                if (ImGui.BeginTable("ImportTable", 2, ImGuiTableFlags.BordersH))
                {
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(GuiResources.HuntTrainGuiText["NameHeader"]);
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(GuiResources.HuntTrainGuiText["LastSeenHeader"]);
                    foreach (var m in _importedTrain)
                    {   //green text if new mob, grey text if exists.
                        ImGui.PushStyleColor(ImGuiCol.Text,
                            _mobList.All(mob => mob.MobID != m.MobID)
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

            if (ImGui.Checkbox(GuiResources.HuntTrainGuiText["ImportAllCheckbox"], ref _importAll))
            {
                _importNew = false;
                _importUpdateTime = false;
            }
            ImGui.SameLine(); ImGuiUtil.ImGui_HelpMarker(GuiResources.HuntTrainGuiText["ImportAllCheckboxToolTip"]);
            ImGui.Dummy(new Vector2(0, 6f));
            ImGui.Separator();
            ImGui.Dummy(new Vector2(0, 6f));

            if (ImGui.Checkbox(GuiResources.HuntTrainGuiText["ImportNewCheckbox"], ref _importNew))
            {
                _importAll = false;
                if (!_importNew) _importUpdateTime = false;
            }
            ImGui.SameLine(); ImGuiUtil.ImGui_HelpMarker(GuiResources.HuntTrainGuiText["ImportNewCheckboxToolTip"]);


            ImGui.SameLine(); ImGui.Dummy(new Vector2(16, 0));
            ImGui.SameLine();

            if (ImGui.Checkbox(GuiResources.HuntTrainGuiText["UpdateLastSeenCheckbox"], ref _importUpdateTime))
            {
                _importNew = true;
                _importAll = false;
            };
            ImGui.SameLine(); ImGuiUtil.ImGui_HelpMarker(GuiResources.HuntTrainGuiText["UpdateLastSeenCheckboxToolTip"]);

            ImGui.Dummy(new Vector2(0, 100));
            ImGui.Dummy(new Vector2(6, 0));
            ImGui.SameLine();
            if (ImGui.Button(GuiResources.HuntTrainGuiText["ImportButton"], new Vector2(80, 0)))
            {
                if (!_importNew && !_importUpdateTime && !_importAll) return;
                ImportTrainData();
                _importedTrain.Clear();
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine(); ImGui.Dummy(new Vector2(6, 0));
            ImGui.SameLine();
            if (ImGui.Button(GuiResources.HuntTrainGuiText["CancelButton"], new Vector2(80, 0)))
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
            _copyText = GuiResources.HuntTrainGuiText["CopyTextCopied"];
            Thread.Sleep(_tooltipChangeTime);
            _copyText = temp;
        });
    }

    //called from command, sets current mob as dead, selects next, sends flag.
    public void GetNextMobCommand()
    {
        if (_selectedIndex < _mobList.Count) _mobList[_selectedIndex].Dead = true;
        SelectNext();
        if (!_mobList[_selectedIndex].Dead)
        {
            _trainManager.SendTrainFlag(_selectedIndex, _openMap);
            if (_teleportMeOnCommand) _teleportManager.TeleportToHunt(_mobList[_selectedIndex]);
        }
        else _trainManager.SendTrainFlag(-1, false);
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

            if (n == _selectedIndex) ImGui.Selectable($"{label}", true, ImGuiSelectableFlags.None, new Vector2(ImGui.GetContentRegionAvail().X, 23f * ImGuiHelpers.GlobalScale));
            else ImGui.Selectable($"{label}", false, ImGuiSelectableFlags.None, new Vector2(ImGui.GetContentRegionAvail().X, 23f * ImGuiHelpers.GlobalScale));

            if (ImGui.IsItemActive() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                _trainManager.SendTrainFlag(n, _openMap);
                if (_teleportMe) _teleportManager.TeleportToHunt(mob);
            }

            if (ImGui.BeginPopupContextItem($"ContextMenu##{mob.Name}", ImGuiPopupFlags.MouseButtonRight))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Vector4.One); //white
                if (ImGui.MenuItem(GuiResources.HuntTrainGuiText["ContextMenuSelect"], true)) _selectedIndex = n;
                ImGui.MenuItem("   ---", false);
                if (ImGui.MenuItem(GuiResources.HuntTrainGuiText["ContextMenuRemove"], true)) toRemove = mob;
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
            HuntTrainMobAttribute.Position => $"({mob.Position.X:0.0}, {mob.Position.Y:0.0})"
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