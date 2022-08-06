using ImGuiNET;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
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
using Microsoft.VisualBasic;
using Action = System.Action;

namespace HuntHelper
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    class PluginUI : IDisposable
    {
        private readonly Configuration _configuration;
        private readonly DalamudPluginInterface _pluginInterface;

        private readonly ClientState _clientState;
        private readonly ObjectTable _objectTable;
        private readonly DataManager _dataManager;
        private readonly HuntManager _huntManager;
        private readonly MapDataManager _mapDataManager;

        private String _territoryName;
        private String _worldName => _clientState.LocalPlayer?.CurrentWorld?.GameData?.Name.ToString() ?? "Not Found";
        private ushort _territoryId;

        private float _bottomPanelHeight = 30;

        //base icon sizes - based on coord size? - not customizable
        private float _mobIconRadius;
        private float _playerIconRadius;
        private float _spawnPointIconRadius;

        #region  User Customizable stuff - load and save to config

        //map opacity - should be between 0-1f;
        private float _mapImageOpacityAsPercentage = 100f;

        // icon colours
        private Vector4 _spawnPointColour = new Vector4(0.24706f, 0.309804f, 0.741176f, 1); //purty blue
        private Vector4 _mobColour = new Vector4(0.4f, 1f, 0.567f, 1f); //green
        private Vector4 _playerIconColour = new Vector4(0f, 0f, 0f, 1f); //black
        private Vector4 _playerIconBackgroundColour = new Vector4(0.117647f, 0.5647f, 1f, 0.7f); //blue
        private Vector4 _directionLineColour = new Vector4(1f, 0.3f, 0.3f, 1f); //redish
        private Vector4 _detectionCircleColour = new Vector4(1f, 1f, 0f, 1f); //goldish
        // icon radius sizes
        private float _allRadiusModifier = 1.0f;
        private float _mobIconRadiusModifier = 2f;
        private float _spawnPointRadiusModifier = 1.0f;
        private float _playerIconRadiusModifier = 1f;
        private float _detectionCircleModifier = 1.0f;
        // mouseover distance modifier for mob icon tooltips
        private float _mouseOverDistanceModifier = 2.5f;
        // icon-related thickness
        private float _detectionCircleThickness = 3f;
        private float _directionLineThickness = 3f;

        // on-screen text positions and colours
        private float _zoneInfoPosXPercentage = 3.5f;
        private float _zoneInfoPosYPercentage = 11.1f;
        private float _worldInfoPosXPercentage = 0.45f;
        private float _worldInfoPosYPercentage = 8.3f;
        private float _priorityMobInfoPosXPercentage = 35f;
        private float _priorityMobInfoPosYPercentage = 2.5f;
        private float _nearbyMobListPosXPercentage = .42f;
        private float _nearbyMobListPosYPercentage = 69f;
        // text colour
        private Vector4 _zoneTextColour = new Vector4(0.282353f, 0.76863f, 0.69412f, 1f); //blue-greenish
        private Vector4 _zoneTextColourAlt = new Vector4(0, 0.4353f, 0.36863f, 1f); //same but darker
        private Vector4 _worldTextColour = new Vector4(0.77255f, 0.3412f, 0.612f, 1f); //purply
        private Vector4 _worldTextColourAlt = new Vector4(0.51765f, 0, 0.3294f, 1f); //same but darker
        private Vector4 _priorityMobTextColour = Vector4.One;
        private Vector4 _priorityMobTextColourAlt = Vector4.One;
        private Vector4 _nearbyMobListColour = Vector4.One;
        private Vector4 _nearbyMobListColourAlt = Vector4.One;
        private Vector4 _priorityMobColourBackground = new Vector4(1f, 0.43529411764705883f, 0.5137254901960784f, 0f); //nicely pink :D
        private Vector4 _nearbyMobListColourBackground = new Vector4(1f, 0.43529411764705883f, 0.5137254901960784f, 0f); //nicely pink :D

        //checkbox bools
        private bool _priorityMobEnabled = true;
        private bool _nearbyMobListEnabled = true;
        private bool _showZoneName = true;
        private bool _showWorldName = true;
        private bool _saveSpawnData = true;
        private bool _useMapImages = false;

        //initial window position
        private Vector2 _mapWindowPos = new Vector2(25, 25);
        private Vector4 _mapWindowColour = new Vector4(0f, 0f, 0f, 0.2f); //alpha / w value isn't used
        private float _mapWindowOpacityAsPercentage = 20f;
        //window sizes
        private int _currentWindowSize = 512;
        private int _presetOneWindowSize = 512;
        private int _presetTwoWindowSize = 1024;

        //Hunt Window Flag - used for toggling title bar
        private int _huntWindowFlag = (int)(ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoTitleBar);

        //notification stuff
        private string _ttsVoiceName;
        private string _ttsAMessage = string.Empty;
        private string _ttsBMessage = string.Empty;
        private string _ttsSMessage = string.Empty;
        private string _chatAMessage = string.Empty;
        private string _chatBMessage = string.Empty;
        private string _chatSMessage = string.Empty;
        private bool _ttsAEnabled = false;
        private bool _ttsBEnabled = false;
        private bool _ttsSEnabled = false;
        private bool _chatAEnabled = false;
        private bool _chatBEnabled = false;
        private bool _chatSEnabled = false;


        #endregion


        private bool _showDebug = false;
        private bool _showOptionsWindow = true;

        private float _mapZoneMaxCoordSize = 41; //default to 41 as thats most common for hunt zones

        public float SingleCoordSize => ImGui.GetWindowSize().X / _mapZoneMaxCoordSize;

        //message input lengths
        private uint _inputTextMaxLength = 50;



        private readonly Vector4 _defaultTextColour = Vector4.One; //white


        //window bools
        private bool _mainWindowVisible = false;
        private bool _showDatabaseListWindow = false;

        public bool MainWindowVisible
        {
            get { return this._mainWindowVisible; }
            set { this._mainWindowVisible = value; }
        }

        private bool _mapVisible = false;
        public bool MapVisible
        {
            get => _mapVisible;
            set => _mapVisible = value;
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
            this._configuration = configuration;
            this._pluginInterface = pluginInterface; //not using atm...

            this._clientState = clientState;
            this._objectTable = objectTable;
            this._dataManager = dataManager;
            this._huntManager = huntManager;
            this._mapDataManager = mapDataManager;
            _ttsVoiceName = huntManager.TTS.Voice.Name; // load default voice first, then from settings if avail.
            _territoryName = String.Empty;

            ClientState_TerritoryChanged(null, 0);
            _clientState.TerritoryChanged += ClientState_TerritoryChanged;

            LoadSettings();
            LoadMapImages();

            //Task.Run(() => loop()); //for tts in background?
        }

        private int testCount = 0;
        private bool count = true;

        private async void loop()
        {
            while (count)
            {
                testCount++;
                Thread.Sleep(500);
            }
        }


        public void Dispose()
        {
            _huntManager.Dispose();
            SaveSettings();
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
            if (ImGui.Begin("Debug stuff", ref this._mainWindowVisible))
            {

                if (ImGui.Button("Settings"))
                {
                    SettingsVisible = true;
                }

                ImGui.Spacing();

                ImGui.Indent(55);
                ImGui.Unindent(55);

                ImGui.Text($"Territory: {_territoryName}");
                ImGui.Text($"Territory ID: {_clientState.TerritoryType}");

                //PLAYER POS
                var v3 = _clientState.LocalPlayer?.Position ?? new Vector3(0, 0, 0);
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
                foreach (var obj in this._objectTable)
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
            if (!MapVisible)
            {
                return;
            }


            ImGui.SetNextWindowSize(new Vector2(_currentWindowSize, _currentWindowSize), ImGuiCond.Always);
            ImGui.SetNextWindowSizeConstraints(new Vector2(512, -1), new Vector2(float.MaxValue, -1)); //disable manual resize vertical
            ImGui.SetNextWindowPos(_mapWindowPos, ImGuiCond.FirstUseEver);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(_mapWindowColour.X, _mapWindowColour.Y, _mapWindowColour.Z, _mapWindowOpacityAsPercentage / 100f));
            if (ImGui.Begin("Hunt Helper", ref this._mapVisible, (ImGuiWindowFlags)_huntWindowFlag))
            {
                _currentWindowSize = (int)ImGui.GetWindowSize().X;
                _mapWindowPos = ImGui.GetWindowPos();
                _mapZoneMaxCoordSize = _huntManager.GetMapZoneCoordSize(_territoryId);
                //if custom size not used, use these default sizes - resize with window size
                //radius for mob / spawn point circles - equal to half a map coord size
                _spawnPointIconRadius = _allRadiusModifier * _spawnPointRadiusModifier * (0.25f * SingleCoordSize);
                _mobIconRadius = _allRadiusModifier * _mobIconRadiusModifier * (0.25f * SingleCoordSize);
                _playerIconRadius = _allRadiusModifier * _playerIconRadiusModifier * (0.125f * SingleCoordSize); //default half of mob icon size

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
                    if (!_huntManager.ImagesLoaded) LoadMapImages(); //better way to do this...

                    var mapImg = _huntManager.GetMapImage(_territoryName);
                    if (mapImg != null)
                    {
                        ImGui.SetCursorPos(Vector2.Zero);
                        ImGui.Image(mapImg.ImGuiHandle, ImGui.GetWindowSize(), default(Vector2), new Vector2(1f, 1f), new Vector4(1f, 1f, 1f, _mapImageOpacityAsPercentage / 100));
                        ImGui.SetCursorPos(Vector2.Zero);
                    }
                }


                //show map coordinates when mouse is over gui
                ShowCoordOnMouseOver();

                //draw player icon and info
                UpdatePlayerInfo();

                //draw spawn points for the current map, if applicable.
                DrawSpawnPoints(_territoryId);

                UpdateMobInfo();

                //bottom docking window with buttons and options and stuff
                if (_showOptionsWindow)
                {
                    DrawOptionsWindow();
                }

                //putting this here instead because I want to draw it on this window, not a new one.
                if (_showDebug) ShowDebugInfo();

                //button to toggle bottom panel thing
                var cursorPos = new Vector2(8, ImGui.GetWindowSize().Y - 30);
                ImGui.SetCursorPos(cursorPos);
                //get a cogwheel img or something idk
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(.4f, .4f, .4f, 1f));
                if (ImGui.Button(" ö ")) _showOptionsWindow = !_showOptionsWindow;
                ImGui.PopStyleColor();
                if (_huntManager.ErrorPopUpVisible || _mapDataManager.ErrorPopUpVisible)
                {
                    ImGui.Begin("Error loading data");
                    ImGui.Text(_huntManager.ErrorMessage);
                    ImGui.Text(_mapDataManager.ErrorMessage);
                    ImGui.End();
                }

                //optional togglable stuff 

                //zone name
                if (_showZoneName)
                {
                    DoStuffWithMonoFont(() =>
                    {
                        SetCursorPosByPercentage(_zoneInfoPosXPercentage, _zoneInfoPosYPercentage);
                        if (!_useMapImages) ImGui.TextColored(_zoneTextColour, $"{_territoryName}");
                        if (_useMapImages) ImGui.TextColored(_zoneTextColourAlt, $"{_territoryName}");
                    });

                }
                if (_showWorldName)
                {
                    DoStuffWithMonoFont(() =>
                    {
                        SetCursorPosByPercentage(_worldInfoPosXPercentage, _worldInfoPosYPercentage);
                        if (!_useMapImages) ImGui.TextColored(_worldTextColour, $"{_worldName}");
                        if (_useMapImages) ImGui.TextColored(_worldTextColourAlt, $"{_worldName}");
                    });
                }
            }
            ImGui.PopStyleColor();
            ImGui.End();
            ImGui.PopStyleVar(2);
        }

        //move this
        public string text = "";
        private void DrawTriangleButtonForSidePanel()
        {
            /* ToDO
            * DRAW TRIANGLE !!
            */

            var drawlist = ImGui.GetWindowDrawList();
            Vector2 points = new Vector2(ImGui.GetWindowPos().X + 80, ImGui.GetWindowPos().Y + 50);
            drawlist.AddCircleFilled(points, 20, ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0f, 0f, 1f)), 1);
            var mousePos = ImGui.GetMousePos();
            if (mousePos.X >= points.X - 20 && mousePos.X <= points.X + 20)
            {
                if (mousePos.Y >= points.Y - 20 && mousePos.Y <= points.Y + 20)
                {
                    if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    {
                        text += " clicked";
                    }
                }
            }
            ImGui.SetCursorPosX(80);
            ImGui.SetCursorPosY(80);
            ImGui.Text(text);
            //DSADSASDASDSDJILASDJOAJDWIOADJIOQWJDOWJ
            //WQIODJQWDQDWQDOKPQWDKOPQWDKOPKWOQPKDPQWDK




        }

        //break this down? lazyu- could def make functions for repeat gui element sets...
        private void DrawOptionsWindow()
        {
            var bottomDockingPos = Vector2.Add(ImGui.GetWindowPos(), new Vector2(0, ImGui.GetWindowSize().Y));

            ImGui.SetNextWindowSize(new Vector2(ImGui.GetWindowSize().X, _bottomPanelHeight), ImGuiCond.Always);
            ImGui.SetNextWindowSizeConstraints(new Vector2(-1, 95), new Vector2(-1, 300));

            //hide grip color
            ImGui.PushStyleColor(ImGuiCol.ResizeGrip, 0);
            if (ImGui.Begin("Options Window", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove))
            {
                ImGui.SetWindowPos(bottomDockingPos);
                ImGui.Dummy(new Vector2(0, 2f));
                ImGui.Columns(2);
                ImGui.SetColumnWidth(0, ImGui.GetWindowSize().X / 5);
                ImGui.SetColumnWidth(1, ImGui.GetWindowSize().X);
                ImGui.Checkbox("Map Image", ref _useMapImages);
                ImGui.SameLine();
                ImGui_HelpMarker("Use a map image instead of blank background\n\n" +
                                 "\t\t=======================================\n" +
                                 "\t\tMap Images Created By Cable Monkey of Goblin\n" +
                                 "         \t\t   http://cablemonkey.us/huntmap2/\n" +
                                 "         \t\t   Same thing for spawn point data :D\n" +
                                 "\t\t=======================================");

                ImGui.CheckboxFlags("Hide Title Bar", ref _huntWindowFlag, 1);
                ImGui.SameLine();
                ImGui_HelpMarker("Wasn't initially designed to have a title bar, things might need adjusting -->");

                ImGui.Checkbox("Show Debug", ref _showDebug);
                ImGui.SameLine();
                ImGui_HelpMarker("idk shows random debug info i used");

                ImGui.Checkbox("Save Spawn Data", ref _saveSpawnData);
                ImGui.SameLine();
                ImGui_HelpMarker("Saves S Rank Information to desktop txt (ToDo)");

                //ImGui.Dummy(new Vector2(0, 4f));

                if (ImGui.Button("Loaded Hunt Data")) _showDatabaseListWindow = !_showDatabaseListWindow;
                ImGui.SameLine();
                ImGui_HelpMarker("Show the loaded hunt and spawn point data");
                DrawDataBaseWindow();




                Debug_OptionsWindowTable_ShowWindowSize();



                ImGui.NextColumn();
                if (ImGui.BeginTabBar("Options", ImGuiTabBarFlags.None))
                {
                    if (ImGui.BeginTabItem("General"))
                    {
                        _bottomPanelHeight = 212f;
                        var widgetWidth = 69f;

                        var tableSizeX = ImGui.GetContentRegionAvail().X;
                        var tableSizeY = ImGui.GetContentRegionAvail().Y;

                        ImGui.Dummy(new Vector2(0, 8f));



                        if (ImGui.BeginChild("general left side", new Vector2(1.2f * tableSizeX / 3, 0f)))
                        {
                            if (ImGui.BeginTable("General Options table", 2))
                            {
                                /*
                                 *  CHANGE THESE TO PERCENTAGE OF SCREEN SIZE
                                 * OR THEY'LL CLIP OFF SCREEN WHEN RESIZED
                                 */

                                ImGui.TableNextColumn();
                                ImGui.Checkbox("Zone Name", ref _showZoneName);
                                ImGui.SameLine();
                                ImGui_HelpMarker("Shows Zone Name\nAdjust position below as % of window.");
                                ImGui.PushItemWidth(widgetWidth);
                                ImGui.DragFloat("Zone X", ref _zoneInfoPosXPercentage, 0.05f, 0, 100, "%.2f",
                                    ImGuiSliderFlags.NoRoundToFormat);
                                ImGui.SameLine();
                                ImGui_HelpMarker("Ctrl+Click to enter manually, Shift+Drag to speed up.");
                                ImGui.DragFloat("Zone Y", ref _zoneInfoPosYPercentage, 0.05f, 0, 100, "%.2f",
                                    ImGuiSliderFlags.NoRoundToFormat);

                                ImGui.ColorEdit4("Colour##Zone", ref _zoneTextColour,
                                    ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.PickerHueWheel);
                                ImGui.ColorEdit4("Alt##Zone", ref _zoneTextColourAlt,
                                    ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.PickerHueWheel);

                                ImGui.SameLine();
                                ImGui_HelpMarker("Alternate colour for when map image is used.");

                                ImGui.TableNextColumn();
                                ImGui.Checkbox("World Name", ref _showWorldName);
                                ImGui.SameLine();
                                ImGui_HelpMarker(
                                    "Shows World Name\nAdjust position below as % of window");
                                ImGui.PushItemWidth(widgetWidth);
                                ImGui.DragFloat("World X", ref _worldInfoPosXPercentage, 0.05f, 0, 100f, "%.2f",
                                    ImGuiSliderFlags.NoRoundToFormat);
                                ImGui.DragFloat("World Y", ref _worldInfoPosYPercentage, 0.05f, 0, 100f, "%.2f",
                                    ImGuiSliderFlags.NoRoundToFormat);

                                ImGui.ColorEdit4("Colour##World", ref _worldTextColour,
                                    ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.PickerHueWheel);
                                ImGui.ColorEdit4("Alt##World", ref _worldTextColourAlt,
                                    ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.PickerHueWheel);
                                ImGui.SameLine();
                                ImGui_HelpMarker("Alternate colour for when map image is used.");

                                ImGui.EndTable();
                            }

                            ImGui.EndChild();
                        }


                        ImGui.SameLine();
                        if (ImGui.BeginChild("general right side resize section",
                                new Vector2((1.8f * tableSizeX / 3) - 8f, tableSizeY - 24f), true))
                        {
                            if (ImGui.BeginTable("right side table", 4))
                            {
                                ImGui.TableSetupColumn("test",
                                    ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize);

                                var intWidgetWidth = 36;

                                // Current Window Size
                                ImGui.TableNextColumn();
                                ImGui.Dummy(new Vector2(0f, 0f));
                                ImGui.Dummy(new Vector2(0f, 0f));
                                ImGui.SameLine();
                                ImGui.Text("Window Size");
                                ImGui.SameLine();
                                ImGui_HelpMarker("Current Window Size");

                                ImGui.TableNextColumn();
                                ImGui.TableSetupColumn("test", ImGuiTableColumnFlags.WidthFixed, 5f);
                                ImGui.Dummy(new Vector2(0f, 0f));
                                ImGui.PushItemWidth(intWidgetWidth);
                                ImGui.InputInt("", ref _currentWindowSize, 0);
                                ImGui.PopID(); //popid so it can be used by another element

                                ImGui.TableNextColumn();
                                ImGui.Dummy(new Vector2(0f, 0f));
                                ImGui.ColorEdit4("##Window Colour", ref _mapWindowColour, ImGuiColorEditFlags.PickerHueWheel | ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoAlpha);
                                ImGui.SameLine(); ImGui_HelpMarker("Window Colour - Opacity can be changed down below.");

                                ImGui.TableNextColumn();

                                // Window Size Preset 1
                                ImGui.TableNextColumn();
                                ImGui.Dummy(new Vector2(0f, 0f));
                                ImGui.Dummy(new Vector2(0f, 0f));
                                ImGui.SameLine();
                                ImGui.Text("Preset 1");
                                ImGui.SameLine();
                                ImGui_HelpMarker(
                                    "Save a preset for quick switching\nOnly need to type it in.\nPress Apply to change to this size");

                                ImGui.TableNextColumn();
                                ImGui.Dummy(new Vector2(0f, 0f));
                                ImGui.PushItemWidth(intWidgetWidth);
                                ImGui.InputInt("", ref _presetOneWindowSize, 0);
                                ImGui.PopID();

                                ImGui.TableNextColumn();
                                ImGui.Dummy(new Vector2(0f, 0f));
                                if (ImGui.Button("Apply")) _currentWindowSize = _presetOneWindowSize;
                                ImGui.PopID();

                                ImGui.TableNextColumn();


                                // Window Size Preset 2
                                ImGui.TableNextColumn();
                                ImGui.Dummy(new Vector2(0f, 0f));
                                ImGui.Dummy(new Vector2(0f, 0f));
                                ImGui.SameLine();
                                ImGui.Text("Preset 2");
                                ImGui.SameLine();
                                ImGui_HelpMarker(
                                    "Save a preset for quick switching\nOnly need to type it in.\nPress Apply to change to this size");

                                ImGui.TableNextColumn();
                                ImGui.Dummy(new Vector2(0f, 0f));
                                ImGui.PushItemWidth(intWidgetWidth);
                                ImGui.InputInt("", ref _presetTwoWindowSize, 0);
                                ImGui.PopID();

                                ImGui.TableNextColumn();
                                ImGui.Dummy(new Vector2(0f, 0f));
                                if (ImGui.Button("Apply")) _currentWindowSize = _presetTwoWindowSize;
                                ImGui.SameLine();
                                ImGui_HelpMarker("why button go right :(");
                                ImGui.PopID();
                                ImGui.EndTable();

                                //Map Image Opacity Slider
                                ImGui.Separator();
                                //ImGui_CentreText("Map Opacity", Vector4.One);
                                ImGui.Dummy(new Vector2(0, 0));
                                ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - 16f);
                                ImGui.SameLine();
                                ImGui.DragFloat("##Map Opacity", ref _mapImageOpacityAsPercentage, .2f, 0f, 100f,
                                    "Map Opacity - %.0f");

                                //Map Window Opacity Slider
                                //ImGui.Separator();
                                //ImGui_CentreText("Window Opacity", Vector4.One);
                                ImGui.Dummy(new Vector2(0, 0));
                                ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - 16f);
                                ImGui.SameLine();
                                ImGui.DragFloat("##Map Window Opacity", ref _mapWindowOpacityAsPercentage, .2f, 0f, 100f,
                                    "Window Opacity - %.0f");
                            }

                            ImGui.EndChild();
                        }

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Visuals"))
                    {
                        _bottomPanelHeight = 165f;

                        if (ImGui.BeginTabBar("Visuals sub-bar"))
                        {
                            if (ImGui.BeginTabItem("Sizing"))
                            {
                                ImGui.Dummy(new Vector2(0, 2f));
                                if (ImGui.BeginTable("Sizing Options Table", 3))
                                {
                                    var widgetWidth = 40f;

                                    ImGui.TableNextColumn();
                                    ImGui.PushItemWidth(widgetWidth);
                                    ImGui.InputFloat("Player Modifier", ref _playerIconRadiusModifier, 0, 0, "%.2f");
                                    ImGui.SameLine();
                                    ImGui_HelpMarker("Player Icon Radius Modifier: Default 1");

                                    ImGui.TableNextColumn();
                                    ImGui.PushItemWidth(widgetWidth);
                                    ImGui.InputFloat("Mob Modifier", ref _mobIconRadiusModifier, 0, 0, "%.2f");
                                    ImGui.SameLine();
                                    ImGui_HelpMarker("Mob Icon Radius Modifier, default: 2");

                                    ImGui.TableNextColumn();
                                    ImGui.PushItemWidth(widgetWidth);
                                    ImGui.InputFloat("Spawn Point Modifier", ref _spawnPointRadiusModifier, 0, 0,
                                        "%.2f");
                                    ImGui.SameLine();
                                    ImGui_HelpMarker("Spawn Point Radius Modifier, default: 1.0");

                                    ImGui.TableNextColumn();
                                    ImGui.InputFloat("All Modifier", ref _allRadiusModifier, 0, 0, "%.2f");
                                    ImGui.SameLine();
                                    ImGui_HelpMarker(
                                        "Increase all icons proportionally, default: 1\nBy Default, A Mob icon is 1.5x a Spawn Point, a Player icon is 0.2x a Spawn Point");

                                    ImGui.TableNextColumn();
                                    ImGui.InputFloat("Detection Circle Modifier", ref _detectionCircleModifier, 0, 0,
                                        "%.2f");
                                    ImGui.SameLine();
                                    ImGui_HelpMarker(
                                        "Default represents 2 in-game coordinates. Modify if you feel this is inaccurate, default: 1.0");

                                    ImGui.TableNextColumn();
                                    ImGui.Dummy(new Vector2(0, 26f));

                                    ImGui.TableNextColumn();
                                    ImGui.Separator();
                                    ImGui.Dummy(new Vector2(0, 1f));
                                    ImGui.InputFloat("Detection Circle Thickness", ref _detectionCircleThickness, 0, 0,
                                        "%.2f");
                                    ImGui.SameLine();
                                    ImGui_HelpMarker("default: 3.0");

                                    ImGui.TableNextColumn();
                                    ImGui.Separator();
                                    ImGui.Dummy(new Vector2(0, 1f));
                                    ImGui.InputFloat("Direction Line Thickness", ref _directionLineThickness, 0, 0,
                                        "%.2f");
                                    ImGui.SameLine();
                                    ImGui_HelpMarker("default: 3.0");

                                    ImGui.TableNextColumn();
                                    ImGui.Separator();
                                    ImGui.Dummy(new Vector2(0, 1f));
                                    if (ImGui.Button("Reset"))
                                    {
                                        _allRadiusModifier = 1.0f;
                                        _mobIconRadiusModifier = 2f;
                                        _spawnPointRadiusModifier = 1.0f;
                                        _playerIconRadiusModifier = 1f;
                                        _detectionCircleModifier = 1.0f;
                                        _detectionCircleThickness = 3f;
                                        _directionLineThickness = 3f;
                                    }

                                    ImGui.SameLine();
                                    ImGui_HelpMarker("Reset all sizes to default.");

                                    ImGui.PopItemWidth();

                                    ImGui.EndTable();
                                }

                                ImGui.EndTabItem();
                            }

                            if (ImGui.BeginTabItem("Colours"))
                            {
                                ImGui.Dummy(new Vector2(0, 2f));

                                if (ImGui.BeginTable("Colour Options Table", 3))
                                {
                                    ImGui.Dummy(new Vector2(0, 2f));

                                    ImGui.TableNextColumn();
                                    var color3 = new Vector4(0f, 0f, 1f, 1f);
                                    ImGui.ColorEdit4("Player Icon", ref _playerIconColour,
                                        ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.PickerHueWheel);

                                    ImGui.TableNextColumn();
                                    var color = new Vector4(1f, 1f, 1f, 1f);
                                    ImGui.ColorEdit4("Mob Icon", ref _mobColour,
                                        ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.PickerHueWheel);

                                    ImGui.TableNextColumn();
                                    ImGui.ColorEdit4("Spawn Point", ref _spawnPointColour,
                                        ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.PickerHueWheel);

                                    ImGui.Dummy(new Vector2(0, 4f));

                                    ImGui.TableNextColumn();
                                    var color2 = new Vector4(0f, 1f, 0f, 1f);
                                    ImGui.ColorEdit4("Player Background", ref _playerIconBackgroundColour,
                                        ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.PickerHueWheel);
                                    ImGui.SameLine();
                                    ImGui_HelpMarker(
                                        "Background of the player / detection circle.\nChange the Alpha A: for opacity.");


                                    ImGui.Dummy(new Vector2(0, 4f));

                                    ImGui.TableNextColumn();
                                    var color4 = new Vector4(0f, 0f, 1f, 1f);
                                    ImGui.ColorEdit4("Direction Line", ref _directionLineColour,
                                        ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.PickerHueWheel);

                                    ImGui.TableNextColumn();
                                    var color5 = new Vector4(0f, 0f, 1f, 1f);
                                    ImGui.ColorEdit4("Detection Circle", ref _detectionCircleColour,
                                        ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.PickerHueWheel);

                                    ImGui.EndTable();
                                }

                                ImGui.EndTabItem();
                            }

                            ImGui.EndTabBar();
                        }

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Notifications"))
                    {
                        _bottomPanelHeight = 145f;

                        if (ImGui.BeginTabBar("Notifications sub-bar"))
                        {
                            //ImGui.PushFont(UiBuilder.MonoFont); //aligns things, but then looks ugly so idk.. table?
                            if (ImGui.BeginTabItem(" A "))
                            {
                                ImGui.Dummy(new Vector2(0, 2f));
                                ImGui.TextUnformatted("Chat Message");
                                ImGui.SameLine();
                                ImGui.InputText("##A Rank A Chat Msg", ref _chatAMessage, _inputTextMaxLength);
                                ImGui.SameLine();
                                ImGui.Checkbox("##A Rank A Chat Checkbox", ref _chatAEnabled);
                                ImGui.SameLine();
                                ImGui_HelpMarker("Message to send to chat using /echo, usable tags: <pos> <name> <rank> <hpp>.");

                                ImGui.Dummy(new Vector2(0, 2f));
                                ImGui.TextUnformatted("TTS   Message");
                                ImGui.SameLine();
                                ImGui.InputText("##A Rank A TTS Msg", ref _ttsAMessage, _inputTextMaxLength);
                                ImGui.SameLine();
                                ImGui.Checkbox("##A Rank A TTS Checkbox", ref _ttsAEnabled);
                                ImGui.SameLine();
                                ImGui_HelpMarker("Message to use with Text-to-Speech, usable tags: <name> <rank>");

                                ImGui.EndTabItem();
                            }

                            if (ImGui.BeginTabItem(" B "))
                            {
                                ImGui.Dummy(new Vector2(0, 2f));
                                ImGui.TextUnformatted("Chat Message");
                                ImGui.SameLine();
                                ImGui.InputText("##A Rank B Chat Msg", ref _chatBMessage, _inputTextMaxLength);
                                ImGui.SameLine();
                                ImGui.Checkbox("##A Rank B Chat Checkbox", ref _chatBEnabled);
                                ImGui.SameLine();
                                ImGui_HelpMarker(
                                    "Message to send to chat using /echo, usable tags: <pos> <name> <rank> <hpp>.");

                                ImGui.Dummy(new Vector2(0, 2f));
                                ImGui.TextUnformatted("TTS   Message");
                                ImGui.SameLine();
                                ImGui.InputText("##A Rank B TTS Msg", ref _ttsBMessage, _inputTextMaxLength);
                                ImGui.SameLine();
                                ImGui.Checkbox("##A Rank B TTS Checkbox", ref _ttsBEnabled);
                                ImGui.SameLine();
                                ImGui_HelpMarker("Message to use with Text-to-Speech, usable tags: <name> <rank>");

                                ImGui.EndTabItem();
                            }

                            if (ImGui.BeginTabItem(" S "))
                            {
                                ImGui.Dummy(new Vector2(0, 2f));
                                ImGui.TextUnformatted("Chat Message");
                                ImGui.SameLine();
                                ImGui.InputText("##A Rank S Chat Msg", ref _chatSMessage, _inputTextMaxLength);
                                ImGui.SameLine();
                                ImGui.Checkbox("##A Rank S Chat Checkbox", ref _chatSEnabled);
                                ImGui.SameLine();
                                ImGui_HelpMarker(
                                    "Message to send to chat using /echo, usable tags: <pos> <name> <rank> <hpp>.");

                                ImGui.Dummy(new Vector2(0, 2f));
                                ImGui.TextUnformatted("TTS   Message");
                                ImGui.SameLine();
                                ImGui.InputText("##A Rank S TTS Msg", ref _ttsSMessage, _inputTextMaxLength);
                                ImGui.SameLine();
                                ImGui.Checkbox("##A Rank S TTS Checkbox", ref _ttsSEnabled);
                                ImGui.SameLine();
                                ImGui_HelpMarker("Message to use with Text-to-Speech, usable tags: <name> <rank>");

                                ImGui.EndTabItem();
                            }

                            if (ImGui.BeginTabItem(" TTS Settings "))
                            {
                                var tts = _huntManager.TTS;
                                var voiceList = tts.GetInstalledVoices();
                                var listOfVoiceNames = new string[voiceList.Count];
                                for (int i = 0; i < voiceList.Count; i++)
                                {
                                    listOfVoiceNames[i] = voiceList[i].VoiceInfo.Name;
                                }
                                var itemPos = Array.IndexOf(listOfVoiceNames, _ttsVoiceName);
                                ImGui.Dummy(new Vector2(0f, 5f));
                                ImGui.Text("Select Voice:"); ImGui.SameLine();
                                if (ImGui.Combo("##TTS Voice Combo", ref itemPos, listOfVoiceNames, listOfVoiceNames.Length))
                                {
                                    tts.SelectVoice(listOfVoiceNames[itemPos]);
                                    _ttsVoiceName = listOfVoiceNames[itemPos]; ;
                                    tts.SpeakAsync($"{_ttsAMessage}");
                                    tts.SpeakAsync($"{_ttsBMessage}");
                                    tts.SpeakAsync($"{_ttsSMessage}");
                                }


                                ImGui.EndTabItem();
                            }

                            ImGui.EndTabBar();
                        }

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem(
                            "Onscreen Mob Info")) //what do i call this, mob context, mob text, hunt info data text idk
                    {
                        _bottomPanelHeight = 185f;

                        if (ImGui.BeginTable("Mob info Table", 2))
                        {
                            ImGui.TableNextColumn();
                            ImGui.Checkbox("Priority Mob", ref _priorityMobEnabled);
                            ImGui.SameLine();
                            ImGui_HelpMarker("The big thing that labels highest rank mob in zone, S/SS > A > B\n\n" +
                                             "Ctrl+Click to enter manually, Shift+Click for fast drag.");
                            ImGui.DragFloat("X##Priority Mob", ref _priorityMobInfoPosXPercentage, 0.05f, 0, 100,
                                "%.2f", ImGuiSliderFlags.NoRoundToFormat);
                            ImGui.DragFloat("Y##Priority Mob", ref _priorityMobInfoPosYPercentage, 0.05f, 0, 100,
                                "%.2f", ImGuiSliderFlags.NoRoundToFormat);
                            ImGui.ColorEdit4("Main##PriorityMain", ref _priorityMobTextColour,
                                ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.PickerHueWheel);
                            ImGui.SameLine();
                            ImGui.ColorEdit4("Alt##PriorityAlt", ref _priorityMobTextColourAlt,
                                ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.PickerHueWheel);
                            ImGui.SameLine();
                            ImGui.ColorEdit4("Background##PriorityAlt", ref _priorityMobColourBackground,
                                ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.PickerHueWheel);
                            ImGui.SameLine();
                            ImGui_HelpMarker(
                                "Main = without map, Alt = with map images, Background = background duh \n\n- Turn up Alpha A: for solid colour.");

                            ImGui.TableNextColumn();
                            ImGui.Checkbox("Nearby Mob List", ref _nearbyMobListEnabled);
                            ImGui.SameLine();
                            ImGui_HelpMarker("A list of all nearby mobs.\n\n" +
                                             "Ctrl+Click to enter manually, Shift+Click for fast drag.");
                            ImGui.DragFloat("X##Nearby Mobs", ref _nearbyMobListPosXPercentage, 0.05f, 0, 100, "%.2f",
                                ImGuiSliderFlags.NoRoundToFormat);
                            ImGui.DragFloat("Y##Nearby Mobs", ref _nearbyMobListPosYPercentage, 0.05f, 0, 100, "%.2f",
                                ImGuiSliderFlags.NoRoundToFormat);
                            ImGui.ColorEdit4("Main##Nearby Mobs", ref _nearbyMobListColour,
                                ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.PickerHueWheel);
                            ImGui.SameLine();
                            ImGui.ColorEdit4("Alt##Nearby Alt", ref _nearbyMobListColourAlt,
                                ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.PickerHueWheel);
                            ImGui.SameLine();
                            ImGui.ColorEdit4("Background##Nearby Mobs", ref _nearbyMobListColourBackground,
                                ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.PickerHueWheel);
                            ImGui.SameLine();
                            ImGui_HelpMarker(
                                "Main = without map, Alt = with map images, Background = background duh \n\n- Turn up Alpha A: for solid colour.");

                            ImGui.EndTable();
                        }

                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }
            }
            ImGui.PopStyleColor();
            ImGui.End();
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
                var configValue = this._configuration.UseMapImages;
                if (ImGui.Checkbox("Use Map Image Bool", ref configValue))
                {
                    this._configuration.UseMapImages = configValue;
                    // can save immediately on change, if you don't want to provide a "Save and Close" button
                    //this._configuration.Save();
                }

                if (ImGui.Button("Show Loaded Hunt Data"))
                {
                    _showDatabaseListWindow = !_showDatabaseListWindow;
                }

                DrawDataBaseWindow();
            }
            ImGui.End();
        }

        private void SaveSettings()
        {
            _configuration.PriorityMobEnabled = _priorityMobEnabled;
            _configuration.NearbyMobListEnabled = _nearbyMobListEnabled;
            _configuration.ShowZoneName = _showZoneName;
            _configuration.ShowWorldName = _showWorldName;
            _configuration.SaveSpawnData = _saveSpawnData;
            _configuration.UseMapImages = _useMapImages;
            _configuration.MapWindowPos = _mapWindowPos;
            _configuration.CurrentWindowSize = _currentWindowSize;
            _configuration.PresetOneWindowSize = _presetOneWindowSize;
            _configuration.PresetTwoWindowSize = _presetTwoWindowSize;
            _configuration.MapImageOpacityAsPercentage = _mapImageOpacityAsPercentage;
            _configuration.SpawnPointColour = _spawnPointColour;
            _configuration.MobColour = _mobColour;
            _configuration.PlayerIconColour = _playerIconColour;
            _configuration.PlayerIconBackgroundColour = _playerIconBackgroundColour;
            _configuration.DirectionLineColour = _directionLineColour;
            _configuration.DetectionCircleColour = _detectionCircleColour;
            _configuration.AllRadiusModifier = _allRadiusModifier;
            _configuration.MobIconRadiusModifier = _mobIconRadiusModifier;
            _configuration.SpawnPointRadiusModifier = _spawnPointRadiusModifier;
            _configuration.PlayerIconRadiusModifier = _playerIconRadiusModifier;
            _configuration.DetectionCircleModifier = _detectionCircleModifier;
            _configuration.MouseOverDistanceModifier = _mouseOverDistanceModifier;
            _configuration.DetectionCircleThickness = _detectionCircleThickness;
            _configuration.DirectionLineThickness = _directionLineThickness;
            _configuration.ZoneInfoPosXPercentage = _zoneInfoPosXPercentage;
            _configuration.ZoneInfoPosYPercentage = _zoneInfoPosYPercentage;
            _configuration.WorldInfoPosXPercentage = _worldInfoPosXPercentage;
            _configuration.WorldInfoPosYPercentage = _worldInfoPosYPercentage;
            _configuration.PriorityMobInfoPosXPercentage = _priorityMobInfoPosXPercentage;
            _configuration.PriorityMobInfoPosYPercentage = _priorityMobInfoPosYPercentage;
            _configuration.NearbyMobListPosXPercentage = _nearbyMobListPosXPercentage;
            _configuration.NearbyMobListPosYPercentage = _nearbyMobListPosYPercentage;
            _configuration.ZoneTextColour = _zoneTextColour;
            _configuration.ZoneTextColourAlt = _zoneTextColourAlt;
            _configuration.WorldTextColour = _worldTextColour;
            _configuration.WorldTextColourAlt = _worldTextColourAlt;
            _configuration.PriorityMobTextColour = _priorityMobTextColour;
            _configuration.PriorityMobTextColourAlt = _priorityMobTextColourAlt;
            _configuration.NearbyMobListColour = _nearbyMobListColour;
            _configuration.NearbyMobListColourAlt = _nearbyMobListColourAlt;
            _configuration.PriorityMobColourBackground = _priorityMobColourBackground;
            _configuration.NearbyMobListColourBackground = _nearbyMobListColourBackground;
            _configuration.HuntWindowFlag = _huntWindowFlag;
            _configuration.MapWindowColour = _mapWindowColour;
            _configuration.MapWindowOpacityAsPercentage = _mapWindowOpacityAsPercentage;
            _configuration.TTSVoiceName = _ttsVoiceName;
            _configuration.TTSAMessage = _ttsAMessage;
            _configuration.TTSBMessage = _ttsBMessage;
            _configuration.TTSSMessage = _ttsSMessage;
            _configuration.ChatAMessage = _chatAMessage;
            _configuration.ChatBMessage = _chatBMessage;
            _configuration.ChatSMessage = _chatSMessage;
            _configuration.TTSAEnabled = _ttsAEnabled;
            _configuration.TTSBEnabled = _ttsBEnabled;
            _configuration.TTSSEnabled = _ttsSEnabled;
            _configuration.ChatAEnabled = _chatAEnabled;
            _configuration.ChatBEnabled = _chatBEnabled;
            _configuration.ChatSEnabled = _chatSEnabled;

            this._configuration.Save();
        }

        private void LoadSettings()
        {
            _priorityMobEnabled = _configuration.PriorityMobEnabled;
            _nearbyMobListEnabled = _configuration.NearbyMobListEnabled;
            _showZoneName = _configuration.ShowZoneName;
            _showWorldName = _configuration.ShowWorldName;
            _saveSpawnData = _configuration.SaveSpawnData;
            _useMapImages = _configuration.UseMapImages;
            _mapWindowPos = _configuration.MapWindowPos;
            _currentWindowSize = _configuration.CurrentWindowSize;
            _presetOneWindowSize = _configuration.PresetOneWindowSize;
            _presetTwoWindowSize = _configuration.PresetTwoWindowSize;
            _mapImageOpacityAsPercentage = _configuration.MapImageOpacityAsPercentage;
            _spawnPointColour = _configuration.SpawnPointColour;
            _mobColour = _configuration.MobColour;
            _playerIconColour = _configuration.PlayerIconColour;
            _playerIconBackgroundColour = _configuration.PlayerIconBackgroundColour;
            _directionLineColour = _configuration.DirectionLineColour;
            _detectionCircleColour = _configuration.DetectionCircleColour;
            _allRadiusModifier = _configuration.AllRadiusModifier;
            _mobIconRadiusModifier = _configuration.MobIconRadiusModifier;
            _spawnPointRadiusModifier = _configuration.SpawnPointRadiusModifier;
            _playerIconRadiusModifier = _configuration.PlayerIconRadiusModifier;
            _detectionCircleModifier = _configuration.DetectionCircleModifier;
            _mouseOverDistanceModifier = _configuration.MouseOverDistanceModifier;
            _detectionCircleThickness = _configuration.DetectionCircleThickness;
            _directionLineThickness = _configuration.DirectionLineThickness;
            _zoneInfoPosXPercentage = _configuration.ZoneInfoPosXPercentage;
            _zoneInfoPosYPercentage = _configuration.ZoneInfoPosYPercentage;
            _worldInfoPosXPercentage = _configuration.WorldInfoPosXPercentage;
            _worldInfoPosYPercentage = _configuration.WorldInfoPosYPercentage;
            _priorityMobInfoPosXPercentage = _configuration.PriorityMobInfoPosXPercentage;
            _priorityMobInfoPosYPercentage = _configuration.PriorityMobInfoPosYPercentage;
            _nearbyMobListPosXPercentage = _configuration.NearbyMobListPosXPercentage;
            _nearbyMobListPosYPercentage = _configuration.NearbyMobListPosYPercentage;
            _zoneTextColour = _configuration.ZoneTextColour;
            _zoneTextColourAlt = _configuration.ZoneTextColourAlt;
            _worldTextColour = _configuration.WorldTextColour;
            _worldTextColourAlt = _configuration.WorldTextColourAlt;
            _priorityMobTextColour = _configuration.PriorityMobTextColour;
            _priorityMobTextColourAlt = _configuration.PriorityMobTextColourAlt;
            _nearbyMobListColour = _configuration.NearbyMobListColour;
            _nearbyMobListColourAlt = _configuration.NearbyMobListColourAlt;
            _priorityMobColourBackground = _configuration.PriorityMobColourBackground;
            _nearbyMobListColourBackground = _configuration.NearbyMobListColourBackground;
            _huntWindowFlag = _configuration.HuntWindowFlag;
            _mapWindowColour = _configuration.MapWindowColour;
            _mapWindowOpacityAsPercentage = _configuration.MapWindowOpacityAsPercentage;
            _ttsAMessage = _configuration.TTSAMessage;
            _ttsBMessage = _configuration.TTSBMessage;
            _ttsSMessage = _configuration.TTSSMessage; 
            _chatAMessage = _configuration.ChatAMessage;
            _chatBMessage = _configuration.ChatBMessage;
            _chatSMessage = _configuration.ChatSMessage;
            _ttsAEnabled = _configuration.TTSAEnabled;
            _ttsBEnabled = _configuration.TTSBEnabled;
            _ttsSEnabled = _configuration.TTSSEnabled;
            _chatAEnabled = _configuration.ChatAEnabled;
            _chatBEnabled = _configuration.ChatBEnabled;
            _chatSEnabled = _configuration.ChatSEnabled;



            //if voice name available on user's pc, set as tts voice. --else default already set.
            if (_huntManager.TTS.GetInstalledVoices().Any(v => v.VoiceInfo.Name == _configuration.TTSVoiceName))
            {
                _ttsVoiceName = _configuration.TTSVoiceName;
                _huntManager.TTS.SelectVoice(_ttsVoiceName);
            }
        }


        private void ClientState_TerritoryChanged(object? sender, ushort e)
        {
            _territoryName = Utilities.MapHelpers.GetMapName(_dataManager, this._clientState.TerritoryType);
            //_worldName = _clientState.LocalPlayer?.CurrentWorld?.GameData?.Name.ToString() ?? "Not Found";
            _territoryId = _clientState.TerritoryType;
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
                    DoStuffWithMonoFont(() => ImGui_CentreText(_huntManager.GetDatabaseAsString(), _defaultTextColour));
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("spawn points"))
                {
                    ImGui.TextUnformatted(_mapDataManager.ToString());
                    ImGui.TextUnformatted("ik it's ugly, i'm sorry");
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
            var spawnPoints = _mapDataManager.GetSpawnPoints(mapID);
            if (spawnPoints.Count == 0) return;
            var drawList = ImGui.GetWindowDrawList();

            foreach (var sp in spawnPoints)
            {
                var drawPos = CoordinateToPositionInWindow(sp);
                drawList.AddCircleFilled(drawPos, _spawnPointIconRadius, ImGui.ColorConvertFloat4ToU32(_spawnPointColour));
            }
        }

        #region Mob


        private void UpdateMobInfo()
        {//logic is -> if mob doesn't exist in current list, add it -> if it does, add to removal list - > after processing all obj, remove from current any in removal
            var drawlist = ImGui.GetWindowDrawList();
            
            /*_huntManager.ClearMobs();
            var mobRemovalList = new List<uint>();
            foreach (var obj in this._objectTable)
            {
                if (obj is not BattleNpc mob) continue;
                if (!_huntManager.IsHunt(mob.NameId)) continue;
                if (!_huntManager.IsMobInCurrentMobList(mob.NameId)) 
                {   //add to list it not preexisting
                    _huntManager.AddMob(mob, _ttsAEnabled, _ttsBEnabled, _ttsSEnabled, _ttsAMessage, _ttsBMessage, _ttsSMessage);
                    continue;
                }
                DrawMobIcon(mob); 
            }
            _huntManager.RemoveFromCurrentMobsList(mobRemovalList);*/


            var nearbyMobs = new List<BattleNpc>();
            //sift through and add any hunt mobs to new list
            foreach (var obj in _objectTable)
            {
                if (obj is not BattleNpc mob) continue;
                if (!_huntManager.IsHunt(mob.NameId)) continue;
                nearbyMobs.Add(mob);
            }
            _huntManager.AddNearbyMobs(nearbyMobs, _ttsAEnabled, _ttsBEnabled, _ttsSEnabled, _ttsAMessage, _ttsBMessage, _ttsSMessage);

            var mobs = _huntManager.GetCurrentMobs();
            foreach (var mob in mobs)
            {
                DrawMobIcon(mob);
            }

            DrawPriorityMobInfo();
            DrawNearbyMobInfo();
        }

        private void DrawMobIcon(BattleNpc mob)
        {
            var mobPos = CoordinateToPositionInWindow(new Vector2(ConvertPosToCoordinate(mob.Position.X),
                ConvertPosToCoordinate(mob.Position.Z)));
            var drawlist = ImGui.GetWindowDrawList();
            drawlist.AddCircleFilled(mobPos, _mobIconRadius, ImGui.ColorConvertFloat4ToU32(_mobColour));

            //draw mob icon tooltip
            if (Vector2.Distance(ImGui.GetMousePos(), mobPos) < _mobIconRadius * _mouseOverDistanceModifier)
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
            if (!_priorityMobEnabled) return;

            var (rank, mob) = _huntManager.GetPriorityMob();
            if (mob == null) return;

            DoStuffWithMonoFont(() =>
            {
                SetCursorPosByPercentage(_priorityMobInfoPosXPercentage, _priorityMobInfoPosYPercentage);
                ImGui.PushStyleColor(ImGuiCol.Border, _priorityMobColourBackground);
                ImGui.PushStyleColor(ImGuiCol.ChildBg, _priorityMobColourBackground);
                ImGui.BeginChild("Priorty Mob Label", new Vector2(240f, 40f), true, ImGuiWindowFlags.NoScrollbar);
                var colour = _useMapImages ? _priorityMobTextColourAlt : _priorityMobTextColour; //if using map image, use alt colour.
                ImGui_CentreText(
                     $" {rank}  |  {mob.Name}  |  {Math.Round(((1.0 * mob.CurrentHp) / mob.MaxHp) * 100, 2):0.00}%%",
                     colour);
                ImGui_CentreText(
                    $"({ConvertPosToCoordinate(mob.Position.X):0.00}, {ConvertPosToCoordinate(mob.Position.Z):0.00})",
                    colour);
                ImGui.EndChild();
                ImGui.PopStyleColor(2);
            });
        }

        private void DrawNearbyMobInfo()
        {
            if (!_nearbyMobListEnabled) return;
            var nearbyMobs = _huntManager.CurrentMobs;
            if (nearbyMobs.Count == 0) return;

            var size = new Vector2(200f, 40f * nearbyMobs.Count);


            DoStuffWithMonoFont(() =>
            {
                SetCursorPosByPercentage(_nearbyMobListPosXPercentage, _nearbyMobListPosYPercentage);
                ImGui.PushStyleColor(ImGuiCol.Border, _nearbyMobListColourBackground);
                ImGui.PushStyleColor(ImGuiCol.ChildBg, _nearbyMobListColourBackground);
                ImGui.BeginChild("Nearby Mob List Info", size, true, ImGuiWindowFlags.NoScrollbar);
                var colour = _useMapImages ? _nearbyMobListColourAlt : _nearbyMobListColour; //if using map image, use alt colour.
                ImGui.Separator();
                foreach (var hunt in nearbyMobs)
                {
                    var mob = hunt.Mob;
                    ImGui_CentreText($"{hunt.Rank} | {mob.Name} \n " +
                                     $"{Math.Round((mob.CurrentHp * 1.0 / mob.MaxHp) * 100, 2)}%% | " +
                                     $"({ConvertPosToCoordinate(mob.Position.X)}, {ConvertPosToCoordinate(mob.Position.Y)})", colour);
                    ImGui.Separator();
                }
                ImGui.EndChild();
                ImGui.PopStyleColor(2);
            });


        }

        #endregion

        #region Player
        private void UpdatePlayerInfo()
        {
            //if this stuff ain't null, draw player position
            if (_clientState?.LocalPlayer?.Position != null)
            {
                var playerPos = CoordinateToPositionInWindow(
                    new Vector2(ConvertPosToCoordinate(_clientState.LocalPlayer.Position.X),
                        ConvertPosToCoordinate(_clientState.LocalPlayer.Position.Z)));

                var detectionRadius = 2 * SingleCoordSize * _detectionCircleModifier;
                var rotation = Math.Abs(_clientState.LocalPlayer.Rotation - Math.PI);
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
            drawlist.AddCircleFilled(playerPos, detectionRadius, ImGui.ColorConvertFloat4ToU32(_playerIconBackgroundColour));
            //direction line
            drawlist.AddLine(playerPos, lineEnding, ImGui.ColorConvertFloat4ToU32(_directionLineColour), _directionLineThickness);
            //player filled circle
            drawlist.AddCircleFilled(playerPos, _playerIconRadius, ImGui.ColorConvertFloat4ToU32(_playerIconColour));
            //detection circle
            drawlist.AddCircle(playerPos, detectionRadius, ImGui.ColorConvertFloat4ToU32(_detectionCircleColour), 0, _detectionCircleThickness);
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
            ImGui_TextColourSwapWhenUsingMapImage("Random Debug Info?!:");
            ImGui.BeginChild("debug");
            ImGui.Columns(2);
            ImGui_TextColourSwapWhenUsingMapImage($"Content1 Region: {ImGui.GetContentRegionAvail()}");
            ImGui_TextColourSwapWhenUsingMapImage($"Window Size: {ImGui.GetWindowSize()}");
            ImGui_TextColourSwapWhenUsingMapImage($"Window  Pos: {ImGui.GetWindowPos()}");

            if (_clientState?.LocalPlayer?.Position != null)
            {
                ImGui_TextColourSwapWhenUsingMapImage($"rotation: {_clientState.LocalPlayer.Rotation}");
                var rotation = Math.Abs(_clientState.LocalPlayer.Rotation - Math.PI);
                ImGui_TextColourSwapWhenUsingMapImage($"rotation rad.: {Math.Round(rotation, 2):0.00}");
                ImGui_TextColourSwapWhenUsingMapImage($"rotation sin.: {Math.Round(Math.Sin(rotation), 2):0.00}");
                ImGui_TextColourSwapWhenUsingMapImage($"rotation cos.: {Math.Round(Math.Cos(rotation), 2):0.00}");
                ImGui_TextColourSwapWhenUsingMapImage($"Player Pos: (" +
                                   $"{Utilities.MapHelpers.ConvertToMapCoordinate(_clientState.LocalPlayer.Position.X, _mapZoneMaxCoordSize):0.00}" +
                                   $"," +
                                   $" {Utilities.MapHelpers.ConvertToMapCoordinate(_clientState.LocalPlayer.Position.Z, _mapZoneMaxCoordSize):0.00}" +
                                   $")");
                var playerPos = CoordinateToPositionInWindow(
                    new Vector2(ConvertPosToCoordinate(_clientState.LocalPlayer.Position.X),
                        ConvertPosToCoordinate(_clientState.LocalPlayer.Position.Z)));
                ImGui_TextColourSwapWhenUsingMapImage($"Player Pos: {playerPos}");
                ImGui.NextColumn();
                ImGui_RightAlignText($"Map: {_territoryName} - {_territoryId}");
                ImGui_RightAlignText($"Coord size: {_mapZoneMaxCoordSize} ");

                //priority mob stuff
                ImGui_RightAlignText("Priority Mob:");
                var priorityMob = _huntManager.GetPriorityMob().Item2;
                if (priorityMob != null)
                {
                    ImGui_RightAlignText($"Rank {_huntManager.GetPriorityMob().Item1} " +
                                         $"{priorityMob.Name} " +
                                         $"{Math.Round(1.0 * priorityMob.CurrentHp / priorityMob.MaxHp * 100, 2):0.00}% | " +
                                         $"({ConvertPosToCoordinate(priorityMob.Position.X):0.00}, " +
                                         $"{ConvertPosToCoordinate(priorityMob.Position.Z):0.00}) ");
                }

                var currentMobs = _huntManager.GetAllCurrentMobsWithRank();
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

        private void ImGui_CentreText(string text, Vector4 colour, float offset = 1f)
        {
            var windowWidth = ImGui.GetWindowSize().X;
            var textWidth = ImGui.CalcTextSize(text).X;
            ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f * offset);
            ImGui.TextColored(colour, text);
            //ImGui.TextUnformatted(text);
        }

        private void ImGui_RightAlignText(string text)
        {
            var windowWidth = ImGui.GetWindowSize().X;
            var textWidth = ImGui.CalcTextSize(text).X;
            ImGui.SetCursorPosX((windowWidth - textWidth) * .95f);
            ImGui_TextColourSwapWhenUsingMapImage(text);
            //ImGui.TextUnformatted(text);
        }

        private void ImGui_TextColourSwapWhenUsingMapImage(string text)
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

        private void DoStuffWithMonoFont(Action function)
        {
            ImGui.PushFont(UiBuilder.MonoFont);
            function();
            ImGui.PopFont();
        }

        private void LoadMapImages()
        {
            if (!_useMapImages) return;
            _huntManager.LoadMapImages();
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

        private void Debug_OptionsWindowTable_ShowWindowSize()
        {
            ImGui.TextDisabled($"{ImGui.GetWindowSize()}");
            //ImGui.TextDisabled($"{_bottomPanelHeight}");

        }

        private void SetCursorPosByPercentage(float percentX, float percentY)
        {
            var winSize = ImGui.GetWindowSize();
            var textHeight = ImGui.GetTextLineHeight();
            //cant' easily get width and idc to try
            ImGui.SetCursorPos(new Vector2(winSize.X * percentX / 100, winSize.Y * percentY / 100 - textHeight));
        }
    }
}
