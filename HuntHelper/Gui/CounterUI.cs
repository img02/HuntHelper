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

namespace HuntHelper.Gui;

public unsafe class CounterUI : IDisposable
{
    private readonly ClientState _clientState;
    private readonly ChatGui _chatGui;
    private readonly GameGui _gameGui;
    private readonly Configuration _config;
    private readonly ObjectTable _objectTable;
    private readonly FateTable _fateTable;
    private readonly List<CounterBase> _counters;

    private Vector2 _windowPos = new Vector2(50, 50);
    private Vector2 _windowSize = new Vector2(250, 50);
    private bool _countInBackground = true;

    public bool WindowVisible = false;

    public CounterUI(ClientState clientState, ChatGui chatGui, GameGui gameGui, Configuration config, ObjectTable objectTable, FateTable fateTable)
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
        ImGuiComponents.ToggleButton("##backgroundcounttoggle", ref _countInBackground);
        ImGuiUtil.ImGui_HoveredToolTip($"{GuiResources.CounterGuiText["BackgroundToggleToolTipInfo"]}\n" +
                                       GuiResources.CounterGuiText["BackgroundToggleToolTipStatus"] +
                                       (_countInBackground ?
                                           GuiResources.CounterGuiText["BackgroundToggleToolTipEnabled"] :
                                           GuiResources.CounterGuiText["BackgroundToggleToolTipDisabled"]));
        ImGui.SameLine();
        if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash)) counter.Reset();
        ImGuiUtil.ImGui_HoveredToolTip(GuiResources.CounterGuiText["Reset"]);

        if (_clientState.TerritoryType == (ushort)MapID.TheSeaofClouds)
        {
            ImGui.SameLine();
            ImGuiUtil.ImGui_HelpMarker("Chirp count does not affect BoP spawn, this info is just for fun.");
        }
    }

    #region Nunni - Fates - Southern Than

    private string _lastFailedFateInfo = string.Empty;
    private DateTime _startTime = DateTime.Now;
    private HashSet<Fate> _currentFates = new HashSet<Fate>();

    private readonly Vector4 _green = new Vector4(0, 1, 0, 1);
    private readonly Vector4 _red = new Vector4(1, 0, 0, 1);
    private readonly Vector4 _yellow = new Vector4(1, 1, 0, 1);

    private void DrawNunyunuwiCounter()
    {
        var endTime = _startTime.AddHours(1);

        ImGui.PushFont(UiBuilder.MonoFont);

        DrawNunWindowString(endTime);

        ImGuiUtil.ImGui_Separator(6);

        DrawNunCountdownString(endTime);
        PopulateFateList();
        UpdateCurrentFates();

        ImGuiUtil.ImGui_Separator(6);

        DrawCurrentFateUI();

        ImGuiUtil.ImGui_Separator(6);

        ImGui.TextColored(_red, _lastFailedFateInfo);
        DrawNunResetButton();

        ImGui.PopFont();
    }

    private void DrawNunWindowString(DateTime endTime)
    {
        var startTimeString = _startTime.ToString("h:mm:ss tt", CultureInfo.InvariantCulture);
        var endTimeString = endTime.ToString("h:mm:ss tt", CultureInfo.InvariantCulture);

        ImGui.Text($"{startTimeString} - {endTimeString}\n");
        ImGui.SameLine();
        Utilities.ImGuiUtil.ImGui_HelpMarker("May finish earlier based on the last failed fate before you entered zone)");
    }

    private void DrawNunCountdownString(DateTime endTime)
    {
        var countdown = endTime.Subtract(DateTime.Now);
        if (countdown.TotalSeconds < 0) countdown = TimeSpan.Zero;
        var countdownString = (countdown).ToString(@"mm\:ss", CultureInfo.InvariantCulture);

        ImGui.Text($"Countdown: ");
        ImGui.SameLine();
        if (countdown.TotalSeconds > 300) ImGui.Text($"{countdownString}");
        else ImGui.TextColored(_green, $"{countdownString}");
    }

    private void DrawNunResetButton()
    {   // not really needed tbh
        ImGuiUtil.ImGui_Separator(10);
        if (ImGui.Button("reset"))
        {
            _startTime = DateTime.Now;
            _lastFailedFateInfo = string.Empty;
        }
    }
    private void PopulateFateList()
    {
        foreach (var fate in _fateTable)
        {
            if (_currentFates.Add(fate))
            {
                _currentFates = new HashSet<Fate>(_currentFates.OrderBy(f => f.TimeRemaining > 0 ? f.TimeRemaining : long.MaxValue));
            }
        }

#if DEBUG
        if (ImGui.CollapsingHeader("DEBUG FATE INFO"))
        {
            ImGui.Text($"{"Name",-35} -> {"Dura",4} | " +
                       $"{"Rem.",5} | " +
                       $"{"Prog",4} : {"State",12} | " +
                       $"{"Start",10} | " +
                       $"{"ID",4} | " +
                       $"{"Pos"}");
            foreach (var fate in _fateTable)
            {
                ImGui.Text($"{fate.Name,-35} -> {fate.Duration,4} | " +
                           $"{new TimeSpan(0, 0, 0, (int)(fate.TimeRemaining - 7)).ToString(@"mm\:ss"),4} | " +
                           $"{fate.Progress,4} : {fate.State,12} | " +
                           $"{fate.StartTimeEpoch,10} | " +
                           $"{fate.FateId,4} | " +
                           $"{fate.Position}");
            }
        }
#endif
    }

    private void UpdateCurrentFates()
    {
        foreach (var cf in _currentFates)
        {
            if (cf.State == FateState.Ended) _currentFates.Remove(cf);
            if (cf.State == FateState.Failed)
            {
                _lastFailedFateInfo = $"FAIL\n" +
                                      $"{cf.Name} @ {cf.Progress}%%\n\n" +
                                      $"> {DateTime.Now.ToString("hh:mm:ss tt", CultureInfo.InvariantCulture)} <\n\n" +
                                      $"Countdown reset.";
                _currentFates.Remove(cf);
                _startTime = DateTime.Now;
            }
        }
    }

    private void DrawCurrentFateUI()
    {
        if (ImGui.CollapsingHeader("Fates"))
        {
            foreach (var cf in _currentFates)
            {
                DrawFateInfoUI(cf);
            }
        }
        ImGuiUtil.ImGui_HoveredToolTip("Click a Fate to see it on the map.");
    }
    private void DrawFateInfoUI(Fate cf)
    {
        var fateText = GetFateInfoString(cf);
        var fateTime = GetFateTimeString(cf);

        ImGui.TextUnformatted(fateText);
        if (ImGui.IsItemClicked()) OpenMapOnFateClick(cf);
        Utilities.ImGuiUtil.ImGui_HoveredToolTip($"{cf.Name}");

        ImGui.SameLine();

        switch (cf.TimeRemaining)
        {
            case > 0 and <= 67:
                ImGui.TextColored(_red, fateTime);
                break;
            case > 67 and < 127:
                ImGui.TextColored(_yellow, fateTime);
                break;
            default:
                ImGui.TextUnformatted(fateTime);
                break;
        }
        if (ImGui.IsItemClicked()) OpenMapOnFateClick(cf);
    }

    private string GetFateInfoString(Fate fate)
    {
        var fateName = fate.Name.ToString();
        if (fateName.Length > 16) fateName = fateName.Substring(0, 14) + "..";
        return $"{fateName,-16} {fate.Progress,3}%  -> ";
    }

    private string GetFateTimeString(Fate fate)
    {
        return fate.State != FateState.Preparation
            ? $"{new TimeSpan(0, 0, 0, (int)(fate.TimeRemaining - 7)).ToString(@"mm\:ss", CultureInfo.InvariantCulture)}"
            : "upcoming or requires activation - can fail if not activated in time";
    }

    private void OpenMapOnFateClick(Fate fate)
    {
        try
        {
            var ag = AgentMap.Instance();
            var mapId = ag->CurrentMapId;
            var territoryId = ag->CurrentTerritoryId;
            var pos = Dalamud.Utility.MapUtil.WorldToMap(fate.Position, ag->CurrentOffsetX, ag->CurrentOffsetY);

            var mapLink = new MapLinkPayload(territoryId, mapId, pos.X, pos.Y);
            _gameGui.OpenMapWithMapLink(mapLink);
        }
        catch (Exception e)
        {
            PluginLog.Error(e.ToString());
        }
    }
    private void ClientState_TerritoryChanged(object? sender, ushort e)
    {
        _currentFates.Clear();
        _startTime = DateTime.Now;
        _lastFailedFateInfo = string.Empty;

        Task.Run(() =>
        {
            Thread.Sleep(1000);
            return _currentFates =
                new HashSet<Fate>(_currentFates.OrderBy(f =>
                    f.TimeRemaining > 0 ? f.TimeRemaining : Int64.MaxValue));
        });
    }

    #endregion

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

    public void Dispose()
    {
        _chatGui.ChatMessage -= ChatGui_ChatMessage;
        _clientState.TerritoryChanged -= ClientState_TerritoryChanged;
    }
}