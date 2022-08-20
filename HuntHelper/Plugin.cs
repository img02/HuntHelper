using Dalamud.Data;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.FlyText;
using Dalamud.Plugin;
using HuntHelper.Gui;
using HuntHelper.Managers.Hunts;
using HuntHelper.Managers.MapData;
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
        private const string CounterCommand = "/hhc";
        private const string SpawnPointCommand = "/hhr";
        private const string DebugCommand = "/hhdebug";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        private Configuration Configuration { get; init; }
        private PluginUI PluginUi { get; init; }
        private HuntTrainUI HuntTrainUI { get; init; }
        private CounterUI CounterUI { get; init; }
        private PointerUI PointerUI{ get; init; }
        private SpawnPointFinderUI SpawnPointFinderUI { get; init; }
        private ClientState ClientState { get; init; }
        private ObjectTable ObjectTable { get; init; }
        private DataManager DataManager { get; init; }
        private ChatGui ChatGui { get; init; }
        private HuntManager HuntManager { get; init; }
        private TrainManager TrainManager { get; init; }
        private MapDataManager MapDataManager { get; init; }
        private FlyTextGui FlyTextGui { get; init; }
        private GameGui GameGui { get; init; }

        public Plugin(
            DalamudPluginInterface pluginInterface,
            CommandManager commandManager,
            ClientState clientState,
            ObjectTable objectTable,
            DataManager dataManager,
            ChatGui chatGui,
            FlyTextGui flyTextGui,
            GameGui gameGui)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;

            this.ClientState = clientState;
            this.ObjectTable = objectTable;
            this.DataManager = dataManager;
            this.ChatGui = chatGui;
            this.FlyTextGui = flyTextGui;
            this.GameGui = gameGui;

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            this.TrainManager = new TrainManager(ChatGui, GameGui, Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, @"Data\HuntTrain.json"));
            this.HuntManager = new HuntManager(PluginInterface, TrainManager, chatGui, flyTextGui);
            this.MapDataManager = new MapDataManager(Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, @"Data\SpawnPointData.json"));

            this.PluginUi = new PluginUI(this.Configuration, pluginInterface, clientState, objectTable, dataManager, HuntManager, MapDataManager, GameGui);
            this.HuntTrainUI = new HuntTrainUI(TrainManager, Configuration);
            this.CounterUI = new CounterUI(ClientState, ChatGui, Configuration);
            this.SpawnPointFinderUI = new SpawnPointFinderUI(MapDataManager, Configuration);
            this.PointerUI = new PointerUI(HuntManager, Configuration, GameGui);

            this.CommandManager.AddHandler(MapWindowCommand, new CommandInfo(HuntMapCommand)
            {
                HelpMessage = "Opens the Hunt Map"
            });
            this.CommandManager.AddHandler(MapWindowPresetOne, new CommandInfo(ApplyPresetOneCommand)
            {
                HelpMessage = "Applies Preset One"
            });
            this.CommandManager.AddHandler(MapWindowPresetTwo, new CommandInfo(ApplyPresetTwoCommand)
            {
                HelpMessage = "Applies Preset Two"
            });
            this.CommandManager.AddHandler(MapWindowPresetOneSave, new CommandInfo(SavePresetOneCommand)
            {
                HelpMessage = "Save Preset One"
            });
            this.CommandManager.AddHandler(MapWindowPresetTwoSave, new CommandInfo(SavePresetTwoCommand)
            {
                HelpMessage = "Save Preset Two"
            });
            this.CommandManager.AddHandler(HuntTrainWindowCommand, new CommandInfo(HuntTrainCommand)
            {
                HelpMessage = $"Opens the Hunt Train Window."
            });
            this.CommandManager.AddHandler(NextHuntInTrainCommand, new CommandInfo(GetNextMobCommand)
            {
                HelpMessage = "Sends the flag for the next train mob into chat"
            });
#if DEBUG
            this.CommandManager.AddHandler(DebugCommand, new CommandInfo(DebugWindowCommand)
            {
                HelpMessage = "random data, debug info"
            });
#endif
            this.CommandManager.AddHandler(CounterCommand, new CommandInfo(CounterWindowCommand)
            {
                HelpMessage = "Counter window -- for counter-based S ranks"
            });
            this.CommandManager.AddHandler(SpawnPointCommand, new CommandInfo(SpawnPointWindowCommand)
            {
                HelpMessage = "spawn point tracking window"
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
            this.FlyTextGui.Dispose();
        }

        private void DebugWindowCommand(string command, string args) => this.PluginUi.RandomDebugWindowVisisble = !PluginUi.RandomDebugWindowVisisble;
        private void HuntMapCommand(string command, string args)
        {
            PluginUi.MapVisible = !PluginUi.MapVisible;
            Configuration.MapWindowVisible = PluginUi.MapVisible;
        }

        private void ApplyPresetOneCommand(string command, string args) => PluginUi.ApplyPreset(1);
        private void ApplyPresetTwoCommand(string command, string args) => PluginUi.ApplyPreset(2);
        private void SavePresetOneCommand(string command, string args) => PluginUi.SavePreset(1);
        private void SavePresetTwoCommand(string command, string args) => PluginUi.SavePreset(2);
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
            this.PointerUI.Draw();
        }

        private void DrawConfigUI()
        {
            this.PluginUi.SettingsVisible = true;
        }
    }
}
