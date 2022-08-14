using Dalamud.Data;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.FlyText;
using Dalamud.Plugin;
using HuntHelper.Gui;
using HuntHelper.Managers.Hunts;
using System.IO;
using HuntHelper.Managers.MapData;

namespace HuntHelper
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Hunt Helper";

        private const string MapWindowCommand = "/hh";
        private const string HuntTrainWindowCommand = "/hht";
        private const string NextHuntInTrainCommand = "/hhn";
        private const string CounterCommand = "/hhc";
        private const string SpawnPointCommand = "/hhr";
        private const string DebugCommand = "/hhdebug";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        private Configuration Configuration { get; init; }
        private PluginUI PluginUi { get; init; }
        private HuntTrainUI HuntTrainUI { get; init; }
        private CounterUI CounterUI { get; init; }
        private SpawnPointFinderUI SpawnPointFinderUI { get; init; }
        private ClientState ClientState { get; init; }
        private ObjectTable ObjectTable { get; init; }
        private DataManager DataManager { get; init; }
        private ChatGui ChatGui { get; init; }
        private HuntManager HuntManager { get; init; }
        private TrainManager TrainManager { get; init; }
        private MapDataManager MapDataManager { get; init; }
        private FlyTextGui FlyTextGui { get; init; }

        public Plugin(
            DalamudPluginInterface pluginInterface,
            CommandManager commandManager,
            ClientState clientState,
            ObjectTable objectTable,
            DataManager dataManager,
            ChatGui chatGui,
            FlyTextGui flyTextGui)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;

            this.ClientState = clientState;
            this.ObjectTable = objectTable;
            this.DataManager = dataManager;
            this.ChatGui = chatGui;
            this.FlyTextGui = flyTextGui;

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            this.TrainManager = new TrainManager(ChatGui, Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, @"Data\HuntTrain.json"));
            this.HuntManager = new HuntManager(PluginInterface, TrainManager, chatGui, flyTextGui);
            this.MapDataManager = new MapDataManager(Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, @"Data\SpawnPointData.json"));

            this.PluginUi = new PluginUI(this.Configuration, pluginInterface, clientState, objectTable, dataManager, HuntManager, MapDataManager);
            this.HuntTrainUI = new HuntTrainUI(TrainManager, Configuration);
            this.CounterUI = new CounterUI(ClientState, ChatGui, Configuration);
            this.SpawnPointFinderUI = new SpawnPointFinderUI(MapDataManager, Configuration);

            this.CommandManager.AddHandler(MapWindowCommand, new CommandInfo(HuntMapCommand)
            {
                HelpMessage = "Opens the Hunt Map"
            });
            this.CommandManager.AddHandler(HuntTrainWindowCommand, new CommandInfo(HuntTrainCommand)
            {
                HelpMessage = "Opens the Hunt Train Window"
            });
            this.CommandManager.AddHandler(NextHuntInTrainCommand, new CommandInfo(GetNextMobCommand)
            {
                HelpMessage = "Sends the flag for the next train mob into chat"
            });
            this.CommandManager.AddHandler(DebugCommand, new CommandInfo(DebugWindowCommand)
            {
                HelpMessage = "random data, debug info"
            });
            this.CommandManager.AddHandler(CounterCommand, new CommandInfo(CounterWindowCommand)
            {
                HelpMessage = "random data, debug info"
            });
            this.CommandManager.AddHandler(SpawnPointCommand, new CommandInfo(SpawnPointWindowCommand)
            {
                HelpMessage = "spawn point refinement window"
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
            this.PluginUi.Dispose();
            this.CommandManager.RemoveHandler(DebugCommand);
            this.CommandManager.RemoveHandler(MapWindowCommand);
            this.CommandManager.RemoveHandler(HuntTrainWindowCommand);
            this.CommandManager.RemoveHandler(NextHuntInTrainCommand);
            this.CommandManager.RemoveHandler(CounterCommand);
            this.CommandManager.RemoveHandler(SpawnPointCommand);

            this.HuntManager.Dispose();
            this.FlyTextGui.Dispose();
        }

        private void DebugWindowCommand(string command, string args) => this.PluginUi.RandomDebugWindowVisisble = !PluginUi.RandomDebugWindowVisisble;
        private void HuntMapCommand(string command, string args) => PluginUi.MapVisible = !PluginUi.MapVisible;
        private void HuntTrainCommand(string command, string args) => HuntTrainUI.HuntTrainWindowVisible = !HuntTrainUI.HuntTrainWindowVisible;
        //gets next available hunt in the recorded train
        private void GetNextMobCommand(string command, string args) => HuntTrainUI.GetNextMobCommand();
        private void CounterWindowCommand(string command, string args) => CounterUI.WindowVisible = !CounterUI.WindowVisible;
        private void SpawnPointWindowCommand(string command, string args) => SpawnPointFinderUI.WindowVisible = !SpawnPointFinderUI.WindowVisible;

        private void DrawUI()
        {
            this.PluginUi.Draw();
            this.HuntTrainUI.Draw();
            this.CounterUI.Draw();
            this.SpawnPointFinderUI.Draw();
        }

        private void DrawConfigUI()
        {
            this.PluginUi.SettingsVisible = true;
        }
    }
}
