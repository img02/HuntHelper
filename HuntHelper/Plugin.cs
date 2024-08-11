using Dalamud.Game.Command;
using Dalamud.IoC;
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
        [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
        private ICommandManager CommandManager { get; init; }
        private Configuration Configuration { get; init; }
        private MapUI MapUi { get; init; }
        private HuntTrainUI HuntTrainUI { get; init; }
        private CounterUI CounterUI { get; init; }
        private PointerUI PointerUI { get; init; }
        private SpawnPointFinderUI SpawnPointFinderUI { get; init; }

        private HuntManager HuntManager { get; init; }
        private TrainManager TrainManager { get; init; }
        private MapDataManager MapDataManager { get; init; }
        private IpcSystem IpcSystem { get; init; }

        public static ICallGateSubscriber<uint, byte, bool> TeleportIpc { get; private set; }
        public static string PluginDir { get; set; } = string.Empty;

        private SubmitDataPrompt SubmitDataPrompt { get; set; }
        private DebugUI DebugUI { get; set; }


        public Plugin(
            IDalamudPluginInterface pluginInterface,
            IFramework framework,
            ICommandManager commandManager,
            IClientState clientState,
            IObjectTable objectTable,
            IDataManager dataManager,
            IChatGui chatGui,
            IFlyTextGui flyTextGui,
            IGameGui gameGui,
            IFateTable fateTable,
            IPluginLog pluginLog,
            IAetheryteList aeth)
        {
            this.CommandManager = commandManager;

            PluginLog.Logger = pluginLog;

            PluginDir = pluginInterface.AssemblyLocation.Directory?.FullName!;

            if (!GuiResources.LoadGuiText(clientState.ClientLanguage))
            {
                PluginLog.Error("Unable to find localisation file. What did you do?! gonna crash ok");
            }

            MapHelpers.SetUp(dataManager);

            Constants.SetCounterLanguage(clientState.ClientLanguage);

            Configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(pluginInterface);

            TrainManager = new TrainManager(chatGui, gameGui, Path.Combine(pluginInterface.AssemblyLocation.Directory?.FullName!, @"Data\HuntTrain.json"));
            HuntManager = new HuntManager(pluginInterface, TrainManager, chatGui, flyTextGui, Configuration.TTSVolume);
            MapDataManager = new MapDataManager(Path.Combine(pluginInterface.AssemblyLocation.Directory?.FullName!, @"Data\SpawnPointData.json"));

            MapUi = new MapUI(this.Configuration, clientState, objectTable, HuntManager, MapDataManager, gameGui);
            HuntTrainUI = new HuntTrainUI(TrainManager, Configuration);
            CounterUI = new CounterUI(clientState, chatGui, gameGui, Configuration, objectTable, fateTable);
            SpawnPointFinderUI = new SpawnPointFinderUI(MapDataManager, Configuration);
            PointerUI = new PointerUI(HuntManager, Configuration, gameGui);
            SubmitDataPrompt = new SubmitDataPrompt(Configuration);
#if DEBUG
            DebugUI = new DebugUI(aeth, dataManager, clientState, HuntManager, objectTable);
#endif

            IpcSystem = new IpcSystem(pluginInterface, framework, TrainManager, HuntManager);
            TeleportIpc = pluginInterface.GetIpcSubscriber<uint, byte, bool>("Teleport");

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

            pluginInterface.UiBuilder.Draw += DrawUI;
            pluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
            pluginInterface.UiBuilder.OpenMainUi += HuntMapCommandGetRidOfValidationMsg;
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
        }

        private void DebugWindowCommand(string command, string args) => DebugUI.RandomDebugWindowVisisble = !DebugUI.RandomDebugWindowVisisble;
        private void HuntMapCommand(string command, string args)
        {
            MapUi.MapVisible = !MapUi.MapVisible;
            Configuration.MapWindowVisible = MapUi.MapVisible;

            //todo idk delete this or comment out after dawntrail maps done
            if (MapUi.MapVisible) MapUi.CheckMapImageVer();
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
#if DEBUG
                DebugUI.Draw();
#endif

                //uhh keeping this around for future expansions
                if (Constants.NEW_EXPANSION) SubmitDataPrompt.Draw();
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
