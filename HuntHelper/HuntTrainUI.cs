using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq.Expressions;
using System.Numerics;
using Dalamud.Logging;
using HuntHelper.Managers.Hunts;
using HuntHelper.Managers.Hunts.Models;
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
        if (ImGui.Begin("Hunt Train##Window", ref _huntTrainWindowVisible))
        {
            var childSize = new Vector2(ImGui.GetWindowSize().X / numOfColumns, ImGui.GetWindowSize().Y - 100);

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));

            #region Headers
            ImGui.SameLine();
            ImGui.BeginChild("NameHeader", new Vector2(ImGui.GetWindowSize().X / numOfColumns, 20), _useBorder);
            ImGui.Text("Name");
            ImGui.EndChild();
            if (_showPos)
            {
                ImGui.SameLine();
                ImGui.BeginChild("PositionHeader", new Vector2(ImGui.GetWindowSize().X / numOfColumns, 20), _useBorder);
                ImGui.Text("Position");
                ImGui.EndChild();
            }

            if (_showLastSeen)
            {
                ImGui.SameLine();
                ImGui.BeginChild("LastSeenHeader", new Vector2(ImGui.GetWindowSize().X / numOfColumns, 20), _useBorder);
                ImGui.Text("Last Seen");
                ImGui.EndChild();
            }
            ImGui.SameLine(); PluginUI.ImGui_HelpMarker("HOW TO:\n\n" +
                                      "Click on any of the rows to send the relevant Map Link into chat. (Drag to reorder the list)\n\n" +
                                      "Click on the checkbox to mark a mob as dead.\n\n" +
                                      "Right-click -- Select - change the selected mob\n" +
                                      "            -- Remove - Removes the mob from the list (This permanently deletes data on that mob)\n\n" +
                                      "Use the command \"/hhn\" to automatically mark the current selected as dead, and send the next Map link into chat.\n" +
                                      "(you will have to click the first one manually)");
            ImGui.SameLine(); ImGui.TextColored(new Vector4(1f,.3f,.3f,1f), "  <<<");
            #endregion

            ImGui.Separator();

            ImGui.BeginChild("Name", childSize);
            SelectableFromList(HuntTrainMobAttribute.Name);
            ImGui.EndChild();
            if (_showPos)
            {
                ImGui.SameLine();
                ImGui.BeginChild("Position", childSize, _useBorder);
                //moblist.ForEach(m => ImGui.TextUnformatted($"{m.Position}"));
                SelectableFromList( HuntTrainMobAttribute.Position);
                ImGui.EndChild();
            }
            if (_showLastSeen)
            {
                ImGui.SameLine();
                ImGui.BeginChild("Last seen", childSize, _useBorder);
                //moblist.ForEach(m => ImGui.TextUnformatted($"{(DateTime.Now.ToUniversalTime() - m.LastSeenUTC).TotalMinutes:0}m"));
                SelectableFromList(HuntTrainMobAttribute.LastSeen);
                ImGui.EndChild();
            }

            ImGui.SameLine();
            ImGui.BeginChild("DeadButtons", childSize, _useBorder);
            _mobList.ForEach(m =>
            {
                var d = m.Dead;
                ImGui.Checkbox($"##DeadCheckBox{m.Name}", ref d);
                ImGui.Separator();
                m.Dead = d;
                if (m.Dead && _mobList.IndexOf(m) == _selectedIndex) SelectNext();
            });
            ImGui.EndChild();

            ImGui.Separator();
            ImGui.Text("BOTTOM"+$" {_selectedIndex}");

            ImGui.PopStyleVar(); //pop itemspacing

            ImGui.End();
        }
        ImGui.PopStyleVar();//pop window padding
    }

    private int _selectedIndex = 0;
    private bool _showPos = true;
    private bool _showLastSeen = true;
    private bool _useBorder = true;
    private Vector4 _deadTextColour = new Vector4(.6f, .7f, .6f, 1f);

    //called from command, sets current mob as dead, selects next, sends flag.
    public void GetNextMobCommand()
    {
        if (_selectedIndex < _mobList.Count) _mobList[_selectedIndex].Dead = true;
        SelectNext();
        if (!_mobList[_selectedIndex].Dead) _huntManager.SendTrainFlag(_selectedIndex);
    }

    private void SelectableFromList( HuntTrainMobAttribute attributeToDisplay)
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

            if (ImGui.IsItemActive() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                _huntManager.SendTrainFlag(n);
            }
            if (ImGui.IsItemActive() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                mob.Dead = !mob.Dead;
            }

            if (ImGui.BeginPopupContextItem($"ContextMenu##{mob.Name}", ImGuiPopupFlags.MouseButtonRight))
            {
                if (ImGui.MenuItem("   Select", true)) _selectedIndex = n;
                ImGui.MenuItem("   ---", false);
                if (ImGui.MenuItem("   Remove", true)) toRemove = mob;
                ImGui.EndPopup();
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
            HuntTrainMobAttribute.Position => $"({mob.Position.X}, {mob.Position.Y})",
        };

    private void SelectNext()
    {
        var train = _huntManager.HuntTrain;

        for (; _selectedIndex < train.Count; _selectedIndex++)
        {
            if (!train[_selectedIndex].Dead) return;
        }
        _selectedIndex = 0; //if all mobs are dead, set to index 0.
    }
    private enum HuntTrainMobAttribute
    {
        Name, Position, LastSeen
    }
}