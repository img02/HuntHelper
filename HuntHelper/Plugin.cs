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
using HuntHelper.MapInfoManager;
using ImGuiNET;

namespace HuntHelper
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Hunt Helper";

        private const string commandName = "/pmycommand";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        private Configuration Configuration { get; init; }
        private PluginUI PluginUi { get; init; }

        private ClientState ClientState { get; init; }
        private ObjectTable ObjectTable { get; init; }
        private DataManager DataManager { get; init; }

        private HuntManager huntManager { get; init; }
        private MapDataManager mapDataManager { get; init; }

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager,
            ClientState clientState,
            [RequiredVersion("1.0")] ObjectTable objectTable,
            DataManager dataManager)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;

            this.ClientState = clientState;
            this.ObjectTable = objectTable;
            this.DataManager = dataManager;
            
            this.huntManager = new HuntManager(PluginInterface);
            this.mapDataManager = new MapDataManager(PluginInterface);

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            // you might normally want to embed resources and load them from the manifest stream
            var imagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");

            var goatImage = this.PluginInterface.UiBuilder.LoadImage(imagePath);

            #region idk
            #endregion

            this.PluginUi = new PluginUI(this.Configuration, pluginInterface, goatImage, clientState, objectTable, dataManager, huntManager, mapDataManager);

            this.CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "A useful message to display in /xlhelp"
            });

            this.CommandManager.AddHandler("/hh", new CommandInfo(TestCommand)
            {
                HelpMessage = "test"
            });

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;



        }

        public void Dispose()
        {
            this.PluginUi.Dispose();
            this.CommandManager.RemoveHandler(commandName);
            this.CommandManager.RemoveHandler("/hh");
        }

        private void OnCommand(string command, string args)
        {
            // in response to the slash command, just display our main ui
            this.PluginUi.Visible = true;
        }
        private void TestCommand(string command, string args)
        {
            //this.PluginUi.TestVisible = true;

            PluginUi.TestVisible = !PluginUi.TestVisible;
        }

        private void DrawUI()
        {
            this.PluginUi.Draw();
        }

        private void DrawConfigUI()
        {
            this.PluginUi.SettingsVisible = true;
        }


        private void LoadHuntData()
        {
            //load hunt data from jsons / factories into dictionaries

            //reuse logic from ufht
            /*
             * load data into dictionaries
             *
             * also later, move logic into own class... HuntStatus -contains current status, MobA1,MobB2,MobS,MobSS, MobA2,Mobb2 - maybe player,playerpos,map.etc.
             *
             * scan against dictionary based on map location
             *
             */



            //ToDo
        }
    }
}
