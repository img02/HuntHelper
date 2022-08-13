using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Logging;
using HuntHelper.Managers.Counters;
using HuntHelper.Utilities;
using ImGuiNET;

namespace HuntHelper;

public class CounterUI : IDisposable
{
    private readonly ClientState _clientState;
    private readonly ChatGui _chatGui;
    private readonly Configuration _config;
    private readonly List<CounterBase> _counters;

    private Vector2 _windowPos = new Vector2(50, 50);
    private Vector2 _windowSize = new Vector2(200, 50);

    public bool WindowVisible = false;

    public CounterUI(ClientState clientState, ChatGui chatGui, Configuration config)
    {
        _clientState = clientState;
        _chatGui = chatGui;
        _config = config;
        _counters = new List<CounterBase>()
        {
            new MinhocaoCounter()
        };
        LoadSettings();

        _chatGui.ChatMessage += chatGui_ChatMessage;
    }


    private void LoadSettings()
    {
        _windowPos = _config.CounterWindowPos;
        _windowSize = _config.CounterWindowSize;
    }

    public void SaveSettings()
    {
        _config.CounterWindowPos = _windowPos;
        _config.CounterWindowSize = _windowSize;
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
        if (ImGui.Begin("Counter", ref WindowVisible))
        {
            var counter = _counters.FirstOrDefault(c => c.MapID == _clientState.TerritoryType);
            if (counter == null) return;

            foreach (var (name, count) in counter.Tally)
            {
                ImGuiUtil.DoStuffWithMonoFont(() =>
                {
                    ImGui.TextUnformatted($"{name}: ");
                    ImGui.SameLine();
                    //ImGui.TextUnformatted($"{count:D3}");
                    ImGui.TextUnformatted($"{count}");
                });
            }
            ImGui.End();
        }
    }

    private bool _countInBackground = true;
    private void chatGui_ChatMessage(Dalamud.Game.Text.XivChatType type, uint senderId, ref Dalamud.Game.Text.SeStringHandling.SeString sender, ref Dalamud.Game.Text.SeStringHandling.SeString message, ref bool isHandled)
    {
        if (!_countInBackground) return;

        PluginLog.Warning($"?? line: " + message + $" {type}");
        if ((ushort)type is not 2874 or 2115) return; //2874 = death message?, 2115 = gather attempt

        var counter = _counters.FirstOrDefault(c => c.MapID == _clientState.TerritoryType);
        if (counter == null) return;
        counter.TryAddFromLogLine(message.ToString());
    }

    public void Dispose()
    {
        _chatGui.ChatMessage -= chatGui_ChatMessage;
    }
}