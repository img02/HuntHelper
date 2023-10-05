using Dalamud.Data;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using HuntHelper.Gui.Resource;
using HuntHelper.Managers.MapData;
using HuntHelper.Managers.MapData.Models;
using HuntHelper.Utilities;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;

namespace HuntHelper.Gui;
//https://github.com/mellinoe/ImGui.NET/issues/107#issuecomment-467146212
public unsafe class SpawnPointFinderUI : IDisposable//idk what to call this
{
    private readonly MapDataManager _mapDataManager;
    private readonly IDataManager _dataManager;
    private readonly Configuration _config;
    private readonly List<MapSpawnPoints> _spawnPoints;
    private ImGuiTextFilterPtr _filter;

    private bool _recordAll = false;
    private readonly List<MapSpawnPoints> _importList;
    private Task _copyTextTask = Task.CompletedTask;
    private string _copyText = GuiResources.SpawnPointerFinderGuiText["CopyText"];
    private string _copyTextAllExport = GuiResources.SpawnPointerFinderGuiText["CopyTextAllExport"];
    private int _tooltipChangeTime = 400;

    public bool WindowVisible = false;

    public SpawnPointFinderUI(MapDataManager mapDataManager, IDataManager dataManager, Configuration config)
    {
        _mapDataManager = mapDataManager;
        _dataManager = dataManager;
        _config = config;
        _spawnPoints = _mapDataManager.SpawnPointsList;
        LoadSettings();
        _importList = _mapDataManager.ImportedList;
        var filterPtr = ImGuiNative.ImGuiTextFilter_ImGuiTextFilter(null);
        _filter = new ImGuiTextFilterPtr(filterPtr);
    }

    public void Draw()
    {
        DrawSpawnPointRefinementWindow();
    }

    public void LoadSettings()
    {
        _recordAll = _config.SpawnPointRecordAll;
    }
    public void SaveSettings()
    {
        _config.SpawnPointRecordAll = _recordAll;
        _config.Save();
        _mapDataManager.SaveSpawnPointData();
    }

    public void Dispose()
    {
        ImGuiNative.ImGuiTextFilter_destroy(_filter.NativePtr);
    }

