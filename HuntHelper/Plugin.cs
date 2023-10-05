using Dalamud.Data;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.FlyText;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using HuntHelper.Gui;
using HuntHelper.Managers.Hunts;
using HuntHelper.Managers.MapData;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.IO;
using Dalamud;
using Dalamud.Game.ClientState.Fates;
using Dalamud.Plugin.Services;
using HuntHelper.Gui.Resource;

namespace HuntHelper
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Hunt Helper";

        private const string MapWindowCommand = "/hh";
        private const string MapWindowPresetOne = "/hh1";
        private const string MapWindowPresetTwo = "/hh2";
        private const string MapWindowPresetOneSave = "/hh1save";
        private const string MapWindowPresetTwoSave = "/hh2save";
        private const string HuntTrainWindowCommand = "/hht";
        private const string NextHuntInTrainCommand = "/hhn";
        private const string CounterCommand = "/hhc";
        private const string SpawnPointCommand = "/hhr";
        private const string DebugCommand = "/hhdebug";

        private DalamudPluginInterface PluginInterface { get; init; }
        private ICommandManager CommandManager { get; init; }
        private Configuration Configuration { get; init; }
        private MapUI MapUi { get; init; }
        private HuntTrainUI HuntTrainUI { get; init; }
        private CounterUI CounterUI { get; init; }
        private PointerUI PointerUI { get; init; }
        private SpawnPointFinderUI SpawnPointFinderUI { get; init; }
        private IClientState ClientState { get; init; }
        private IObjectTable ObjectTable { get; init; }
        private IDataManager DataManager { get; init; }
        private IChatGui ChatGui { get; init; }
        private HuntManager HuntManager { get; init; }
        private TrainManager TrainManager { get; init; }
        private MapDataManager MapDataManager { get; init; }
        private IFlyTextGui FlyTextGui { get; init; }
        private IGameGui GameGui { get; init; }

        private IFateTable FateTable { get; init; }

        public static ICallGateSubscriber<uint, byte, bool> TeleportIpc { get; private set; }
        public static string PluginDir { get; set; } = string.Empty;

        public Plugin(
            DalamudPluginInterface pluginInterface,
            ICommandManager commandManager,
            IClientState clientState,
            IObjectTable objectTable,
            IDataManager dataManager,
            IChatGui chatGui,
            IFlyTextGui flyTextGui,
            IGameGui gameGui,
            IFateTable fateTable)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;

            PluginDir = PluginInterface.AssemblyLocation.Directory?.FullName!;

            this.ClientState = clientState;
            
            if (!GuiResources.LoadGuiText(ClientState.ClientLanguage))
            {
                PluginLog.Error("Unable to find localisation file. What did you do?! gonna crash ok");
            }

            this.FateTable = fateTable;
            this.ObjectTable = objectTable;
            this.DataManager = dataManager;
            this.ChatGui = chatGui;
            this.FlyTextGui = flyTextGui;
            this.GameGui = gameGui;

            Constants.SetCounterLanguage(ClientState.ClientLanguage);

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            this.TrainManager = new TrainManager(ChatGui, GameGui, dataManager, Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, @"Data\HuntTrain.json"));
            this.HuntManager = new HuntManager(PluginInterface, TrainManager, chatGui, flyTextGui, this.Configuration.TTSVolume);
            this.MapDataManager = new MapDataManager(Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, @"Data\SpawnPointData.json"));

            this.MapUi = new MapUI(this.Configuration, pluginInterface, clientState, objectTable, dataManager, HuntManager, MapDataManager, GameGui);
            this.HuntTrainUI = new HuntTrainUI(TrainManager, Configuration);
            this.CounterUI = new CounterUI(ClientState, ChatGui, GameGui, Configuration, ObjectTable, FateTable);
            this.SpawnPointFinderUI = new SpawnPointFinderUI(MapDataManager, DataManager, Configuration);
            this.PointerUI = new PointerUI(HuntManager, Configuration, GameGui);

            TeleportIpc = PluginInterface.GetIpcSubscriber<uint, byte, bool>("Teleport");

            this.CommandManager.AddHandler(MapWindowCommand, new CommandInfo(HuntMapCommand)
            {
                HelpMessage = GuiResources.PluginText["/hh helpmessage"]
            });
            this.CommandManager.AddHandler(MapWindowPresetOne, new CommandInfo(ApplyPresetOneCommand)
            {
                HelpMessage = GuiResources.PluginText["/hh1 helpmessage"]
            });
            this.CommandManager.AddHandler(MapWindowPresetTwo, new CommandInfo(ApplyPresetTwoCommand)
            {
                HelpMessage = GuiResources.PluginText["/hh2 helpmessage"]
            });
            this.CommandManager.AddHandler(MapWindowPresetOneSave, new CommandInfo(SavePresetOneCommand)
            {
                HelpMessage = GuiResources.PluginText["/hh1save helpmessage"]
            });
            this.CommandManager.AddHandler(MapWindowPresetTwoSave, new CommandInfo(SavePresetTwoCommand)
            {
                HelpMessage = GuiResources.PluginText["/hh2save helpmessage"]
            });
            this.CommandManager.AddHandler(HuntTrainWindowCommand, new CommandInfo(HuntTrainCommand)
            {
                HelpMessage = GuiResources.PluginText["/hht helpmessage"]
            });
            this.CommandManager.AddHandler(NextHuntInTrainCommand, new CommandInfo(GetNextMobCommand)
            {
                HelpMessage = GuiResources.PluginText["/hhn helpmessage"]
            });
