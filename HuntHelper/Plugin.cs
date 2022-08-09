using System;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using System.Reflection;
using Dalamud.Data;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using System.Drawing;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.FlyText;
using HuntHelper.MapInfoManager;
using ImGuiNET;
using HuntHelper.Managers.Hunts;

namespace HuntHelper
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Hunt Helper";

        private const string MapWindowCommand = "/hh";
        private const string HuntTrainWindowCommand = "/hht";
        private const string NextHuntInTrainCommand = "/hhn";
        private const string CommandName = "/hh1";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        private Configuration Configuration { get; init; }
        private PluginUI PluginUi { get; init; }
        private HuntTrainUI HuntTrainUI { get; init; }
        private ClientState ClientState { get; init; }
        private ObjectTable ObjectTable { get; init; }
        private DataManager DataManager { get; init; }
        private ChatGui ChatGui { get; init; }
        private HuntManager HuntManager { get; init; }
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

            this.HuntManager = new HuntManager(PluginInterface, chatGui, flyTextGui);
            this.MapDataManager = new MapDataManager(PluginInterface);
            
            this.PluginUi = new PluginUI(this.Configuration, pluginInterface, clientState, objectTable, dataManager, HuntManager, MapDataManager);
            this.HuntTrainUI = new HuntTrainUI(HuntManager, Configuration);

            this.CommandManager.AddHandler(CommandName, new CommandInfo(DebugWindowCommand)
            {
                HelpMessage = "random data, debug info"
            });
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
                HelpMessage = "Gets a flag for the next mob in the recorded hunt train"
            });

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        }

        public void Dispose()
        {
            //save hunttrainui config first
            this.HuntTrainUI.Dispose();
            this.PluginUi.Dispose();
            this.CommandManager.RemoveHandler(CommandName);
            this.CommandManager.RemoveHandler(MapWindowCommand);
            this.CommandManager.RemoveHandler(HuntTrainWindowCommand);
            this.CommandManager.RemoveHandler(NextHuntInTrainCommand);
            this.HuntManager.Dispose();
            this.FlyTextGui.Dispose();
        }

        private void DebugWindowCommand(string command, string args) => this.PluginUi.RandomDebugWindowVisisble = !PluginUi.RandomDebugWindowVisisble;
        private void HuntMapCommand(string command, string args) => PluginUi.MapVisible = !PluginUi.MapVisible;
        private void HuntTrainCommand(string command, string args) => HuntTrainUI.HuntTrainWindowVisible = !HuntTrainUI.HuntTrainWindowVisible;
        //gets next available hunt in the recorded train
        private void GetNextMobCommand(string command, string args) => HuntTrainUI.GetNextMobCommand();

        private void DrawUI()
        {
            this.PluginUi.Draw();
            this.HuntTrainUI.Draw();
        }

        private void DrawConfigUI()
        {
            this.PluginUi.SettingsVisible = true;
        }
    }
}
