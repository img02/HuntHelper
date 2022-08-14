using System;
using System.Collections.Generic;
using System.Numerics;
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


    public bool WindowVisible = false;
    public SpawnPointFinderUI(MapDataManager mapDataManager, Configuration config)
    {
        _mapDataManager = mapDataManager;
        _config = config;
        _spawnPoints = _mapDataManager.SpawnPointsList;
        LoadSettings();
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
                    foreach (var sp in _spawnPoints)
                    {
                        if (_filter.PassFilter(sp.MapName))
                        {
                            ImGui.TableNextColumn();
                            ImGui.TextUnformatted($"{sp.MapName}");
                            ImGui.TableNextColumn();
                            if (!sp.Recording)
                            {
                                if (ImGuiComponents.IconButton(i++, FontAwesomeIcon.Video)) sp.Recording = true;
                                ImGuiUtil.ImGui_HoveredToolTip("Start Recording");
                            }
                            else
                            {
                                if (ImGuiComponents.IconButton(j--, FontAwesomeIcon.VideoSlash))
                                {
                                    sp.Recording = false;
                                    _mapDataManager.ClearTakenSpawnPoints(sp.MapID);
                                }
                                ImGuiUtil.ImGui_HoveredToolTip("Stop Recording -- This will WIPE all data.");
                               
                            } 
                            ImGui.TableNextColumn();
                            ImGuiComponents.IconButton(FontAwesomeIcon.SignOutAlt);
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
            ImGuiComponents.IconButton(FontAwesomeIcon.SignOutAlt);
            ImGui.SameLine();
            ImGuiComponents.IconButton(FontAwesomeIcon.SignInAlt);
            ImGui.SameLine();
            ImGui.Text($"{ImGui.GetWindowSize()}");

            ImGui.End();
        }
    }
}