using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Fates;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HuntHelper.Gui.Resource;
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
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;

namespace HuntHelper.Gui;

public unsafe partial class CounterUI : IDisposable
{
    private readonly IClientState _clientState;
    private readonly IChatGui _chatGui;
    private readonly IGameGui _gameGui;
    private readonly Configuration _config;
    private readonly IObjectTable _objectTable;
    private readonly IFateTable _fateTable;
    private readonly List<CounterBase> _counters;

    private Vector2 _windowPos = new Vector2(50, 50);
    private Vector2 _windowSize = new Vector2(250, 50);
    private bool _countInBackground = true;

    public bool WindowVisible = false;

    public CounterUI(IClientState clientState, IChatGui chatGui, IGameGui gameGui, Configuration config, IObjectTable objectTable, IFateTable fateTable)
    {
        _clientState = clientState;
        _chatGui = chatGui;
        _gameGui = gameGui;
        _config = config;
        _objectTable = objectTable;
        _fateTable = fateTable;
        _counters = new List<CounterBase>()
        {
            new MinhocaoCounter(),
            new SquonkCounter(),
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

        _chatGui.ChatMessage += ChatGui_ChatMessage;
        _clientState.TerritoryChanged += ClientState_TerritoryChanged;
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
        if (_clientState.TerritoryType == (ushort)MapID.SouthernThanalan)
            FateUpdateLoop();

        if (!WindowVisible) return;

        ImGui.SetNextWindowSize(_windowSize, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(_windowPos, ImGuiCond.FirstUseEver);
        if (ImGui.Begin(GuiResources.CounterGuiText["MainWindowTitle"], ref WindowVisible, ImGuiWindowFlags.NoScrollbar))
        {
            Action counter = _clientState.TerritoryType switch
            {
                (ushort)MapID.UltimaThule => DrawWeeEaCounter,
                (ushort)MapID.SouthernThanalan => DrawNunyunuwiCounter,
                _ => DrawCounter,
            };

            counter();
            ImGui.End();
        }
    }

    private void DrawCounter()
    {
        var counter = _counters.FirstOrDefault(c => c.MapID == _clientState.TerritoryType);
        if (counter == null) return;

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

        DrawBackgroundToggleButton();

        ImGui.SameLine();
        if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash)) counter.Reset();
        ImGuiUtil.ImGui_HoveredToolTip(GuiResources.CounterGuiText["Reset"]);

        if (_clientState.TerritoryType == (ushort)MapID.TheSeaofClouds)
        {
            ImGui.SameLine();
            ImGuiUtil.ImGui_HelpMarker("Chirp count does not affect BoP spawn, this info is just for fun.");
        }
    }

    #region Narrow-rift - Wee Ea - Ultima Thule

    private void DrawWeeEaCounter()
    {
        var count = _objectTable.Count(obj => obj.DataId == Constants.WeeEa);

        if (ImGui.BeginTable("CounterTable", 2, ImGuiTableFlags.Borders))
        {
            ImGui.TableSetupColumn("name", ImGuiTableColumnFlags.None, ImGui.GetWindowPos().X * (3.0f / 4.0f));
            ImGui.TableSetupColumn("count", ImGuiTableColumnFlags.None, ImGui.GetWindowPos().X / 4.0f);

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(GuiResources.CounterGuiText["WeeEa"]);
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{count}");

            ImGui.EndTable();
        }
    }

    #endregion

    private void ChatGui_ChatMessage(XivChatType type, uint senderId, ref Dalamud.Game.Text.SeStringHandling.SeString sender, ref Dalamud.Game.Text.SeStringHandling.SeString message, ref bool isHandled)
    {
        if (!_countInBackground && !WindowVisible) return;

        //PluginLog.Warning($"?? line: " + message + $" {type}");
        if ((ushort)type is not 2874 and not 2115 and not 17210 and not 57 and not 10283) return; //2874 = you killed, 2115 = gather attempt, 17210 = chocobo killed owo, 10283 = Squonk uses Chirp
        var counter = _counters.FirstOrDefault(c => c.MapID == _clientState.TerritoryType);
        if (counter == null) return;
        counter.TryAddFromLogLine(message.ToString());
    }

    private void DrawBackgroundToggleButton()
    {
        ImGuiComponents.ToggleButton("##backgroundcounttoggle", ref _countInBackground);
        ImGuiUtil.ImGui_HoveredToolTip($"{GuiResources.CounterGuiText["BackgroundToggleToolTipInfo"]}\n" +
                                       GuiResources.CounterGuiText["BackgroundToggleToolTipStatus"] +
                                       (_countInBackground ?
                                           GuiResources.CounterGuiText["BackgroundToggleToolTipEnabled"] :
                                           GuiResources.CounterGuiText["BackgroundToggleToolTipDisabled"]));
    }
    public void Dispose()
    {
        _chatGui.ChatMessage -= ChatGui_ChatMessage;
        _clientState.TerritoryChanged -= ClientState_TerritoryChanged;
    }
}