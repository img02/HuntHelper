using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using HuntHelper.Gui.Resource;
using HuntHelper.Managers;
using HuntHelper.Managers.Hunts;
using HuntHelper.Managers.Hunts.Models;
using HuntHelper.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

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
    private bool _teleToAetheryte = false;
    private bool _showInChat = true;
    private bool _showTrainUIDuringIPCImport = true;
    private bool _showMapName = false;
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
        if (_showTrainUIDuringIPCImport && _trainManager.ImportFromIPC) { _huntTrainWindowVisible = true; }
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
        _teleToAetheryte = _config.HuntTrainTeleportToAetheryte;
        _showInChat = _config.HuntTrainShowFlagInChat;
        _showTrainUIDuringIPCImport = _config.HuntTrainShowUIDuringIPCImport;
        _showMapName = _config.HuntTrainShowMapName;
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
        _config.HuntTrainTeleportToAetheryte = _teleToAetheryte;
        _config.HuntTrainShowFlagInChat = _showInChat;
        _config.HuntTrainShowUIDuringIPCImport = _showTrainUIDuringIPCImport;
        _config.HuntTrainShowMapName = _showMapName;
    }


    private Vector4 bg = new Vector4(.3f, .3f, .3f, 1f);
    private float trainItemY => 23f * ImGuiHelpers.GlobalScale;
    private ImGuiWindowFlags defaultTrainFlags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

    public void DrawHuntTrainWindow()
    {
        if (!HuntTrainWindowVisible) return;
        if (_trainManager.ImportFromIPC) ImGui.SetNextWindowCollapsed(false);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
        ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize, 10);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));

        ImGui.SetNextWindowSize(_huntTrainWindowSize, ImGuiCond.FirstUseEver);
        ImGui.SetWindowPos(_huntTrainWindowPos);

        if (ImGui.Begin($"{GuiResources.HuntTrainGuiText["MainWindowTitle"]}##Window", ref _huntTrainWindowVisible))
        {


            float moveCursorX(ref float x, float move)
            {
                x += move * ImGuiHelpers.GlobalScale;
                return x;
            }

            #region main train list            
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 6); //padding

            for (int n = 0; n < _mobList.Count; n++)
            {
                var m = _mobList[n];
                var x = 0f;

                if (m.Dead) ImGui.PushStyleColor(ImGuiCol.Text, _deadTextColour);
                else ImGui.PushStyleColor(ImGuiCol.Text, Vector4.One); //white

                ImGui.SetCursorPosX(moveCursorX(ref x, 6)); //random padding
                ImGui.BeginGroup();

                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));
                if (_showMapName)
                {
                    ImGui.Selectable($"「{MapHelpers.GetMapName(m.TerritoryID)}」##{m.Name} {m.Instance.GetInstanceGlyph()}", n == _selectedIndex,
                        ImGuiSelectableFlags.None, new Vector2(ImGui.GetWindowWidth(), trainItemY));
                    ImGui.SetItemAllowOverlap();
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(moveCursorX(ref x, 160));
                    ImGui.Text($"{m.Name} {m.Instance.GetInstanceGlyph()}");

                }
                else
                {
                    ImGui.Selectable($"{m.Name} {m.Instance.GetInstanceGlyph()}", n == _selectedIndex,
                        ImGuiSelectableFlags.None, new Vector2(ImGui.GetWindowWidth(), trainItemY));
                }
                ImGui.PopStyleVar();

                ImGui.SetItemAllowOverlap();
                ImGui.SameLine();
                ImGui.SetCursorPosX(moveCursorX(ref x, 140));

                //disable tele buttons if tele on click cause clipping issue i dont want to fix
                if (_showTeleButtons && !_teleportMe)
                {
                    if (ImGui.Button($"{GuiResources.HuntTrainGuiText["TeleportButton"]}##{m.MapID}{m.Position}", new Vector2(33f * ImGuiHelpers.GlobalScale, trainItemY - 2)))
                    {
                        _teleportManager.TeleportToHunt(m);
                        _trainManager.OpenMap(m, _openMap);
                    }
                    if (ImGui.IsItemHovered()) ImGuiUtil.ImGui_HoveredToolTip($"{m.Name}\n{m.MapName}");

                    ImGui.SetItemAllowOverlap();
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(moveCursorX(ref x, 40));
                }

                if (_showLastSeen)
                {
                    ImGui.Text($"{(DateTime.Now.ToUniversalTime() - m.LastSeenUTC).TotalMinutes:0.}m");
                    ImGui.SetItemAllowOverlap();
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(moveCursorX(ref x, 60));
                }

                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 99);
                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3 * ImGuiHelpers.GlobalScale);
                var d = m.Dead;
                ImGui.Checkbox($"##DeadCheckBox{m.Name}:{m.Instance}", ref d);
                ImGui.PopStyleVar(2);
                m.Dead = d;
                if (m.Dead && _mobList.IndexOf(m) == _selectedIndex) SelectNext();//infinite if all dead
                ImGui.SetItemAllowOverlap();

                ImGui.Separator();
                ImGui.EndGroup();

                //mouse pos calc so it doesn't glitch out when clicking outside group -- far right in expanded window
                var rightSideOfNamePosX = ImGui.GetMousePos().X - ImGui.GetWindowPos().X;

                // click = send flag / tele, drag = move in list
                if (ImGui.IsItemFocused() && rightSideOfNamePosX < x)
                {
                    if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) && Math.Abs(ImGui.GetMouseDragDelta().Y) < 0.1f)
                    {
                        _trainManager.SendTrainFlag(n, _openMap, _showInChat);
                        if (_teleportMe) _teleportManager.TeleportToHunt(m);
                    }

                    if (!ImGui.IsItemHovered())
                    {
#if DEBUG
                        PluginLog.Error($"{ImGui.GetMouseDragDelta(ImGuiMouseButton.Left).Y}");
#endif
                        int n_next = n;
                        if (ImGui.GetMouseDragDelta(0, 0f).Y < 0.3f) n_next -= 1;
                        else if (ImGui.GetMouseDragDelta(ImGuiMouseButton.Left).Y > 0f) n_next += 1;
                        if (n_next >= 0 && n_next < _mobList.Count)
                        {
                            _mobList[n] = _mobList[n_next];
                            _mobList[n_next] = m;
                            ImGui.ResetMouseDragDelta();
                        }
                    }
                }

                if (ImGui.BeginPopupContextItem($"ContextMenu##{m.Name}:{m.Instance}", ImGuiPopupFlags.MouseButtonRight))
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Vector4.One); //white
                    if (ImGui.MenuItem(GuiResources.HuntTrainGuiText["ContextMenuSelect"], true)) _selectedIndex = n;
                    ImGui.MenuItem("   ---", false);
                    if (ImGui.MenuItem(GuiResources.HuntTrainGuiText["ContextMenuRemove"], true)) _trainManager.TrainRemove(m);
                    ImGui.EndPopup();
                    ImGui.PopStyleColor();
                }

                ImGui.PopStyleColor();
            }

            ImGui.Separator();
            ImGui.Dummy(new Vector2(0, 10));
            #endregion


            if (ImGui.TreeNode($"{GuiResources.HuntTrainGuiText["Settings"]}"))
            {
                if (ImGui.TreeNode("cosmetic##iwanttodeletethisbutmaybesomeusersactuallyuseit"))
                {
                    if (ImGui.BeginTable("settingsalignment", 2))
                    {
                        ImGui.TableNextColumn();
                        ImGui.Checkbox(GuiResources.HuntTrainGuiText["Border"], ref _useBorder); //doesn't do anything anymore
                        ImGui.TableNextColumn();
                        ImGui.Checkbox(GuiResources.HuntTrainGuiText["LastSeen"], ref _showLastSeen);
                        ImGuiUtil.ImGui_HoveredToolTip(GuiResources.HuntTrainGuiText["LastSeenToolTip"]);
                        ImGui.TableNextColumn();
                        ImGui.Checkbox("Show Map Name", ref _showMapName);
                        ImGuiUtil.ImGui_HoveredToolTip("Show Map Name");
                        ImGui.TableNextColumn();
                        ImGui.Checkbox(GuiResources.HuntTrainGuiText["ShowFlagInChat"], ref _showInChat);
                        ImGuiUtil.ImGui_HoveredToolTip(GuiResources.HuntTrainGuiText["ShowFlagInChat"]);
                        ImGui.EndTable();
                    }
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("Teleport"))
                {
                    if (ImGui.BeginTable("settingsalignment", 2))
                    {
                        ImGui.TableNextColumn();
                        ImGui.Checkbox(GuiResources.HuntTrainGuiText["OpenMap"], ref _openMap);
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
                        ImGui.TableNextColumn();
                        ImGui.Checkbox(GuiResources.HuntTrainGuiText["TeleportToAetheryte"], ref _teleToAetheryte);
                        ImGuiUtil.ImGui_HoveredToolTip(GuiResources.HuntTrainGuiText["TeleportToAetheryte"]);
                        ImGui.EndTable();
                    }
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("IPCRelatedStuffIdk"))
                {
                    if (ImGui.BeginTable("settingsalignment", 1))
                    {
                        ImGui.TableNextColumn();
                        ImGui.Checkbox(GuiResources.HuntTrainGuiText["OpenUIWhenIPCImport"], ref _showTrainUIDuringIPCImport);
                        ImGuiUtil.ImGui_HoveredToolTip(GuiResources.HuntTrainGuiText["OpenUIWhenIPCImport"]);
                        ImGui.EndTable();
                    }
                    ImGui.TreePop();
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
            ImGui.SetCursorPosX(ImGui.GetWindowSize().X - (50 * ImGuiHelpers.GlobalScale));
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
            ImGui.SetCursorPosX(ImGui.GetWindowSize().X - (80 * ImGuiHelpers.GlobalScale));
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

            if (ImGuiComponents.IconButton(FontAwesomeIcon.SignInAlt) || _trainManager.ImportFromIPC)
            {
                if (_trainManager.ImportFromIPC)
                {
                    _trainManager.ImportFromIPC = false;
                }
                else
                {
                    var importCode = string.Empty;
                    try
                    {
                        importCode = ImGui.GetClipboardText();
                    }
                    catch { } // if clipboard doesn't have string throws object null error. i.e. a file
                    _trainManager.Import(importCode);
                }

                ImGui.OpenPopup($"{GuiResources.HuntTrainGuiText["ImportWindowTitle"]}##popup");
            }

            ImGuiUtil.ImGui_HoveredToolTip(GuiResources.HuntTrainGuiText["ImportButton"]);

            DrawDeleteModal();
            DrawImportWindowModal();
            #endregion

            if (_teleportManager.TeleportPluginNotFound) ImGui.OpenPopup($"{GuiResources.HuntTrainGuiText["TeleportWindowTitle"]}##ModalPopupWindowThingymajig");
            DrawTeleportPluginNotFoundModal();

            ImGui.End();
        }
        ImGui.PopStyleVar(3); // itemspacing,  scrollbar sizing, window padding
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
                            _mobList.All(mob => mob.MobID != m.MobID || mob.Instance != m.Instance)
                                ? new Vector4(0.1647f, 1f, 0.647f, 1f) //greenish
                                : new Vector4(0.51f, 0.51f, 0.51f, 1f)); //grey
                                                                         // : new Vector4(1f, 0.345f, 0.345f, 1f)); //redish
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"{m.Name}{m.Instance.GetInstanceGlyph()}");
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
            }
            ;
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

    /// <summary>
    /// called from command /hhn, sets current mob as dead, selects next, sends flag.
    /// </summary>
    public void GetNextMobCommand()
    {
        if (_selectedIndex < _mobList.Count) _trainManager.TrainMarkAsDead(_selectedIndex);
        SelectNext();
        if (!_mobList[_selectedIndex].Dead)
        {
            _trainManager.SendTrainFlag(_selectedIndex, _openMap, _showInChat);
            if (_teleportMeOnCommand && !_teleToAetheryte) _teleportManager.TeleportToHunt(_mobList[_selectedIndex]);
        }
        else _trainManager.SendTrainFlag(-1, false, true);
    }

    /// <summary>
    /// called from command /hhna, gets flag of closest aetheryte to the next hunt mob
    /// </summary>
    public void GetNextMobNearestAetheryte()
    {
        var nextIndex = GetNextIndex();
        if (nextIndex == -1)
        {
            _trainManager.SendTrainFlag(-1, false, true);
            return;
        }

        if (!_mobList[nextIndex].Dead)
        {
            var mob = _mobList[nextIndex];
            var aeth = _teleportManager.GetNearestAetheryte(mob.TerritoryID, mob.Position);
            if (aeth == null) return;
            _trainManager.OpenMap((AetheryteData)aeth, mob);
            if (_teleportMeOnCommand && _teleToAetheryte) _teleportManager.TeleportToHunt(_mobList[nextIndex]);
        }
    }

    //based off of https://github.com/ocornut/imgui/blob/docking/imgui_demo.cpp#L2337
    /// <summary>
    /// no longer used since ui rework
    /// </summary>
    /// <param name="attributeToDisplay"></param>
    private void SelectableFromList(HuntTrainMobAttribute attributeToDisplay)
    {
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
                _trainManager.SendTrainFlag(n, _openMap, _showInChat);
                if (_teleportMe) _teleportManager.TeleportToHunt(mob);
            }

            if (ImGui.BeginPopupContextItem($"ContextMenu##{mob.Name}:{mob.Instance}", ImGuiPopupFlags.MouseButtonRight))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Vector4.One); //white
                if (ImGui.MenuItem(GuiResources.HuntTrainGuiText["ContextMenuSelect"], true)) _selectedIndex = n;
                ImGui.MenuItem("   ---", false);
                if (ImGui.MenuItem(GuiResources.HuntTrainGuiText["ContextMenuRemove"], true)) _trainManager.TrainRemove(mob);
                ImGui.EndPopup();
                ImGui.PopStyleColor();
            }

            if (ImGui.IsItemActive() && !ImGui.IsItemHovered())
            {
                int n_next = n + (ImGui.GetMouseDragDelta(ImGuiMouseButton.Left).Y < 0f ? -1 : 1);
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
    }

    private string GetAttributeFromMob(HuntTrainMobAttribute attribute, HuntTrainMob mob) =>
        attribute switch
        {
            HuntTrainMobAttribute.Name =>
                _showMapName ? $"「{MapHelpers.GetMapName(mob.TerritoryID)}」{mob.Name}{mob.Instance.GetInstanceGlyph()}"
                : $"{mob.Name}{mob.Instance.GetInstanceGlyph()}",
            HuntTrainMobAttribute.LastSeen => $"{(DateTime.Now.ToUniversalTime() - mob.LastSeenUTC).TotalMinutes:0.}m",
            HuntTrainMobAttribute.Position => $"({mob.Position.X:0.0}, {mob.Position.Y:0.0})"
        };

    /// <summary>
    /// returns -1 if no living mobs remain
    /// </summary>
    /// <returns></returns>
    private int GetNextIndex()
    {
        // start from selected index to continue downwards
        for (int i = _selectedIndex + 1; i < _mobList.Count; i++)
        {
            if (_mobList[i].Dead) continue;
            return i;
        }
        // then go back to start
        for (int i = 0; i < _selectedIndex; i++)
        {
            if (_mobList[i].Dead) continue;
            return i;
        }

        return -1;
    }

    private void SelectNext()
    {
        var nextIndex = GetNextIndex();
        _selectedIndex = nextIndex == -1 ? 0 : nextIndex;
    }

    private enum HuntTrainMobAttribute
    {
        Name, Position, LastSeen
    }

}