    public void DrawSpawnPointRefinementWindow()
    {
        if (!WindowVisible) return;
        _mapDataManager.SortSpawnlistByRecordingStatus();
        ImGui.SetNextWindowSize(new Vector2(250, 540), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new Vector2(50, 50), ImGuiCond.FirstUseEver);

        if (ImGui.Begin($"{GuiResources.SpawnPointerFinderGuiText["MainWindowTitle"]}##idkwhattocallthis", ref WindowVisible, ImGuiWindowFlags.NoScrollbar))
        {
            _filter.Draw(GuiResources.SpawnPointerFinderGuiText["FilterLabel"], 120);
            ImGui.SameLine(); ImGuiUtil.ImGui_HelpMarker(GuiResources.SpawnPointerFinderGuiText["FilterToolTip"]);
            ImGui.Dummy(Vector2.Zero);
            if (ImGui.BeginChild("sp table child##", new Vector2(ImGui.GetWindowSize().X, ImGui.GetWindowSize().Y * .82f), false, ImGuiWindowFlags.NoScrollbar))
            {
                if (ImGui.BeginTable("sp table##", 3, ImGuiTableFlags.BordersH))
                {
                    ImGui.TableSetupColumn("MapName", ImGuiTableColumnFlags.WidthStretch, 50);
                    ImGui.TableSetupColumn("MapID", ImGuiTableColumnFlags.WidthFixed, 25 * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("Buttons", ImGuiTableColumnFlags.WidthFixed, 40 * ImGuiHelpers.GlobalScale);

                    var i = 1;
                    var j = -1;
                    var k = 100;
                    foreach (var msp in _spawnPoints)
                    {
                        if (_filter.PassFilter(MapHelpers.GetMapName(_dataManager, msp.MapID)))
                        {
                            ImGui.TableNextColumn();
                            ImGui.TextUnformatted($"{MapHelpers.GetMapName(_dataManager, msp.MapID)}");
                            ImGui.TableNextColumn();
                            if (!msp.Recording)
                            { //using id overload bugged - now adds some other weird icon to the end ? gotta use push/pop id
                                ImGui.PushID(i++);
                                if (ImGuiComponents.IconButton(FontAwesomeIcon.Video)) msp.Recording = true;
                                ImGui.PopID();
                                ImGuiUtil.ImGui_HoveredToolTip(GuiResources.SpawnPointerFinderGuiText["StartRecording"]);
                            }
                            else
                            {
                                ImGui.PushID(j--);
                                if (ImGuiComponents.IconButton(FontAwesomeIcon.VideoSlash))
                                {
                                    msp.Recording = false;
                                    _mapDataManager.ClearTakenSpawnPoints(msp.MapID);
                                }
                                ImGui.PopID();
                                ImGuiUtil.ImGui_HoveredToolTip(GuiResources.SpawnPointerFinderGuiText["StopRecording"]);

                            }
                            ImGui.TableNextColumn();
                            ImGui.PushID(k++);
                            if (ImGuiComponents.IconButton(FontAwesomeIcon.SignOutAlt))
                            {
                                var exportList = new List<MapSpawnPoints>() { msp };
                                ImGui.SetClipboardText(ExportImport.Export(exportList));
                                ChangeCopyText();
                            }
                            ImGui.PopID(); ;
                            ImGuiUtil.ImGui_HoveredToolTip(_copyText);
                        }
                    }
                    ImGui.EndTable();
                }
                ImGui.EndChild();
            }
            ImGui.Separator();
            if (!_recordAll)
            {
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Video))
                {
                    _recordAll = true;
                    _mapDataManager.SpawnPointsList.ForEach(msp => msp.Recording = true);
                }
                ImGuiUtil.ImGui_HoveredToolTip(GuiResources.SpawnPointerFinderGuiText["StartRecordingAll"]);
            }
            else
            {
                if (ImGuiComponents.IconButton(FontAwesomeIcon.VideoSlash))
                {
                    _recordAll = false;
                    _mapDataManager.ClearAllTakenSpawnPoints();
                }
                ImGuiUtil.ImGui_HoveredToolTip(GuiResources.SpawnPointerFinderGuiText["StopRecordingAll"]);

            }
            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetWindowSize().X - 64 * ImGuiHelpers.GlobalScale);
            if (ImGuiComponents.IconButton(FontAwesomeIcon.SignOutAlt))
            {
                var tempList = new List<MapSpawnPoints>();
                _spawnPoints.ForEach(msp =>
                {
                    if (msp.Recording) tempList.Add(msp);
                });
                ImGui.SetClipboardText(ExportImport.Export(tempList));
                ChangeCopyText();
            }
            ImGuiUtil.ImGui_HoveredToolTip(_copyTextAllExport);
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.SignInAlt))
            {
                var importCode = ImGui.GetClipboardText();
                _mapDataManager.Import(importCode);
                ImGui.OpenPopup($"{GuiResources.SpawnPointerFinderGuiText["ImportWindowTitle"]}##modal");
            }
            ImGuiUtil.ImGui_HoveredToolTip(GuiResources.SpawnPointerFinderGuiText["ImportButtonToolTip"]);

            DrawImportModal();
            ImGui.End();
        }
    }

    private void DrawImportModal()
    {
        var center = ImGui.GetWindowPos() + ImGui.GetWindowSize() / 2;
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new Vector2(200, 150), ImGuiCond.FirstUseEver);

        if (ImGui.BeginPopupModal($"{GuiResources.SpawnPointerFinderGuiText["ImportWindowTitle"]}##modal"))
        {
            if (_importList.Count == 0)
            {
                ImGui.TextWrapped(GuiResources.SpawnPointerFinderGuiText["ErrorMessage"]);
                ImGui.Dummy(new Vector2(0, 25));
                ImGui.Dummy(new Vector2(6, 0)); ImGui.SameLine();
                if (ImGui.Button(GuiResources.SpawnPointerFinderGuiText["CloseButton"], new Vector2(80, 0))) ImGui.CloseCurrentPopup();
                return;
            }

            ImGui.TextUnformatted(GuiResources.SpawnPointerFinderGuiText["ImportMessage"]);
            ImGui.Separator(); ImGui.Dummy(Vector2.Zero);

            foreach (var msp in _importList)
            {
                ImGui.TextUnformatted(MapHelpers.GetMapName(_dataManager, msp.MapID));
            }
            ImGui.Dummy(Vector2.Zero); ImGui.Separator(); ImGui.Dummy(Vector2.Zero);

            if (ImGui.Button(GuiResources.SpawnPointerFinderGuiText["OverwriteButton"]))
            {
                _mapDataManager.ImportOverwrite();
                ImGui.CloseCurrentPopup();
            }
            ImGuiUtil.ImGui_HoveredToolTip(GuiResources.SpawnPointerFinderGuiText["OverwriteButtonToolTip"]);
            ImGui.SameLine();
            if (ImGui.Button(GuiResources.SpawnPointerFinderGuiText["ImportNewButton"]))
            {
                _mapDataManager.ImportOnlyNew();
                ImGui.CloseCurrentPopup();
            }
            ImGuiUtil.ImGui_HoveredToolTip(GuiResources.SpawnPointerFinderGuiText["ImportNewButtonToolTip"]);

            ImGui.SetCursorPosX(90);
            ImGui.SetCursorPosY(ImGui.GetCursorPos().Y + 12);
            if (ImGui.Button(GuiResources.SpawnPointerFinderGuiText["CancelButton"])) ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
        }
    }

    private void ChangeCopyText()
    {
        if (!_copyTextTask.IsCompleted) return;
        _copyTextTask = Task.Run(() =>
        {
            var temp = _copyText;
            var tempAll = _copyTextAllExport;
            _copyText = GuiResources.SpawnPointerFinderGuiText["CopyTextCopied"];
            _copyTextAllExport = GuiResources.SpawnPointerFinderGuiText["CopyTextCopied"];
            Thread.Sleep(_tooltipChangeTime);
            _copyText = temp;
            _copyTextAllExport = tempAll;
        });
    }
}