using Dalamud.Game.Command;
using Dalamud.Interface;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using HuntHelper.Gui;
using HuntHelper.Gui.Resource;
using HuntHelper.Managers;
using HuntHelper.Managers.Hunts;
using HuntHelper.Managers.MapData;
using HuntHelper.Managers.NewExpansion;
using HuntHelper.Utilities;
using System;
using System.IO;

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
        private const string NextHuntInTrainAetheryteCommand = "/hhna";
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
        private IpcSystem IpcSystem { get; init; }
        private IFlyTextGui FlyTextGui { get; init; }
        private IGameGui GameGui { get; init; }

        private IFateTable FateTable { get; init; }

        public static ICallGateSubscriber<uint, byte, bool> TeleportIpc { get; private set; }
        public static string PluginDir { get; set; } = string.Empty;

        private SubmitDataPrompt SubmitDataPrompt { get; set; }


        public Plugin(
            DalamudPluginInterface pluginInterface,
            IFramework framework,
            ICommandManager commandManager,
            IClientState clientState,
            IObjectTable objectTable,
            IDataManager dataManager,
            IChatGui chatGui,
            IFlyTextGui flyTextGui,
            IGameGui gameGui,
            IFateTable fateTable,
            IPluginLog pluginLog)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;

            PluginLog.Logger = pluginLog;

            PluginDir = PluginInterface.AssemblyLocation.Directory?.FullName!;

            this.ClientState = clientState;

            if (!GuiResources.LoadGuiText(ClientState.ClientLanguage))
            {
                PluginLog.Error("Unable to find localisation file. What did you do?! gonna crash ok");
            }

            FateTable = fateTable;
            ObjectTable = objectTable;
            DataManager = dataManager;
            ChatGui = chatGui;
            FlyTextGui = flyTextGui;
            GameGui = gameGui;

            MapHelpers.SetUp(DataManager);

            Constants.SetCounterLanguage(ClientState.ClientLanguage);

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);

            TrainManager = new TrainManager(ChatGui, GameGui, Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, @"Data\HuntTrain.json"));
            HuntManager = new HuntManager(PluginInterface, TrainManager, chatGui, flyTextGui, Configuration.TTSVolume);
            MapDataManager = new MapDataManager(Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, @"Data\SpawnPointData.json"));

            MapUi = new MapUI(this.Configuration, ClientState, ObjectTable, HuntManager, MapDataManager, GameGui);
            HuntTrainUI = new HuntTrainUI(TrainManager, Configuration);
            CounterUI = new CounterUI(ClientState, ChatGui, GameGui, Configuration, ObjectTable, FateTable);
            SpawnPointFinderUI = new SpawnPointFinderUI(MapDataManager, Configuration);
            PointerUI = new PointerUI(HuntManager, Configuration, GameGui);
            SubmitDataPrompt = new SubmitDataPrompt(Configuration);

            IpcSystem = new IpcSystem(pluginInterface, framework, TrainManager);
            TeleportIpc = PluginInterface.GetIpcSubscriber<uint, byte, bool>("Teleport");
                        
            CommandManager.AddHandler(MapWindowCommand, new CommandInfo(HuntMapCommand) { HelpMessage = GuiResources.PluginText["/hh helpmessage"] });
            CommandManager.AddHandler(MapWindowPresetOne, new CommandInfo(ApplyPresetOneCommand) { HelpMessage = GuiResources.PluginText["/hh1 helpmessage"] });
            CommandManager.AddHandler(MapWindowPresetTwo, new CommandInfo(ApplyPresetTwoCommand) { HelpMessage = GuiResources.PluginText["/hh2 helpmessage"] });
            CommandManager.AddHandler(MapWindowPresetOneSave, new CommandInfo(SavePresetOneCommand) { HelpMessage = GuiResources.PluginText["/hh1save helpmessage"] });
            CommandManager.AddHandler(MapWindowPresetTwoSave, new CommandInfo(SavePresetTwoCommand) { HelpMessage = GuiResources.PluginText["/hh2save helpmessage"] });
            CommandManager.AddHandler(HuntTrainWindowCommand, new CommandInfo(HuntTrainCommand) { HelpMessage = GuiResources.PluginText["/hht helpmessage"] });
            CommandManager.AddHandler(NextHuntInTrainCommand, new CommandInfo(GetNextMobCommand) { HelpMessage = GuiResources.PluginText["/hhn helpmessage"] });
            CommandManager.AddHandler(NextHuntInTrainAetheryteCommand, new CommandInfo(GetNextMobAetheryteCommand) { HelpMessage = GuiResources.PluginText["/hhna helpmessage"] }); //todo replace msg
