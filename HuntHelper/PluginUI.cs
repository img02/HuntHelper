using ImGuiNET;
using System;
using System.Drawing;
using System.IO;
using System.Net.Http.Headers;
using System.Numerics;
using System.Threading;
using Dalamud.Data;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Logging;
using Dalamud.Plugin;
using HuntHelper.MapInfoManager;
using Lumina.Excel.GeneratedSheets;
using HuntHelper.Managers.Hunts;

namespace HuntHelper
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    class PluginUI : IDisposable
    {
        private Configuration configuration;
        private DalamudPluginInterface pluginInterface;

        private ClientState ClientState;
        private ObjectTable ObjectTable;
        private DataManager DataManager;
        private HuntManager HuntManager;
        private MapDataManager mapDataManager;

        private String TerritoryName;
        private ushort TerritoryID;

        private float MobIconRadius;
        private float PlayerIconRadius;
        private float SpawnPointIconRadius;
        private float bottomPanelHeight = 30;

        private float _priorityMobSpacing = 12.0f;

        private float detectionRadiusModifier = 1.0f;
        private float mouseOverDistanceModifier = 2.5f;

        private float _allRadiusModifier = 1.0f; //modifier for all
        private float _mobIconRadiusModifier = 1.5f;
        private float _spawnPointRadiusModifier = 1.0f;
        private float _playerIconRadiusModifier = 0.20f;
        private float _detectionCircleModifier = 1.0f;

        private float _detectionCircleThickness = 2f;
        private float _directionLineThickness = 3f;

        //initial window position
        private Vector2 mapWindowPos = new Vector2(25, 25);

        private float _mapZoneMaxCoordSize = 41; //default to 41 as thats most common for hunt zones

        public float SingleCoordSize => ImGui.GetWindowSize().X / _mapZoneMaxCoordSize;

        //private uint spawnPointColour = ImGui.ColorConvertFloat4ToU32(new Vector4(0.69f, 0.69f, 0.69f, 1f));
        private uint spawnPointColour = ImGui.ColorConvertFloat4ToU32(new Vector4(0.29f, 0.21f, .2f, 1f)); //brownish?
        private uint mobColour = ImGui.ColorConvertFloat4ToU32(new Vector4(0.4f, 1f, 0.567f, 1f)); //green
        private uint playerColour = ImGui.ColorConvertFloat4ToU32(new Vector4(.5f, 0.567f, 1f, 1f)); //darkish blue
        private uint _playerIconBackgroundColour = ImGui.ColorConvertFloat4ToU32(new Vector4(0.117647f, 0.5647f, 1f, 0.7f)); //blue

        private readonly Vector4 _defaultTextColour = new Vector4(1f, 1f, 1f, 1f); //white
        private readonly Vector4 _priorityMobTextColour = new Vector4(1f, 1f, 1f, 1f); //white


        //window bools
        private bool _mainWindowVisible = false;
        private bool _showDatabaseListWindow = false;
        //checkbox bools
        private bool _showDebug = false;
        private bool _showOptionsWindow = true;
        private bool _showZoneName = true;
        private bool _showWorldName = true;
        private bool _saveSpawnData = true;
        private bool _useMapImages = true;

        public bool MainWindowVisible
        {
            get { return this._mainWindowVisible; }
            set { this._mainWindowVisible = value; }
        }

        private bool testVisible = false;
        public bool TestVisible
        {
            get => testVisible;
            set => testVisible = value;
        }

        private bool settingsVisible = false;

        public bool SettingsVisible
        {
            get { return this.settingsVisible; }
            set { this.settingsVisible = value; }
        }


        public PluginUI(Configuration configuration, DalamudPluginInterface pluginInterface,
            ClientState clientState, ObjectTable objectTable, DataManager dataManager,
            HuntManager huntManager, MapDataManager mapDataManager)
        {
            //add hunt manager to this class

            this.configuration = configuration;
            this.pluginInterface = pluginInterface; //not using atm...
            //this.goatImage = goatImage;

            this.ClientState = clientState;
            this.ObjectTable = objectTable;
            this.DataManager = dataManager;
            this.HuntManager = huntManager;
            this.mapDataManager = mapDataManager;

            TerritoryName = String.Empty;
            ClientState_TerritoryChanged(null, 0);

            ClientState.TerritoryChanged += ClientState_TerritoryChanged;

            LoadMapImages();
        }

        public void Dispose()
        {
            HuntManager.Dispose();
        }

        public void Draw()
        {
            // This is our only draw handler attached to UIBuilder, so it needs to be
            // able to draw any windows we might have open.
            // Each method checks its own visibility/state to ensure it only draws when
            // it actually makes sense.
            // There are other ways to do this, but it is generally best to keep the number of
            // draw delegates as low as possible.

            DrawMainWindow();
            DrawSettingsWindow();


            DrawHuntMapWindow();
        }

        public void DrawMainWindow()
        {
            if (!MainWindowVisible)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(375, 330), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(375, 330), new Vector2(float.MaxValue, float.MaxValue));
            if (ImGui.Begin("My Amazing Window!", ref this._mainWindowVisible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.Text($"The random config bool is {this.configuration.SomePropertyToBeSavedAndWithADefault}");

                if (ImGui.Button("Settings"))
                {
                    SettingsVisible = true;
                }

                ImGui.Spacing();

                ImGui.Indent(55);
                //ImGui.Image(this.goatImage.ImGuiHandle, new Vector2(this.goatImage.Width, this.goatImage.Height));
                ImGui.Unindent(55);

                ImGui.Text($"Territory: {TerritoryName}");
                ImGui.Text($"Territory ID: {ClientState.TerritoryType}");

                //PLAYER POS
                var v3 = ClientState.LocalPlayer?.Position ?? new Vector3(0, 0, 0);
                ImGui.Text($"v3: -----\n" +
                           $"X: {ConvertPosToCoordinate(v3.X)} |" +
                           $"Y: {ConvertPosToCoordinate(v3.Z)} |\n" +
                           $"v3: -----\n" +
                           $"X: {Math.Round(v3.X, 2)} |" +
                           $"Y: {Math.Round(v3.Z, 2)} |");
                //END

                ImGui.Indent(55);
                ImGui.Text("");

                var hunt = "";
                foreach (var obj in this.ObjectTable)
                {
                    if (obj is not BattleNpc bobj) continue;
                    if (bobj.MaxHp < 10000) continue; //not really needed if subkind is enemy, once matching to id / name
                    if (bobj.BattleNpcKind != BattleNpcSubKind.Enemy) continue; //not really needed if matching to 'nameID'


                    hunt += $"{obj.Name} \n" +
                            $"KIND: {bobj.BattleNpcKind}\n" +
                            $"NAMEID: {bobj.NameId}\n" +
                            $"|HP: {bobj.CurrentHp}\n" +
                            $"|HP%%: {(bobj.CurrentHp * 1.0 / bobj.MaxHp) * 100}%%\n" +
                            //$"|object ID: {obj.ObjectId}\n| Data ID: {obj.DataId} \n| OwnerID: {obj.OwnerId}\n" +
                            $"X: {ConvertPosToCoordinate(obj.Position.X)};\n" +
                            $"Y: {ConvertPosToCoordinate(obj.Position.Y)}\n" +
                            $"  --------------  \n";
                }
                ImGui.Text($"Found: {hunt}");
            }
            ImGui.End();
        }

        public void DrawHuntMapWindow()
        {
            if (!TestVisible)
            {
                return;
            }
            ImGui.SetNextWindowSize(new Vector2(512, 512), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(512, -1), new Vector2(float.MaxValue, -1)); //disable manual resize vertical
            ImGui.SetNextWindowPos(mapWindowPos, ImGuiCond.FirstUseEver);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
            if (ImGui.Begin("Test Window!", ref this.testVisible,
                    ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoTitleBar))
            {
                _mapZoneMaxCoordSize = HuntManager.GetMapZoneCoordSize(TerritoryID);
                //if custom size not used, use these default sizes - resize with window size
                //radius for mob / spawn point circles - equal to half a map coord size
                SpawnPointIconRadius = _allRadiusModifier * _spawnPointRadiusModifier * (0.25f * SingleCoordSize);
                MobIconRadius = _allRadiusModifier * _mobIconRadiusModifier * (0.25f * SingleCoordSize);
                PlayerIconRadius = _allRadiusModifier * _playerIconRadiusModifier * (0.125f * SingleCoordSize); //default half of mob icon size

                //change height as width changes, to maintain 1:1 ratio. 
                var width = ImGui.GetWindowSize().X;
                ImGui.SetWindowSize(new Vector2(width));

                var bottomDockingPos = Vector2.Add(ImGui.GetWindowPos(), new Vector2(0, ImGui.GetWindowSize().Y));

                //=========================================
                //if using images
                //draw images first so they are at the bottom.
                //=========================================
                if (_useMapImages)
                {
                    var mapImg = HuntManager.GetMapImage(TerritoryName);
                    if (mapImg != null)
                    {
                        ImGui.Image(mapImg.ImGuiHandle, ImGui.GetWindowSize());
                        ImGui.SetCursorPos(Vector2.Zero);
                    }
                }

                //show map coordinates when mouse is over gui
                ShowCoordOnMouseOver();

                //draw player icon and info
                UpdatePlayerInfo();

                //draw spawn points for the current map, if applicable.
                DrawSpawnPoints(TerritoryID);

                UpdateMobInfo();

                //bottom docking window with buttons and options and stuff
                if (_showOptionsWindow) DrawOptionsWindow();

                //putting this here instead because I want to draw it on this window, not a new one.
                if (_showDebug) ShowDebugInfo();

                //button to toggle bottom panel thing
                var cursorPos = new Vector2(8, ImGui.GetWindowSize().Y - 30);
                ImGui.SetCursorPos(cursorPos);
                //get a cogwheel img or something idk
                //ImGui.ColorButton("idk", new Vector4(0f, 0f, 0f, 1f));
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(.4f, .4f, .4f, 1f));
                if (ImGui.Button(" ö ")) _showOptionsWindow = !_showOptionsWindow;
                ImGui.PopStyleColor();
                if (HuntManager.ErrorPopUpVisible || mapDataManager.ErrorPopUpVisible)
                {
                    ImGui.Begin("Error loading data");
                    ImGui.Text(HuntManager.ErrorMessage);
                    ImGui.Text(mapDataManager.ErrorMessage);
                    ImGui.End();
                }

            }
            ImGui.End();
        }

        private void DrawOptionsWindow()
        {
            #region Bottom docking info window
            var bottomDockingPos = Vector2.Add(ImGui.GetWindowPos(), new Vector2(0, ImGui.GetWindowSize().Y));
            
            //ImGui.BeginChild(1, new Vector2(ImGui.GetWindowSize().X, 25));
            ImGui.SetNextWindowSize(new Vector2(ImGui.GetWindowSize().X, bottomPanelHeight), ImGuiCond.Always);
            ImGui.SetNextWindowSizeConstraints(new Vector2(-1, 95), new Vector2(-1, 300));

            //hide grip color
            ImGui.PushStyleColor(ImGuiCol.ResizeGrip, 0);

            ImGui.Begin("Options Window", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove);
            ImGui.SetWindowPos(bottomDockingPos);
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Columns(2);
            /*ImGui.SetColumnWidth(0, ImGui.GetWindowSize().X / 5.5f);
            ImGui.SetColumnWidth(1, 5.5f * ImGui.GetWindowSize().X / 5.5f);*/

            ImGui.Checkbox("Map Image", ref _useMapImages);
            ImGui.SameLine(); ImGui_HelpMarker("Use a map image instead of blank background (ugly tho)");

            ImGui.Checkbox("Show Debug", ref _showDebug);
            ImGui.SameLine(); ImGui_HelpMarker("idk shows random debug info");

            //ImGui.Dummy(new Vector2(0, 4f));

            if (ImGui.Button("Loaded Hunt Data")) _showDatabaseListWindow = !_showDatabaseListWindow;
            ImGui.SameLine(); ImGui_HelpMarker("Show the loaded hunt and spawn point data");
            DrawDataBaseWindow();

            ImGui.NextColumn();
            if (ImGui.BeginTabBar("Options", ImGuiTabBarFlags.None))
            {
                if (ImGui.BeginTabItem("General"))
                {
                    bottomPanelHeight = 95f;
                    ImGui.SameLine(); ImGui.Text("   \t\t\t  ");
                    ImGui.SameLine(); ImGui.TextDisabled($"{ImGui.GetWindowSize()}");
                    ImGui.SameLine(); ImGui.TextDisabled($"{bottomPanelHeight}");

                    ImGui.Dummy(new Vector2(0, 8f));
                    if (ImGui.BeginTable("General Options table", 4)) {
                        ImGui.TableNextColumn();
                        ImGui.Checkbox("Zone Name", ref _showZoneName);
                        ImGui.SameLine(); ImGui_HelpMarker("Shows Zone Name (ToDo)");

                        ImGui.TableNextColumn();
                        ImGui.Checkbox("World Name", ref _showWorldName);
                        ImGui.SameLine(); ImGui_HelpMarker("Shows World Name (ToDo)");

                        ImGui.TableNextColumn();
                        ImGui.Checkbox("Save Spawn Data", ref _saveSpawnData);
                        ImGui.SameLine(); ImGui_HelpMarker("Saves S Rank Information to desktop txt (ToDo)");
                        ImGui.EndTable();
                    }
                    ImGui.EndTabItem();
                }
                
                if (ImGui.BeginTabItem("Visuals"))
                {
                    ImGui.SameLine(); ImGui.TextDisabled($"{ImGui.GetWindowSize()}");
                    ImGui.SameLine(); ImGui.TextDisabled($"{bottomPanelHeight}");
                    bottomPanelHeight = 180f;

                    if (ImGui.BeginTabBar("Visuals sub-bar")){
                        if (ImGui.BeginTabItem("Sizing"))
                        {
                            ImGui.Dummy(new Vector2(0, 2f));
                            if (ImGui.BeginTable("Sizing Options Table", 3))
                            {
                                var widgetWidth = 40f;

                                ImGui.TableNextColumn();
                                ImGui.PushItemWidth(widgetWidth);
                                ImGui.InputFloat("Player Modifier", ref _playerIconRadiusModifier);
                                ImGui.SameLine(); ImGui_HelpMarker("Player Icon Radius Modifier: Default 0.2");

                                ImGui.TableNextColumn();
                                ImGui.PushItemWidth(widgetWidth);
                                ImGui.InputFloat("Mob Modifier", ref _mobIconRadiusModifier);
                                ImGui.SameLine(); ImGui_HelpMarker("Mob Icon Radius Modifier, default: 1.5");

                                ImGui.TableNextColumn();
                                ImGui.PushItemWidth(widgetWidth);
                                ImGui.InputFloat("Spawn Point Modifier", ref _spawnPointRadiusModifier);
                                ImGui.SameLine(); ImGui_HelpMarker("Spawn Point Radius Modifier, default: 1.0");

                                ImGui.TableNextColumn();
                                ImGui.InputFloat("All Modifier", ref _allRadiusModifier);
                                ImGui.SameLine(); ImGui_HelpMarker("Increase all icons proportionally, default: 1\nBy Default, A Mob icon is 1.5x a Spawn Point, a Player icon is 0.2x a Spawn Point");

                                ImGui.TableNextColumn();
                                ImGui.InputFloat("Detection Circle Modifier", ref _detectionCircleModifier);
                                ImGui.SameLine(); ImGui_HelpMarker("Default represents 2 in-game coordinates. Modify if you feel this is inaccurate, default: 1.0");

                                ImGui.TableNextColumn();
                                ImGui.Dummy(new Vector2(0, 26f));

                                ImGui.TableNextColumn();
                                ImGui.Separator();
                                ImGui.Dummy(new Vector2(0, 1f));
                                ImGui.InputFloat("Detection Circle Thickness", ref _detectionCircleThickness);
                                ImGui.SameLine(); ImGui_HelpMarker("default: 2.0");

                                ImGui.TableNextColumn();
                                ImGui.Separator();
                                ImGui.Dummy(new Vector2(0, 1f));
                                ImGui.InputFloat("Direction Line Thickness", ref _directionLineThickness);
                                ImGui.SameLine(); ImGui_HelpMarker("default: 3.0");

                                ImGui.TableNextColumn();
                                ImGui.Separator();
                                ImGui.Dummy(new Vector2(0, 1f));
                                if (ImGui.Button("Reset"))
                                {
                                    //reset values
                                }
                                ImGui.SameLine(); ImGui_HelpMarker("Reset all sizes to default.");

                                ImGui.PopItemWidth();

                                ImGui.EndTable();
                            }
                            ImGui.EndTabItem();
                        }

                        if (ImGui.BeginTabItem("Colours"))
                        {
                            ImGui.Dummy(new Vector2(0, 2f));
                            bottomPanelHeight = 180f;

                            if (ImGui.BeginTable("Colour Options Table", 2))
                            {
                                ImGui.Dummy(new Vector2(0, 2f));

                                ImGui.TableNextColumn();
                                var color = new Vector4(1f, 1f, 1f, 1f);
                                ImGui.ColorEdit4("mob icon", ref color);
                                ImGui.SameLine(); ImGui_HelpMarker("white colour picker");

                                ImGui.TableNextColumn();
                                var color1 = new Vector4(1f, 0f, 0f, 1f);
                                ImGui.ColorEdit4("spawn point", ref color1);
                                ImGui.SameLine(); ImGui_HelpMarker("red colour picker");

                                ImGui.Dummy(new Vector2(0, 4f));

                                ImGui.TableNextColumn();
                                var color2 = new Vector4(0f,1f, 0f, 1f);
                                ImGui.ColorEdit4("player background", ref color2);
                                ImGui.SameLine(); ImGui_HelpMarker("green colour picker");

                                ImGui.TableNextColumn();
                                var color3 = new Vector4(0f,0f, 1f, 1f);
                                ImGui.ColorEdit4("player", ref color3);
                                ImGui.SameLine(); ImGui_HelpMarker("blue colour picker");

                                ImGui.Dummy(new Vector2(0, 4f));

                                ImGui.TableNextColumn();
                                var color4 = new Vector4(0f,0f, 1f, 1f);
                                ImGui.ColorEdit4("direction line", ref color4);
                                ImGui.SameLine(); ImGui_HelpMarker("blue colour picker");
                                
                                ImGui.TableNextColumn();
                                var color5 = new Vector4(0f,0f, 1f, 1f);
                                ImGui.ColorEdit4("detection circle", ref color5);
                                ImGui.SameLine(); ImGui_HelpMarker("blue colour picker");


                                ImGui.EndTable();
                            }

                            ImGui.EndTabItem();
                        }

                        ImGui.EndTabBar();
                    }
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            //bottomPanelHeight = ImGui.GetWindowSize().Y;
            ImGui.End();
            //ImGui.EndChildFrame();
            #endregion
        }
        public void DrawSettingsWindow()
        {
            if (!SettingsVisible)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(232, 75), ImGuiCond.Always);
            if (ImGui.Begin("A Wonderful Configuration Window", ref this.settingsVisible,
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                // can't ref a property, so use a local copy
                var configValue = this.configuration.SomePropertyToBeSavedAndWithADefault;
                if (ImGui.Checkbox("Random Config Bool", ref configValue))
                {
                    this.configuration.SomePropertyToBeSavedAndWithADefault = configValue;
                    // can save immediately on change, if you don't want to provide a "Save and Close" button
                    this.configuration.Save();
                }

                if (ImGui.Button("Show Loaded Hunt Data"))
                {
                    _showDatabaseListWindow = !_showDatabaseListWindow;
                }

                DrawDataBaseWindow();
            }
            ImGui.End();
        }


        private void ClientState_TerritoryChanged(object? sender, ushort e)
        {
            TerritoryName = Utilities.MapHelpers.GetMapName(DataManager, this.ClientState.TerritoryType);
            TerritoryID = ClientState.TerritoryType;
        }

        #region Draw Sub Windows

        private void DrawDataBaseWindow()
        {
            if (!_showDatabaseListWindow) return;

            ImGui.SetNextWindowSize(new Vector2(450, 800));
            ImGui.Begin("Loaded Hunt Data", ref _showDatabaseListWindow);
            if (ImGui.BeginTabBar("info"))
            {
                if (ImGui.BeginTabItem("database"))
                {
                    ImGui.PushFont(UiBuilder.MonoFont);
                    ImGui_CentreText(HuntManager.GetDatabaseAsString(), _defaultTextColour);
                    ImGui.PopFont();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("spawn points"))
                {
                    ImGui.TextUnformatted(mapDataManager.ToString());
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }

            ImGui.End();
        }

        #endregion

        #region DrawList Stuff - Mob Icon, Player Icon
        public void DrawSpawnPoints(ushort mapID)
        {
            var spawnPoints = mapDataManager.GetSpawnPoints(mapID);
            if (spawnPoints.Count == 0) return;
            var drawList = ImGui.GetWindowDrawList();

            foreach (var sp in spawnPoints)
            {
                var drawPos = CoordinateToPositionInWindow(sp);
                drawList.AddCircleFilled(drawPos, SpawnPointIconRadius, spawnPointColour);
            }
        }

        #region Mob


        private void UpdateMobInfo()
        {
            var drawlist = ImGui.GetWindowDrawList();
            HuntManager.ClearMobs();
            foreach (var obj in this.ObjectTable)
            {
                if (obj is not BattleNpc mob) continue;
                if (!HuntManager.IsHunt(mob.NameId)) continue;
                HuntManager.AddMob(mob);
                DrawMobIcon(mob);
            }

            DrawPriorityMobInfo();
        }

        private void DrawMobIcon(BattleNpc mob)
        {
            var mobPos = CoordinateToPositionInWindow(new Vector2(ConvertPosToCoordinate(mob.Position.X),
                ConvertPosToCoordinate(mob.Position.Z)));
            var drawlist = ImGui.GetWindowDrawList();
            drawlist.AddCircleFilled(mobPos, MobIconRadius, mobColour);

            //draw mob icon tooltip
            if (Vector2.Distance(ImGui.GetMousePos(), mobPos) < MobIconRadius * mouseOverDistanceModifier)
            {
                DrawMobIconToolTip(mob);
            }
        }

        private void DrawMobIconToolTip(BattleNpc mob)
        {
            var text = new string[]
            {
                $"{mob.Name}",
                $"({ConvertPosToCoordinate(mob.Position.X)}, {ConvertPosToCoordinate(mob.Position.Z)})",
                $"{Math.Round((mob.CurrentHp * 1.0) / mob.MaxHp * 100, 2)}%"
            };
            ImGui_ToolTip(text);
        }

        private void DrawPriorityMobInfo()
        {
            var (rank, mob) = HuntManager.GetPriorityMob();
            if (mob == null) return;
            ImGui.PushFont(UiBuilder.MonoFont);
            ImGui.Dummy(new Vector2(0f, _priorityMobSpacing));
            ImGui_CentreText($"   {rank}     |  {mob.Name}  |  {Math.Round(((1.0 * mob.CurrentHp) / mob.MaxHp) * 100):0.00}%", _priorityMobTextColour);
            ImGui_CentreText($"({ConvertPosToCoordinate(mob.Position.X):0.00}, {ConvertPosToCoordinate(mob.Position.Z):0.00})", _priorityMobTextColour);
            ImGui.PopFont();
        }
        #endregion

        #region Player
        private void UpdatePlayerInfo()
        {
            //if this stuff ain't null, draw player position
            if (ClientState?.LocalPlayer?.Position != null)
            {
                var playerPos = CoordinateToPositionInWindow(
                    new Vector2(ConvertPosToCoordinate(ClientState.LocalPlayer.Position.X),
                        ConvertPosToCoordinate(ClientState.LocalPlayer.Position.Z)));

                var detectionRadius = 2 * SingleCoordSize * detectionRadiusModifier;
                var rotation = Math.Abs(ClientState.LocalPlayer.Rotation - Math.PI);
                var lineEnding =
                    new Vector2(
                        playerPos.X + (float)(detectionRadius * Math.Sin(rotation)),
                        playerPos.Y - (float)(detectionRadius * Math.Cos(rotation)));

                #region Player Icon Drawing - order: direction, detection, player

                var drawlist = ImGui.GetWindowDrawList();
                DrawPlayerIcon(drawlist, playerPos, detectionRadius, lineEnding);

                #endregion
            }

        }

        private void DrawPlayerIcon(ImDrawListPtr drawlist, Vector2 playerPos, float detectionRadius, Vector2 lineEnding)
        {
            //player icon background circle
            drawlist.AddCircleFilled(playerPos, detectionRadius, _playerIconBackgroundColour);
            //direction line
            drawlist.AddLine(playerPos, lineEnding, ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.3f, 0.3f, 1f)), _directionLineThickness);
            //player filled circle
            drawlist.AddCircleFilled(playerPos, PlayerIconRadius,
                playerColour);
            //detection circle
            drawlist.AddCircle(playerPos, detectionRadius,
                ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 0f, 1f)), 0, _detectionCircleThickness);
        }
        #endregion

        #endregion

        private void ShowCoordOnMouseOver()
        {
            var winPos = ImGui.GetWindowPos();
            var winSize = ImGui.GetWindowSize();
            var mousePos = ImGui.GetMousePos();

            //if mouse pos is between top left (window pos) and bottom right of window (window pos + window size)
            if (Vector2.Subtract(mousePos, winPos).X > 0 && Vector2.Subtract(mousePos, winPos).Y > 0 &&
                Vector2.Subtract(mousePos, Vector2.Add(winPos, winSize)).X < 0 && Vector2.Subtract(mousePos, Vector2.Add(winPos, winSize)).Y < 0)
            {
                var coord = MouseOverPositionToGameCoordinate();
                var text = new string[] { $"({Math.Round(coord.X, 2):0.0}, {Math.Round(coord.Y, 2):0.0})" };
                ImGui_ToolTip(text);
            }
        }

        private void ShowDebugInfo()
        {
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui_TextColoured("Random Debug Info?!:");
            ImGui.BeginChild("debug");
            ImGui.Columns(2);
            ImGui_TextColoured($"Content1 Region: {ImGui.GetContentRegionAvail()}");
            ImGui_TextColoured($"Window Size: {ImGui.GetWindowSize()}");
            ImGui_TextColoured($"Window  Pos: {ImGui.GetWindowPos()}");

            if (ClientState?.LocalPlayer?.Position != null)
            {
                ImGui_TextColoured($"rotation: {ClientState.LocalPlayer.Rotation}");
                var rotation = Math.Abs(ClientState.LocalPlayer.Rotation - Math.PI);
                ImGui_TextColoured($"rotation rad.: {Math.Round(rotation, 2):0.00}");
                ImGui_TextColoured($"rotation sin.: {Math.Round(Math.Sin(rotation), 2):0.00}");
                ImGui_TextColoured($"rotation cos.: {Math.Round(Math.Cos(rotation), 2):0.00}");
                ImGui_TextColoured($"Player Pos: (" +
                                   $"{Utilities.MapHelpers.ConvertToMapCoordinate(ClientState.LocalPlayer.Position.X, _mapZoneMaxCoordSize):0.00}" +
                                   $"," +
                                   $" {Utilities.MapHelpers.ConvertToMapCoordinate(ClientState.LocalPlayer.Position.Z, _mapZoneMaxCoordSize):0.00}" +
                                   $")");
                var playerPos = CoordinateToPositionInWindow(
                    new Vector2(ConvertPosToCoordinate(ClientState.LocalPlayer.Position.X),
                        ConvertPosToCoordinate(ClientState.LocalPlayer.Position.Z)));
                ImGui_TextColoured($"Player Pos: {playerPos}");
                ImGui.NextColumn();
                ImGui_RightAlignText($"Map: {TerritoryName} - {TerritoryID}");
                ImGui_RightAlignText($"Coord size: {_mapZoneMaxCoordSize} ");

                //priority mob stuff
                ImGui_RightAlignText("Priority Mob:");
                var priorityMob = HuntManager.GetPriorityMob().Item2;
                if (priorityMob != null)
                {
                    ImGui_RightAlignText($"Rank {HuntManager.GetPriorityMob().Item1} " +
                                         $"{priorityMob.Name} " +
                                         $"{Math.Round(1.0 * priorityMob.CurrentHp / priorityMob.MaxHp * 100, 2):0.00}% | " +
                                         $"({ConvertPosToCoordinate(priorityMob.Position.X):0.00}, " +
                                         $"{ConvertPosToCoordinate(priorityMob.Position.Z):0.00}) ");
                }

                var currentMobs = HuntManager.GetAllCurrentMobs();
                ImGui_RightAlignText("--");
                ImGui_RightAlignText("Other nearby mobs:");
                foreach (var (rank, mob) in currentMobs)
                {
                    ImGui_RightAlignText("--");
                    ImGui_RightAlignText($"Rank: {rank} | " +
                                         $"{mob.Name} {Math.Round(1.0 * mob.CurrentHp / mob.MaxHp * 100, 2):0.00}% | " +
                                         $"({ConvertPosToCoordinate(mob.Position.X):0.00}, " +
                                         $"{ConvertPosToCoordinate(mob.Position.Z):0.00})");
                }
                ImGui.EndChild();
            }
            else
            {
                ImGui.Text("Can't find local player");
            }
        }

        private void ImGui_CentreText(string text, Vector4 colour)
        {
            var windowWidth = ImGui.GetWindowSize().X;
            var textWidth = ImGui.CalcTextSize(text).X;
            ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
            ImGui.TextColored(colour, text);
            //ImGui.TextUnformatted(text);
        }

        private void ImGui_RightAlignText(string text)
        {
            var windowWidth = ImGui.GetWindowSize().X;
            var textWidth = ImGui.CalcTextSize(text).X;
            ImGui.SetCursorPosX((windowWidth - textWidth) * .95f);
            ImGui_TextColoured(text);
            //ImGui.TextUnformatted(text);
        }

        private void ImGui_TextColoured(string text)
        {
            var colour = new Vector4(1f, 1f, 1f, 1f);
            if (_useMapImages) colour = new Vector4(0f, 0f, 0f, 1f);
            ImGui.TextColored(colour, text);
        }

        //use list instead? fixed input, so slightly more 'optimized' w/ array i guess...
        private void ImGui_ToolTip(string[] text)
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 50.0f);
            ImGui.Separator();

            foreach (var s in text)
            {
                ImGui_CentreText(s, _defaultTextColour);
            }
            ImGui.Separator();
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().IndentSpacing * 0.5f);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }

        private void ImGui_HelpMarker(string text)
        {
            ImGui.TextDisabled("(?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
                ImGui.TextUnformatted(text);
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }
        }

        private void LoadMapImages()
        {
            if (!_useMapImages) return;
            HuntManager.LoadMapImages();
        }

        //=================================================================

        private Vector2 MouseOverPositionToGameCoordinate()
        {
            var mousePos = ImGui.GetMousePos() - ImGui.GetWindowPos();
            var coord = mousePos / SingleCoordSize + Vector2.One;
            return coord;
        }

        private float ConvertPosToCoordinate(float pos)
        {
            return Utilities.MapHelpers.ConvertToMapCoordinate(pos, _mapZoneMaxCoordSize);
        }

        private Vector2 CoordinateToPositionInWindow(Vector2 pos)
        {
            var x = (pos.X - 1) * SingleCoordSize + ImGui.GetWindowPos().X;
            var y = (pos.Y - 1) * SingleCoordSize + ImGui.GetWindowPos().Y;
            return new Vector2(x, y);
        }

        //z pos offset seems diff for every map, idk idc...
        private double ConvertARR_Z_ToCoordinate(float pos)
        {
            // arr z index seems to be 0 == 12. since: 1.0 == 112 and 0.5 == 62. 
            // not exactly super thorough testing tbh

            //return Math.Floor(((pos - 12) / 100.0) * 100) / 100; //western than
            return Math.Floor(((pos + 1) / 100.0) * 100) / 100; //middle la noscea 
        }
    }
}
