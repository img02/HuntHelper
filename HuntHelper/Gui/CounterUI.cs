using Dalamud.Game.ClientState;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using HuntHelper.Managers.Counters;
using HuntHelper.Managers.Counters.ARR;
using HuntHelper.Managers.Counters.EW;
using HuntHelper.Managers.Counters.HW;
using HuntHelper.Managers.Counters.SB;
using HuntHelper.Managers.Counters.ShB;
using HuntHelper.Utilities;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;

namespace HuntHelper.Gui;

public class CounterUI : IDisposable
{
    private readonly ClientState _clientState;
    private readonly ChatGui _chatGui;
    private readonly Configuration _config;
    private readonly ObjectTable _objectTable;
    private readonly List<CounterBase> _counters;

    private Vector2 _windowPos = new Vector2(50, 50);
    private Vector2 _windowSize = new Vector2(250, 50);
    private bool _countInBackground = true;

    public bool WindowVisible = false;

    public CounterUI(ClientState clientState, ChatGui chatGui, Configuration config, ObjectTable objectTable)
    {
        _clientState = clientState;
        _chatGui = chatGui;
        _config = config;
        _objectTable = objectTable;
        _counters = new List<CounterBase>()
        {
            new MinhocaoCounter(),
            new LeucrottaCounter(),
            new GandaweraCounter(),
            new OkinaCounter(),
            new UdumbaraCounter(),
            new SaltAndLightCounter(),
            new ForgivenPedantryCounter(),
            new IxtabCounter(),
            new SphatikaCounter(),
            new RuinatorCounter()
        };
        LoadSettings();

        _chatGui.ChatMessage += chatGui_ChatMessage;
    }


    private void LoadSettings()
    {
        _windowPos = _config.CounterWindowPos;
        _windowSize = _config.CounterWindowSize;
        _countInBackground = _config.CountInBackground;
    }

    public void SaveSettings()
    {
        _config.CounterWindowPos = _windowPos;
        _config.CounterWindowSize = _windowSize;
        _config.CountInBackground = _countInBackground;
    }

    public void Draw()
    {
        DrawCountWindow();
    }

    public void DrawCountWindow()
    {
        if (!WindowVisible) return;

        ImGui.SetNextWindowSize(_windowSize, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(_windowPos, ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Counter", ref WindowVisible, ImGuiWindowFlags.NoScrollbar))
        {
            if (_clientState.TerritoryType == (ushort)MapID.UltimaThule) DrawWeeEaCounter();//todo

                var counter = _counters.FirstOrDefault(c => c.MapID == _clientState.TerritoryType);
                if (counter == null) {DrawWeeEaCounter();return;}

            if (ImGui.BeginTable("CounterTable", 2, ImGuiTableFlags.Borders))
            {
                ImGui.TableSetupColumn("name", ImGuiTableColumnFlags.None, ImGui.GetWindowPos().X * (3.0f / 4.0f));
                ImGui.TableSetupColumn("count", ImGuiTableColumnFlags.None, ImGui.GetWindowPos().X / 4.0f);
                foreach (var (name, count) in counter.Tally)
                {
                    ImGuiUtil.DoStuffWithMonoFont(() =>
                    {
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"{name}: ");
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"{count}");
                    });
                }
                ImGui.EndTable();
            }
            ImGui.Dummy(Vector2.Zero);
            ImGuiComponents.ToggleButton("##backgroundcounttoggle", ref _countInBackground);
            ImGuiUtil.ImGui_HoveredToolTip("Allow counting when window closed.\n" +
                                           "Status: " +
                                           (_countInBackground ? "Enabled" : "Disabled"));
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash)) counter.Reset();
            ImGuiUtil.ImGui_HoveredToolTip("Reset");


            ImGui.End();
        }
    }

    private void DrawWeeEaCounter()
    {
        foreach (var obj in _objectTable)
        {
            if (obj.Name.ToString() == "Wee Ea")
            {
                ImGui.Text($"{obj}");
            }
        }
    }

    private void chatGui_ChatMessage(XivChatType type, uint senderId, ref Dalamud.Game.Text.SeStringHandling.SeString sender, ref Dalamud.Game.Text.SeStringHandling.SeString message, ref bool isHandled)
    {
        if (!_countInBackground && !WindowVisible) return;

        //PluginLog.Warning($"?? line: " + message + $" {type}");
        if ((ushort)type is not 2874 and not 2115 and not 17210 and not 57) return; //2874 = you killed, 2115 = gather attempt, 17210 = chocobo killed owo, 
        var counter = _counters.FirstOrDefault(c => c.MapID == _clientState.TerritoryType);
        if (counter == null) return;
        counter.TryAddFromLogLine(message.ToString());
    }

    public void Dispose()
    {
        _chatGui.ChatMessage -= chatGui_ChatMessage;
    }
}