using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using HuntHelper.Managers.MapData;
using HuntHelper.Managers.MapData.Models;
using ImGuiNET;

namespace HuntHelper.Gui;

public unsafe class SpawnPointFinderUI : IDisposable//idk what to call this
{
    private readonly MapDataManager _mapDataManager;
    private readonly List<MapSpawnPoints> _spawnPoints;
    private ImGuiTextFilterPtr _filter;


    public bool WindowVisible = false;
    public SpawnPointFinderUI(MapDataManager mapDataManager)
    {
        _mapDataManager = mapDataManager;
        _spawnPoints = _mapDataManager.SpawnPointsList;

        var filterPtr = ImGuiNative.ImGuiTextFilter_ImGuiTextFilter(null);
        _filter = new ImGuiTextFilterPtr(filterPtr);
    }


    public void Draw()
    {
        DrawSpawnRefinementWindow();
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

        if (ImGui.Begin("Macrodata Refinement##", ref WindowVisible, ImGuiWindowFlags.NoScrollbar))
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

                    foreach (var sp in _spawnPoints)
                    {
                        if (_filter.PassFilter(sp.MapName))
                        {
                            ImGui.TableNextColumn();
                            ImGui.TextUnformatted($"{sp.MapName}");
                            ImGui.TableNextColumn();
                            ImGuiComponents.IconButton(FontAwesomeIcon.Video);
                            ImGui.SameLine();
                            ImGuiComponents.IconButton(FontAwesomeIcon.VideoSlash);
                            ImGui.TableNextColumn();
                            ImGuiComponents.IconButton(FontAwesomeIcon.SignOutAlt);
                        }
                    }

                    ImGui.EndTable();
                }
                ImGui.EndChild();
            }
            ImGui.Separator();
            ImGuiComponents.IconButton(FontAwesomeIcon.SignOutAlt);
            ImGui.SameLine();
            ImGuiComponents.IconButton(FontAwesomeIcon.SignInAlt);
            ImGui.SameLine();
            ImGui.Text($"{ImGui.GetWindowSize()}");

            ImGui.End();
        }

    }
}