#if DEBUG
            CommandManager.AddHandler(DebugCommand, new CommandInfo(DebugWindowCommand) { HelpMessage = "random data, debug info" });
#endif
            CommandManager.AddHandler(CounterCommand, new CommandInfo(CounterWindowCommand) { HelpMessage = GuiResources.PluginText["/hhc helpmessage"] });
            CommandManager.AddHandler(SpawnPointCommand, new CommandInfo(SpawnPointWindowCommand) { HelpMessage = GuiResources.PluginText["/hhr helpmessage"] });

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
            PluginInterface.UiBuilder.OpenMainUi += HuntMapCommandGetRidOfValidationMsg;
        }

        public void Dispose()
        {
            //save hunttrainui config first
            HuntTrainUI.SaveSettings();
            CounterUI.SaveSettings();
            SpawnPointFinderUI.SaveSettings();

            IpcSystem.Dispose();
            HuntTrainUI.Dispose();
            SpawnPointFinderUI.Dispose();
            CounterUI.Dispose();
            MapUi.Dispose();
#if DEBUG
            CommandManager.RemoveHandler(DebugCommand);
#endif
            CommandManager.RemoveHandler(MapWindowCommand);
            CommandManager.RemoveHandler(MapWindowPresetOne);
            CommandManager.RemoveHandler(MapWindowPresetTwo);
            CommandManager.RemoveHandler(MapWindowPresetOneSave);
            CommandManager.RemoveHandler(MapWindowPresetTwoSave);
            CommandManager.RemoveHandler(HuntTrainWindowCommand);
            CommandManager.RemoveHandler(NextHuntInTrainCommand);
            CommandManager.RemoveHandler(CounterCommand);
            CommandManager.RemoveHandler(SpawnPointCommand);

            HuntManager.Dispose();
            TrainManager.Dispose();
            SubmitDataPrompt.Dispose();
        }

        private void DebugWindowCommand(string command, string args) => this.MapUi.RandomDebugWindowVisisble = !MapUi.RandomDebugWindowVisisble;
        private void HuntMapCommand(string command, string args)
        {
            MapUi.MapVisible = !MapUi.MapVisible;
            Configuration.MapWindowVisible = MapUi.MapVisible;
        }
        private void HuntMapCommandGetRidOfValidationMsg()
        {
            MapUi.MapVisible = !MapUi.MapVisible;
            Configuration.MapWindowVisible = MapUi.MapVisible;
        }

        private void ApplyPresetOneCommand(string _, string __) => MapUi.ApplyPreset(1);
        private void ApplyPresetTwoCommand(string _, string __) => MapUi.ApplyPreset(2);
        private void SavePresetOneCommand(string _, string __) => MapUi.SavePresetByCommand(1);
        private void SavePresetTwoCommand(string _, string __) => MapUi.SavePresetByCommand(2);
        private void HuntTrainCommand(string _, string __) => HuntTrainUI.HuntTrainWindowVisible = !HuntTrainUI.HuntTrainWindowVisible;
        //gets next available hunt in the recorded train
        private void GetNextMobCommand(string _, string __) => HuntTrainUI.GetNextMobCommand();
        private void GetNextMobAetheryteCommand(string _, string __) => HuntTrainUI.GetNextMobNearestAetheryte();
        private void CounterWindowCommand(string _, string __) => CounterUI.WindowVisible = !CounterUI.WindowVisible;
        private void SpawnPointWindowCommand(string _, string __) => SpawnPointFinderUI.WindowVisible = !SpawnPointFinderUI.WindowVisible;

        private void DrawUI()
        {
            try
            {
                MapUi.Draw();
                HuntTrainUI.Draw();
                CounterUI.Draw();
                SpawnPointFinderUI.Draw();
                PointerUI.Draw();
                SubmitDataPrompt.Draw();
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
            MapUi.SettingsVisible = true;
        }


    }
}
