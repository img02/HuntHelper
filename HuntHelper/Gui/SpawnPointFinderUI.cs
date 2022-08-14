using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Logging;
using HuntHelper.Managers.MapData;
using HuntHelper.Managers.MapData.Models;
using HuntHelper.Utilities;
using ImGuiNET;

namespace HuntHelper.Gui;

public unsafe class SpawnPointFinderUI : IDisposable//idk what to call this
{
    private readonly MapDataManager _mapDataManager;
    private readonly Configuration _config;
    private readonly List<MapSpawnPoints> _spawnPoints;
    private ImGuiTextFilterPtr _filter;

    private bool _recordAll = false;
    private readonly List<MapSpawnPoints> _importList;
    private Task _copyTextTask = Task.CompletedTask;
    private string _copyText = "Copy export code clipboard";
    private string _copyTextAllExport = "Export all currently recorded map data";
    private int _tooltipChangeTime = 400;

    public bool WindowVisible = false;

    public SpawnPointFinderUI(MapDataManager mapDataManager, Configuration config)
    {
        _mapDataManager = mapDataManager;
        _config = config;
        _spawnPoints = _mapDataManager.SpawnPointsList;
        LoadSettings();
        _importList = _mapDataManager.ImportedList;
        var filterPtr = ImGuiNative.ImGuiTextFilter_ImGuiTextFilter(null);
        _filter = new ImGuiTextFilterPtr(filterPtr);
    }

    public void Draw()
    {
        DrawSpawnRefinementWindow();
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

    public void DrawSpawnRefinementWindow()
    {
        if (!WindowVisible) return;

        ImGui.SetNextWindowSize(new Vector2(250, 540), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new Vector2(50, 50), ImGuiCond.FirstUseEver);

        if (ImGui.Begin("Spawn Point Refinement##", ref WindowVisible, ImGuiWindowFlags.NoScrollbar))
        {
            _filter.Draw("Search by name", 120);
            ImGui.Dummy(Vector2.Zero);
            if (ImGui.BeginChild("sp table child##", new Vector2(ImGui.GetWindowSize().X, ImGui.GetWindowSize().Y * .82f), false, ImGuiWindowFlags.NoScrollbar))
            {
                if (ImGui.BeginTable("sp table##", 3, ImGuiTableFlags.BordersH))
                {
                    ImGui.TableSetupColumn("MapName", ImGuiTableColumnFlags.WidthStretch, 50);
                    ImGui.TableSetupColumn("MapID", ImGuiTableColumnFlags.WidthFixed, 25);
                    ImGui.TableSetupColumn("Buttons", ImGuiTableColumnFlags.WidthFixed, 40);

                    var i = 1;
                    var j = -1;
                    var k = 100;
                    foreach (var msp in _spawnPoints)
                    {
                        if (_filter.PassFilter(msp.MapName))
                        {
                            ImGui.TableNextColumn();
                            ImGui.TextUnformatted($"{msp.MapName}");
                            ImGui.TableNextColumn();
                            if (!msp.Recording)
                            {
                                if (ImGuiComponents.IconButton(i++, FontAwesomeIcon.Video)) msp.Recording = true;
                                ImGuiUtil.ImGui_HoveredToolTip("Start Recording");
                            }
                            else
                            {
                                if (ImGuiComponents.IconButton(j--, FontAwesomeIcon.VideoSlash))
                                {
                                    msp.Recording = false;
                                    _mapDataManager.ClearTakenSpawnPoints(msp.MapID);
                                }
                                ImGuiUtil.ImGui_HoveredToolTip("Stop Recording -- This will WIPE all data.");
                               
                            } 
                            ImGui.TableNextColumn();
                            if (ImGuiComponents.IconButton( k++,FontAwesomeIcon.SignOutAlt))
                            {
                                var exportList = new List<MapSpawnPoints>(){msp};
                                ImGui.SetClipboardText(ExportImport.Export(exportList));
                                ChangeCopyText();
                            }
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
                if (ImGuiComponents.IconButton(999, FontAwesomeIcon.Video))
                {
                    _recordAll = true;
                    _mapDataManager.SpawnPointsList.ForEach(msp => msp.Recording = true);
                }
                ImGuiUtil.ImGui_HoveredToolTip("Start Recording All");
            }
            else
            {
                if (ImGuiComponents.IconButton(-999, FontAwesomeIcon.VideoSlash))
                {
                    _recordAll = false;
                    _mapDataManager.ClearAllTakenSpawnPoints();
                }
                ImGuiUtil.ImGui_HoveredToolTip("Stop Recording All -- This will WIPE all data.");
                
            }
            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetWindowSize().X - 64);
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
               ImGui.OpenPopup("importmodal");
            }
            ImGuiUtil.ImGui_HoveredToolTip("Import map data");
            
            DrawImportModal();
            ImGui.End();
        }
    }

    private void DrawImportModal()
    {
        var center = ImGui.GetWindowPos() + ImGui.GetWindowSize() / 2;
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new Vector2(200, 150), ImGuiCond.FirstUseEver);

        if (ImGui.BeginPopupModal("importmodal"))
        {
            if (_importList.Count == 0)
            {
                PluginLog.Error($"{_mapDataManager.ImportedList.Count}");
                ImGui.TextWrapped("Nothing to import - or incorrect code");
                ImGui.Dummy(new Vector2(0, 25));
                ImGui.Dummy(new Vector2(6, 0)); ImGui.SameLine();
                if (ImGui.Button("Close", new Vector2(80, 0))) ImGui.CloseCurrentPopup();
                return;
            }

            ImGui.TextUnformatted("Imported Maps:");
            ImGui.Separator(); ImGui.Dummy(Vector2.Zero);

            foreach (var msp in _importList)
            {
                ImGui.TextUnformatted(msp.MapName);
            }
            ImGui.Dummy(Vector2.Zero); ImGui.Separator(); ImGui.Dummy(Vector2.Zero);

            if (ImGui.Button("Import"))
            {
                _mapDataManager.ImportAll();
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel")) ImGui.CloseCurrentPopup();
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
            _copyText = "Copied!";
            _copyTextAllExport  = "copied";
            Thread.Sleep(_tooltipChangeTime);
            _copyText = temp;
            _copyTextAllExport = tempAll;
        });
    }

}