#if DEBUG
            this.CommandManager.AddHandler(DebugCommand, new CommandInfo(DebugWindowCommand)
            {
                HelpMessage = "random data, debug info"
            });
#endif
            this.CommandManager.AddHandler(CounterCommand, new CommandInfo(CounterWindowCommand)
            {
                HelpMessage = GuiResources.PluginText["/hhc helpmessage"]
            });
            this.CommandManager.AddHandler(SpawnPointCommand, new CommandInfo(SpawnPointWindowCommand)
            {
                HelpMessage = GuiResources.PluginText["/hhr helpmessage"]
            });

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        }

        public void Dispose()
        {
            //save hunttrainui config first
            this.HuntTrainUI.SaveSettings();
            this.CounterUI.SaveSettings();
            this.SpawnPointFinderUI.SaveSettings();

            //this.HuntTrainUI.Dispose();
            this.SpawnPointFinderUI.Dispose();
            this.CounterUI.Dispose();
            this.MapUi.Dispose();
#if DEBUG
            this.CommandManager.RemoveHandler(DebugCommand);
#endif
            this.CommandManager.RemoveHandler(MapWindowCommand);
            this.CommandManager.RemoveHandler(MapWindowPresetOne);
            this.CommandManager.RemoveHandler(MapWindowPresetTwo);
            this.CommandManager.RemoveHandler(MapWindowPresetOneSave);
            this.CommandManager.RemoveHandler(MapWindowPresetTwoSave);
            this.CommandManager.RemoveHandler(HuntTrainWindowCommand);
            this.CommandManager.RemoveHandler(NextHuntInTrainCommand);
            this.CommandManager.RemoveHandler(CounterCommand);
            this.CommandManager.RemoveHandler(SpawnPointCommand);

            this.HuntManager.Dispose();
            // this.FlyTextGui.Dispose();
        }

        private void DebugWindowCommand(string command, string args) => this.MapUi.RandomDebugWindowVisisble = !MapUi.RandomDebugWindowVisisble;
        private void HuntMapCommand(string command, string args)
        {
            MapUi.MapVisible = !MapUi.MapVisible;
            Configuration.MapWindowVisible = MapUi.MapVisible;
        }

        private void ApplyPresetOneCommand(string command, string args) => MapUi.ApplyPreset(1);
        private void ApplyPresetTwoCommand(string command, string args) => MapUi.ApplyPreset(2);
        private void SavePresetOneCommand(string command, string args) => MapUi.SavePresetByCommand(1);
        private void SavePresetTwoCommand(string command, string args) => MapUi.SavePresetByCommand(2);
        private void HuntTrainCommand(string command, string args) => HuntTrainUI.HuntTrainWindowVisible = !HuntTrainUI.HuntTrainWindowVisible;
        //gets next available hunt in the recorded train
        private void GetNextMobCommand(string command, string args) => HuntTrainUI.GetNextMobCommand();
        private void CounterWindowCommand(string command, string args) => CounterUI.WindowVisible = !CounterUI.WindowVisible;
        private void SpawnPointWindowCommand(string command, string args) => SpawnPointFinderUI.WindowVisible = !SpawnPointFinderUI.WindowVisible;

        private void DrawUI()
        {
            try
            {
                this.MapUi.Draw();
                this.HuntTrainUI.Draw();
                this.CounterUI.Draw();
                this.SpawnPointFinderUI.Draw();
                this.PointerUI.Draw();
            }
            catch (Exception e)
            {
                PluginLog.Error(e.Message);
                if (e.StackTrace != null) PluginLog.Error(e.StackTrace);
                MapUi.MapVisible = false;
                Configuration.MapWindowVisible = false;
                HuntTrainUI.HuntTrainWindowVisible = false;
                CounterUI.WindowVisible = false;
                SpawnPointFinderUI.WindowVisible = false;
            }
        }

        private void DrawConfigUI()
        {
            this.MapUi.SettingsVisible = true;
        }


    }
}
