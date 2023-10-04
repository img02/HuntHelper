using Dalamud.Data;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Plugin;
using HuntHelper.Gui.Resource;
using HuntHelper.Managers.Hunts;
using HuntHelper.Managers.MapData;
using HuntHelper.Managers.MapData.Models;
using HuntHelper.Utilities;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Speech.Synthesis;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;

namespace HuntHelper.Gui
{
    class MapUI : IDisposable
    {
        private readonly Configuration _configuration;
        private readonly DalamudPluginInterface _pluginInterface;

        private readonly IClientState _clientState;
        private readonly IObjectTable _objectTable;
        private readonly IDataManager _dataManager;
        private readonly HuntManager _huntManager;
        private readonly MapDataManager _mapDataManager;
        private readonly IGameGui _gameGui;

        private string _territoryName;
        private string WorldName => _clientState.LocalPlayer?.CurrentWorld?.GameData?.Name.ToString() ?? "Not Found";
        private ushort _territoryId;

        //used for combo box for language selection window
        private string _guiLanguage = GuiResources.Language;

        private float _bottomPanelHeight = 30 * ImGuiHelpers.GlobalScale;

        //base icon sizes - based on coord size? - not customizable
        private float _mobIconRadius;
        private float _playerIconRadius;
        private float _spawnPointIconRadius;

        #region  User Customisable stuff - load and save to config

        //map opacity - should be between 0-100f;
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
        private int _ttsVoiceVolume;
        private string _ttsAMessage = "<rank> Nearby";
        private string _ttsBMessage = "<rank> Nearby";
        private string _ttsSMessage = "<rank> in zone";
        private string _chatAMessage = "FOUND: <name> @ <flag> ---  <rank>  --  <hpp>";
        private string _chatBMessage = "FOUND: <name> @ <flag> ---  <rank>  --  <hpp>";
        private string _chatSMessage = "FOUND: <name> @ <flag> ---  <rank>  --  <hpp>";
        private bool _ttsAEnabled = false;
        private bool _ttsBEnabled = false;
        private bool _ttsSEnabled = true;
        private bool _chatAEnabled = false;
        private bool _chatBEnabled = false;
        private bool _chatSEnabled = true;
        private bool _flyTxtAEnabled = true;
        private bool _flyTxtBEnabled = true;
        private bool _flyTxtSEnabled = true;
        private bool _pointToARank = true;
        private bool _pointToBRank = true;
        private bool _pointToSRank = true;

        private float _diamondModifier = 2f;


        private bool _enableBackgroundScan = false;

        //window bools
        private bool _showOptionsWindow = true;
        #endregion

        private bool _showDebug = false;
        private float _mapZoneMaxCoordSize = 41; //default to 41 as thats most common for hunt zones

        public float SingleCoordSize => ImGui.GetWindowSize().X / _mapZoneMaxCoordSize;
        //message input lengths
        private uint _inputTextMaxLength = 300;
        private readonly Vector4 _defaultTextColour = Vector4.One; //white

        //window bools
        private bool _randomDebugWindowVisible = false;
        private bool _showDatabaseListWindow = false;

        private Task _backgroundLoop;
        private CancellationTokenSource _backgroundLoopCancelTokenSource;
        private Task _loadingImagesAttempt = Task.CompletedTask;

        public bool RandomDebugWindowVisisble
        {
            get => _randomDebugWindowVisible;
            set { _randomDebugWindowVisible = value; }
        }

        private bool _mapVisible = false;
        public bool MapVisible
        {
            get => _mapVisible;
            set => _mapVisible = value;
        }

        private bool _settingsVisible = false;

        public bool SettingsVisible
        {
            get => _settingsVisible;
            set => _settingsVisible = value;
        }

        public MapUI(Configuration configuration, DalamudPluginInterface pluginInterface,
            IClientState clientState, IObjectTable objectTable, IDataManager dataManager,
            HuntManager huntManager, MapDataManager mapDataManager, IGameGui gameGui)
        {
            _configuration = configuration;
            _pluginInterface = pluginInterface; //not using atm...

            _clientState = clientState;
            _objectTable = objectTable;
            _dataManager = dataManager;
            _huntManager = huntManager;
            _mapDataManager = mapDataManager;
            _gameGui = gameGui;
            if (!huntManager.DontUseSynthesizer)
                _ttsVoiceName = huntManager.TTS.Voice.Name; // load default voice first, then from settings if avail.
            _territoryName = string.Empty;

            ClientState_TerritoryChanged(0);
            _clientState.TerritoryChanged += ClientState_TerritoryChanged;

            LoadSettings();
            LoadMapImages();

            _backgroundLoopCancelTokenSource = new CancellationTokenSource();
            _backgroundLoop = Task.Run(BackgroundLoop, _backgroundLoopCancelTokenSource.Token);


        }

        private void BackgroundLoop()
        {
            while (true)
            {
                if (_backgroundLoopCancelTokenSource.Token.IsCancellationRequested) return;
                while (_enableBackgroundScan && !MapVisible)
                {
                    if (_backgroundLoopCancelTokenSource.Token.IsCancellationRequested) return;
                    UpdateMobInfo();
                    Thread.Sleep(1000);
                }
                Thread.Sleep(1000);
            }
        }

        public void Dispose()
        {
            _clientState.TerritoryChanged -= ClientState_TerritoryChanged;
            _backgroundLoopCancelTokenSource.Cancel();
            while (!_backgroundLoop.IsCompleted) ;
            _backgroundLoopCancelTokenSource.Dispose();
            SaveSettings();
        }

        public void Draw()
        {
            DrawDebugWindow();
            DrawSettingsWindow();
            DrawHuntMapWindow();
        }

        public void DrawDebugWindow()
        {
            if (!RandomDebugWindowVisisble)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(375, 330), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(375, 330), new Vector2(float.MaxValue, float.MaxValue));
            if (ImGui.Begin("Debug stuff", ref _randomDebugWindowVisible))
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
                           $"Y: {ConvertPosToCoordinate(v3.Z)} |\n");
                //END


                var nearby = _huntManager.GetCurrentMobs();
                ImGui.Text("Nearby Mobs");
                nearby.ForEach(m => ImGui.TextUnformatted($"{m.Name} - {m.NameId}"));
                ImGui.Text("============");

                var (rank, mob) = _huntManager.GetPriorityMob();
                ImGui.Text("priority:");
                ImGui.TextUnformatted($"{rank} - {mob?.Name}");

                ImGui.Text("============");

                ShowDebugInfo();

                ImGui.Indent(55);
                ImGui.Text("");

                var hunt = "";
                foreach (var obj in _objectTable)
                {
                    if (obj is not BattleNpc bobj) continue;
                    if (bobj.MaxHp < 10000) continue; //not really needed if subkind is enemy, once matching to id / name
                    if (bobj.BattleNpcKind != BattleNpcSubKind.Enemy) continue; //not really needed if matching to 'nameID'


                    hunt += $"{obj.Name} \n" +
                            $"KIND: {bobj.BattleNpcKind}\n" +
                            $"NAMEID: {bobj.NameId}\n" +
                            $"|HP: {bobj.CurrentHp}\n" +
                            $"|HP%%: {bobj.CurrentHp * 1.0 / bobj.MaxHp * 100}%%\n" +
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
            ImGui.SetNextWindowPos(_mapWindowPos, ImGuiCond.Appearing);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(_mapWindowColour.X, _mapWindowColour.Y, _mapWindowColour.Z, _mapWindowOpacityAsPercentage / 100f));
            if (ImGui.Begin("Hunt Helper", ref _mapVisible, (ImGuiWindowFlags)_huntWindowFlag))
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
                    //if only something went wrong, such as only some maps images downloaded
                    if (_huntManager.HasDownloadErrors) MapImageDownloadWindow();

                    if (!_huntManager.ImagesLoaded && !_huntManager.HasDownloadErrors)
                    {
                        //if images/map doesn't exist, or is empty - show map download window
                        if (!Directory.Exists(_huntManager.ImageFolderPath) || !Directory.EnumerateFiles(_huntManager.ImageFolderPath).Any()) MapImageDownloadWindow();
                        else LoadMapImages();
                    }
                    else
                    {
                        var mapNameEng = MapHelpers.GetMapNameInEnglish(_dataManager, _territoryId);
                        var mapImg = _huntManager.GetMapImage(mapNameEng);
                        if (mapImg != null)
                        {
                            ImGui.SetCursorPos(Vector2.Zero);
                            ImGui.Image(mapImg.ImGuiHandle, ImGui.GetWindowSize(), default,
                                new Vector2(1f, 1f), new Vector4(1f, 1f, 1f, _mapImageOpacityAsPercentage / 100));
                            ImGui.SetCursorPos(Vector2.Zero);
                        }
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
                //draw the button up a lil higher if dalamud scale set high, works fine up to 36pt, but sorta covers image credits :(
                var cursorPos = new Vector2(8, ImGui.GetWindowSize().Y - 50 + (-3 * ImGuiHelpers.GlobalScale)); //positioned so it doesn't block map image credits!
                ImGui.SetCursorPos(cursorPos);

                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(.4f, .4f, .4f, 1f));

                //default dalamud font scale = 12pt = 1
                //ImGui.SetWindowFontScale(12/ImGuiHelpers.GlobalScale/12);
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Cog)) _showOptionsWindow = !_showOptionsWindow;
                //ImGui.SetWindowFontScale(1);

                ImGui.PopStyleColor();
                if (_huntManager.ErrorPopUpVisible || _mapDataManager.ErrorPopUpVisible)
                {
                    ImGui.Begin(GuiResources.MapGuiText["ErrorLoadingDataMessage"]);
                    ImGui.Text(_huntManager.ErrorMessage);
                    ImGui.Text(_mapDataManager.ErrorMessage);
                    ImGui.End();
                }

                //optional togglable stuff 

                //zone name
                if (_showZoneName)
                {
                    ImGuiUtil.DoStuffWithMonoFont(() =>
                    {
                        SetCursorPosByPercentage(_zoneInfoPosXPercentage, _zoneInfoPosYPercentage);
                        if (!_useMapImages) ImGui.TextColored(_zoneTextColour, $"{_territoryName}");
                        if (_useMapImages) ImGui.TextColored(_zoneTextColourAlt, $"{_territoryName}");
                    });

                }
                if (_showWorldName)
                {
                    ImGuiUtil.DoStuffWithMonoFont(() =>
                    {
                        SetCursorPosByPercentage(_worldInfoPosXPercentage, _worldInfoPosYPercentage);
                        if (!_useMapImages) ImGui.TextColored(_worldTextColour, $"{WorldName}");
                        if (_useMapImages) ImGui.TextColored(_worldTextColourAlt, $"{WorldName}");
                    });
                }
            }
            ImGui.PopStyleColor();
            ImGui.End();
            ImGui.PopStyleVar(2);
        }
        private void DrawOptionsWindow()
        {
            var bottomDockingPos = Vector2.Add(ImGui.GetWindowPos(), new Vector2(0, ImGui.GetWindowSize().Y));

            ImGui.SetNextWindowSize(new Vector2(ImGui.GetWindowSize().X, _bottomPanelHeight), ImGuiCond.Always);
            ImGui.SetNextWindowSizeConstraints(new Vector2(-1, 95), new Vector2(-1, -1));
            ImGui.SetNextWindowPos(bottomDockingPos);

            //this gets a little glitchy when it clips out the bottom of the screen, but I already committed and refuse to back down
            //hide grip color
            ImGui.PushStyleColor(ImGuiCol.ResizeGrip, 0);
            if (ImGui.Begin("Options Window", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.HorizontalScrollbar))
            {
                ImGui.SetWindowPos(bottomDockingPos);
                ImGui.Dummy(new Vector2(0, 2f));
                ImGui.Columns(2);
                ImGui.SetColumnWidth(0, ImGui.GetWindowSize().X / 5);
                ImGui.SetColumnWidth(1, ImGui.GetWindowSize().X);

                if (ImGui.BeginChild("##Options left side", ImGui.GetContentRegionAvail(), false, ImGuiWindowFlags.HorizontalScrollbar))
                {
                    ImGui.Checkbox(GuiResources.MapGuiText["MapImageCheckbox"], ref _useMapImages);
                    ImGui.SameLine();
                    ImGuiUtil.ImGui_HelpMarker(GuiResources.MapGuiText["MapImageCheckboxToolTip"]);

                    ImGui.CheckboxFlags(GuiResources.MapGuiText["TitleBarCheckbox"], ref _huntWindowFlag, 1);

                    if (ImGui.Checkbox(GuiResources.MapGuiText["BackgroundScanCheckbox"], ref _enableBackgroundScan))
                    {
                        _configuration.EnableBackgroundScan = _enableBackgroundScan;
                    }

                    ImGuiUtil.ImGui_HoveredToolTip(GuiResources.MapGuiText["BackgroundScanCheckboxToolTip"]);

                    //ImGui.Dummy(new Vector2(0, 4f));

                    if (ImGui.Button(GuiResources.MapGuiText["LoadedHuntDataButton"])) _showDatabaseListWindow = !_showDatabaseListWindow;
                    ImGui.SameLine();
                    ImGuiUtil.ImGui_HelpMarker(GuiResources.MapGuiText["LoadedHuntDataButtonToolTip"]);
                    DrawDataBaseWindow();

                    /*ImGui.TextUnformatted($"{ImGui.GetWindowSize()}");
                    ImGui.TextUnformatted($"{ImGui.GetContentRegionAvail()}");*/

                    ImGui.EndChild();
                }

                ImGui.NextColumn();
                if (ImGui.BeginTabBar("Options", ImGuiTabBarFlags.FittingPolicyScroll))
                {
                    var tabBarHeight = 57f;
                    if (ImGui.BeginTabItem(GuiResources.MapGuiText["GeneralTab"]))
                    {
                        _bottomPanelHeight = tabBarHeight + 155f * ImGuiHelpers.GlobalScale; //tab bar + spacing seems to be around ~57f. 155f is the sizeY of the largest content section
                        var widgetWidth = 69f;

                        var tableSizeX = ImGui.GetContentRegionAvail().X;
                        var tableSizeY = ImGui.GetContentRegionAvail().Y;

                        ImGui.Dummy(new Vector2(0, 8f));

                        ImGui.SetNextWindowContentSize(new Vector2(260f * ImGuiHelpers.GlobalScale, 140f * ImGuiHelpers.GlobalScale));

                        if (ImGui.BeginChild("general left side", new Vector2((1.2f * tableSizeX / 3), 0f), true, ImGuiWindowFlags.HorizontalScrollbar))
                        {
                            if (ImGui.BeginTable("General Options table", 2))
                            {
                                ImGui.TableSetupColumn("first", ImGuiTableColumnFlags.WidthFixed);
                                ImGui.TableSetupColumn("second", ImGuiTableColumnFlags.WidthFixed);
                                ImGui.TableNextColumn();
                                ImGui.Checkbox(GuiResources.MapGuiText["ZoneNameCheckbox"], ref _showZoneName);
                                ImGui.SameLine();
                                ImGuiUtil.ImGui_HelpMarker(GuiResources.MapGuiText["ZoneNameCheckboxToolTip"]);
                                ImGui.PushItemWidth(widgetWidth);
                                ImGui.DragFloat($"{GuiResources.MapGuiText["ZoneLabel"]} X", ref _zoneInfoPosXPercentage, 0.05f, 0, 100, "%.2f",
                                    ImGuiSliderFlags.NoRoundToFormat);
                                ImGui.SameLine();
                                ImGuiUtil.ImGui_HelpMarker(GuiResources.MapGuiText["DragWidgetToolTip"]);
                                ImGui.DragFloat($"{GuiResources.MapGuiText["ZoneLabel"]} Y", ref _zoneInfoPosYPercentage, 0.05f, 0, 100, "%.2f",
                                    ImGuiSliderFlags.NoRoundToFormat);

                                ImGui.ColorEdit4($"{GuiResources.MapGuiText["ColourLabel"]}##Zone", ref _zoneTextColour,
                                    ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.PickerHueWheel);
                                ImGui.ColorEdit4($"{GuiResources.MapGuiText["AlternateColourLabel"]}##Zone", ref _zoneTextColourAlt,
                                    ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.PickerHueWheel);

                                ImGui.SameLine();
                                ImGuiUtil.ImGui_HelpMarker(GuiResources.MapGuiText["AlternateColourToolTip"]);

                                ImGui.TableNextColumn();
                                ImGui.Checkbox(GuiResources.MapGuiText["WorldNameCheckbox"], ref _showWorldName);
                                ImGui.SameLine();
                                ImGuiUtil.ImGui_HelpMarker(GuiResources.MapGuiText["WorldNameCheckboxToolTip"]);
                                ImGui.PushItemWidth(widgetWidth);
                                ImGui.DragFloat($"{GuiResources.MapGuiText["WorldLabel"]} X", ref _worldInfoPosXPercentage, 0.05f, 0, 100f, "%.2f",
                                    ImGuiSliderFlags.NoRoundToFormat);
                                ImGui.DragFloat($"{GuiResources.MapGuiText["WorldLabel"]} Y", ref _worldInfoPosYPercentage, 0.05f, 0, 100f, "%.2f",
                                    ImGuiSliderFlags.NoRoundToFormat);

                                ImGui.ColorEdit4($"{GuiResources.MapGuiText["ColourLabel"]}##World", ref _worldTextColour,
                                    ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.PickerHueWheel);
                                ImGui.ColorEdit4($"{GuiResources.MapGuiText["AlternateColourLabel"]}##World", ref _worldTextColourAlt,
                                    ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.PickerHueWheel);
                                ImGui.SameLine();
                                ImGuiUtil.ImGui_HelpMarker(GuiResources.MapGuiText["AlternateColourToolTip"]);

                                ImGui.EndTable();
                            }

                            //ImGui.SameLine(); ImGui.Text($"{ImGui.GetWindowSize()}");
                            ImGui.EndChild();
                        }


                        ImGui.SameLine();
                        ImGui.SetNextWindowContentSize(new Vector2(270f * ImGuiHelpers.GlobalScale, 155f * ImGuiHelpers.GlobalScale));

                        if (ImGui.BeginChild("general right side resize section", new Vector2((1.8f * tableSizeX / 3 - 8f), tableSizeY - 24f), true, ImGuiWindowFlags.HorizontalScrollbar))
                        {
                            if (ImGui.BeginTable("right side table", 4))
                            {
                                ImGui.TableSetupColumn("test",
                                    ImGuiTableColumnFlags.WidthFixed);

                                var intWidgetWidth = 36f * ImGuiHelpers.GlobalScale;

                                // Current Window Size
                                ImGui.TableNextColumn();
                                ImGui.Dummy(new Vector2(0f, 0f));
                                ImGui.Dummy(new Vector2(0f, 0f));
                                ImGui.SameLine();
                                ImGui.Text(GuiResources.MapGuiText["WindowSize"]);
                                ImGui.SameLine();
                                ImGuiUtil.ImGui_HelpMarker(GuiResources.MapGuiText["WindowSizeToolTip"]);

                                ImGui.TableNextColumn();
                                ImGui.TableSetupColumn("test", ImGuiTableColumnFlags.WidthFixed, 5f);
                                ImGui.Dummy(new Vector2(0f, 0f));
                                ImGui.PushItemWidth(intWidgetWidth);
                                ImGui.InputInt("", ref _currentWindowSize, 0);
                                ImGui.PopID(); //popid so it can be used by another element

                                ImGui.TableNextColumn();
                                ImGui.Dummy(new Vector2(0f, 0f));
                                ImGui.ColorEdit4("##Window Colour", ref _mapWindowColour,
                                    ImGuiColorEditFlags.PickerHueWheel | ImGuiColorEditFlags.NoInputs |
                                    ImGuiColorEditFlags.NoAlpha);
                                ImGui.SameLine();
                                ImGuiUtil.ImGui_HelpMarker(GuiResources.MapGuiText["WindowColourToolTip"]);

                                ImGui.TableNextColumn();

                                // Window Size Preset 1
                                ImGui.TableNextColumn();
                                ImGui.Dummy(new Vector2(0f, 0f));
                                ImGui.Dummy(new Vector2(0f, 0f));
                                ImGui.SameLine();
                                ImGui.Text(GuiResources.MapGuiText["PresetOne"]);
                                ImGui.SameLine();
                                ImGuiUtil.ImGui_HelpMarker(GuiResources.MapGuiText["SavePresetToolTip"]);

                                ImGui.TableNextColumn();
                                ImGui.Dummy(new Vector2(0f, 0f));
                                ImGui.PushItemWidth(intWidgetWidth);
                                ImGui.InputInt("", ref _presetOneWindowSize, 0);
                                ImGui.PopID();

                                ImGui.TableNextColumn();
                                ImGui.Dummy(new Vector2(0f, 0f));
                                if (ImGui.Button(GuiResources.MapGuiText["SaveButton"])) SavePreset(1);
                                ImGui.PopID();
                                ImGui.TableNextColumn();

                                // Window Size Preset 2
                                ImGui.TableNextColumn();
                                ImGui.Dummy(new Vector2(0f, 0f));
                                ImGui.Dummy(new Vector2(0f, 0f));
                                ImGui.SameLine();
                                ImGui.Text(GuiResources.MapGuiText["PresetTwo"]);
                                ImGui.SameLine();
                                ImGuiUtil.ImGui_HelpMarker(GuiResources.MapGuiText["SavePresetToolTip"]);

                                ImGui.TableNextColumn();
                                ImGui.Dummy(new Vector2(0f, 0f));
                                ImGui.PushItemWidth(intWidgetWidth);
                                ImGui.InputInt("", ref _presetTwoWindowSize, 0);
                                ImGui.PopID();

                                ImGui.TableNextColumn();
                                ImGui.Dummy(new Vector2(0f, 0f));
                                if (ImGui.Button(GuiResources.MapGuiText["SaveButton"])) SavePreset(2);
                                ImGui.PopID();
                                ImGui.EndTable();

                                //Map Image Opacity Slider
                                ImGui.Separator();
                                //ImGui_CentreText("Map Opacity", Vector4.One);
                                ImGui.Dummy(new Vector2(0, 0));
                                ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - 16f);
                                ImGui.SameLine();
                                ImGui.DragFloat("##Map Opacity", ref _mapImageOpacityAsPercentage, .2f, 0f, 100f,
                                    $"{GuiResources.MapGuiText["MapOpacityLabel"]} - %.0f");

                                //Map Window Opacity Slider
                                //ImGui.Separator();
                                //ImGui_CentreText("Window Opacity", Vector4.One);
                                ImGui.Dummy(new Vector2(0, 0));
                                ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - 16f);
                                ImGui.SameLine();
                                ImGui.DragFloat("##Map Window Opacity", ref _mapWindowOpacityAsPercentage, .2f, 0f,
                                    100f,
                                    $"{GuiResources.MapGuiText["WindowOpacity"]} - %.0f");
                            }

                            //ImGui.SameLine(); ImGui.Text($"{ImGui.GetWindowSize()}");
                            ImGui.EndChild();
                        }


                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem(GuiResources.MapGuiText["VisualsTab"]))
                    {
                        _bottomPanelHeight = tabBarHeight + 108f * ImGuiHelpers.GlobalScale;

                        if (ImGui.BeginTabBar("Visuals sub-bar"))
                        {
                            if (ImGui.BeginTabItem(GuiResources.MapGuiText["SizingTab"]))
                            {
                                ImGui.Dummy(new Vector2(0, 2f));
                                if (ImGui.BeginTable("Sizing Options Table", 3, ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY))
                                {
                                    var widgetWidth = 40f * ImGuiHelpers.GlobalScale;

                                    ImGui.TableSetupColumn("1#visualsizing", ImGuiTableColumnFlags.WidthFixed);
                                    ImGui.TableSetupColumn("2#visualsizing", ImGuiTableColumnFlags.WidthFixed);
                                    ImGui.TableSetupColumn("3#visualsizing", ImGuiTableColumnFlags.WidthFixed);

                                    ImGui.TableNextColumn();
                                    ImGui.PushItemWidth(widgetWidth);
                                    ImGui.InputFloat(GuiResources.MapGuiText["PlayerModifier"], ref _playerIconRadiusModifier, 0, 0, "%.2f");
                                    ImGui.SameLine();
                                    ImGuiUtil.ImGui_HelpMarker(GuiResources.MapGuiText["PlayerModifierToolTip"]);

                                    ImGui.TableNextColumn();
                                    ImGui.PushItemWidth(widgetWidth);
                                    ImGui.InputFloat(GuiResources.MapGuiText["MobModifier"], ref _mobIconRadiusModifier, 0, 0, "%.2f");
                                    ImGui.SameLine();
                                    ImGuiUtil.ImGui_HelpMarker(GuiResources.MapGuiText["MobModifierToolTip"]);

                                    ImGui.TableNextColumn();
                                    ImGui.PushItemWidth(widgetWidth);
                                    ImGui.InputFloat(GuiResources.MapGuiText["SpawnPointModifier"], ref _spawnPointRadiusModifier, 0, 0,
                                        "%.2f");
                                    ImGui.SameLine();
                                    ImGuiUtil.ImGui_HelpMarker(GuiResources.MapGuiText["SpawnPointToolTip"]);

                                    ImGui.TableNextColumn();
                                    ImGui.InputFloat(GuiResources.MapGuiText["AllModifier"], ref _allRadiusModifier, 0, 0, "%.2f");
                                    ImGui.SameLine();
                                    ImGuiUtil.ImGui_HelpMarker(GuiResources.MapGuiText["AllModifierToolTip"]);

                                    ImGui.TableNextColumn();
                                    ImGui.InputFloat(GuiResources.MapGuiText["DetectionCircleModifier"], ref _detectionCircleModifier, 0, 0,
                                        "%.2f");
                                    ImGui.SameLine();
                                    ImGuiUtil.ImGui_HelpMarker(GuiResources.MapGuiText["DetectionCircleModifierToolTip"]);

                                    ImGui.TableNextColumn();
                                    ImGui.Dummy(new Vector2(0, 26f));

                                    ImGui.TableNextColumn();
                                    ImGui.Separator();
                                    ImGui.Dummy(new Vector2(0, 1f));
                                    ImGui.InputFloat(GuiResources.MapGuiText["DetectionCircleThickness"], ref _detectionCircleThickness, 0, 0,
                                        "%.2f");
                                    ImGui.SameLine();
                                    ImGuiUtil.ImGui_HelpMarker(GuiResources.MapGuiText["DetectionCircleThicknessToolTip"]);

                                    ImGui.TableNextColumn();
                                    ImGui.Separator();
                                    ImGui.Dummy(new Vector2(0, 1f));
                                    ImGui.InputFloat(GuiResources.MapGuiText["DirectionLineThickness"], ref _directionLineThickness, 0, 0,
                                        "%.2f");
                                    ImGui.SameLine();
                                    ImGuiUtil.ImGui_HelpMarker(GuiResources.MapGuiText["DirectionLineThicknessToolTip"]);

                                    ImGui.TableNextColumn();
                                    ImGui.Separator();
                                    ImGui.Dummy(new Vector2(0, 1f));
                                    if (ImGui.Button(GuiResources.MapGuiText["ResetButton"]))
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
                                    ImGuiUtil.ImGui_HelpMarker(GuiResources.MapGuiText["ResetButtonToolTip"]);

                                    ImGui.PopItemWidth();

                                    ImGui.EndTable();
                                }

                                ImGui.EndTabItem();
                            }

                            if (ImGui.BeginTabItem(GuiResources.MapGuiText["ColoursTab"]))
                            {
                                ImGui.Dummy(new Vector2(0, 2f));

                                if (ImGui.BeginTable("Colour Options Table", 3, ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY))
                                {
                                    ImGui.Dummy(new Vector2(0, 2f));

                                    ImGui.TableSetupColumn("1#coloursizing", ImGuiTableColumnFlags.WidthFixed);
                                    ImGui.TableSetupColumn("2#coloursizing", ImGuiTableColumnFlags.WidthFixed);
                                    ImGui.TableSetupColumn("3#coloursizing", ImGuiTableColumnFlags.WidthFixed);

                                    ImGui.TableNextColumn();
                                    ImGui.ColorEdit4(GuiResources.MapGuiText["PlayerIcon"], ref _playerIconColour,
                                        ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.PickerHueWheel);

                                    ImGui.TableNextColumn();
                                    ImGui.ColorEdit4(GuiResources.MapGuiText["MobIcon"], ref _mobColour,
                                        ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.PickerHueWheel);

                                    ImGui.TableNextColumn();
                                    ImGui.ColorEdit4(GuiResources.MapGuiText["SpawnPoint"], ref _spawnPointColour,
                                        ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.PickerHueWheel);

                                    ImGui.Dummy(new Vector2(0, 4f));

                                    ImGui.TableNextColumn();
                                    ImGui.ColorEdit4(GuiResources.MapGuiText["PlayerBackground"], ref _playerIconBackgroundColour,
                                        ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.PickerHueWheel);
                                    ImGui.SameLine();
                                    ImGuiUtil.ImGui_HelpMarker(GuiResources.MapGuiText["PlayerBackgroundToolTip"]);


                                    ImGui.Dummy(new Vector2(0, 4f));

                                    ImGui.TableNextColumn();
                                    ImGui.ColorEdit4(GuiResources.MapGuiText["DirectionLineColourPicker"], ref _directionLineColour,
                                        ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.PickerHueWheel);

                                    ImGui.TableNextColumn();
                                    ImGui.ColorEdit4(GuiResources.MapGuiText["DetectionCircleColourPicker"], ref _detectionCircleColour,
                                        ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.PickerHueWheel);

                                    ImGui.EndTable();
                                }

                                ImGui.EndTabItem();
                            }

                            ImGui.EndTabBar();
                        }

                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem(GuiResources.MapGuiText["NotificationTab"]))
                    {
                        if (ImGui.BeginTabBar("Notifications sub-bar"))
                        {
                            //ImGui.PushFont(UiBuilder.MonoFont); //aligns things, but then looks ugly so idk.. table?
                            if (ImGui.BeginTabItem(" A "))
                            {
                                _bottomPanelHeight = tabBarHeight + 98f * ImGuiHelpers.GlobalScale;
                                if (ImGui.BeginChild("##A child", ImGui.GetContentRegionAvail(), false, ImGuiWindowFlags.HorizontalScrollbar))
                                {
                                    ImGui.Dummy(new Vector2(0, 2f));
                                    ImGui.TextUnformatted(GuiResources.MapGuiText["ChatMessageLabel"]);
                                    ImGui.SameLine();
                                    ImGui.InputText("##A Rank A Chat Msg", ref _chatAMessage, _inputTextMaxLength);
                                    ImGui.SameLine();
                                    ImGui.Checkbox("##A Rank A Chat Checkbox", ref _chatAEnabled);
                                    ImGui.SameLine();
                                    ImGuiUtil.ImGui_HelpMarker(GuiResources.MapGuiText["ChatMessageLabelToolTip"]);

                                    ImGui.Dummy(new Vector2(0, 2f));
                                    ImGui.TextUnformatted(GuiResources.MapGuiText["TTSMessageLabel"]);
                                    ImGui.SameLine();
                                    ImGui.InputText("##A Rank A TTS Msg", ref _ttsAMessage, _inputTextMaxLength);
                                    ImGui.SameLine();
                                    ImGui.Checkbox("##A Rank A TTS Checkbox", ref _ttsAEnabled);
                                    ImGui.SameLine();
                                    ImGuiUtil.ImGui_HelpMarker(GuiResources.MapGuiText["TTSMessageLabelToolTip"]);

                                    ImGui.Checkbox("FlyText##A Rank", ref _flyTxtAEnabled);
                                    ImGui.SameLine();
                                    ImGuiUtil.ImGui_HelpMarker(GuiResources.MapGuiText["FlyTextToolTip"]);
                                    ImGui.SameLine();
                                    if (ImGui.Checkbox($"{GuiResources.MapGuiText["VisualHelpLabel"]}##A Rank", ref _pointToARank))
                                    {
                                        _configuration.PointToARank = _pointToARank;
                                    }

                                    ImGuiUtil.ImGui_HoveredToolTip(GuiResources.MapGuiText["VisualHelpToolTip"]);

                                    ImGui.EndChild();
                                }

                                ImGui.EndTabItem();
                            }

                            if (ImGui.BeginTabItem(" B "))
                            {
                                _bottomPanelHeight = tabBarHeight + 98f * ImGuiHelpers.GlobalScale;

                                if (ImGui.BeginChild("##B child", ImGui.GetContentRegionAvail(), false,
                                        ImGuiWindowFlags.HorizontalScrollbar))
                                {
                                    ImGui.Dummy(new Vector2(0, 2f));
                                    ImGui.TextUnformatted(GuiResources.MapGuiText["ChatMessageLabel"]);
                                    ImGui.SameLine();
                                    ImGui.InputText("##A Rank B Chat Msg", ref _chatBMessage, _inputTextMaxLength);
                                    ImGui.SameLine();
                                    ImGui.Checkbox("##A Rank B Chat Checkbox", ref _chatBEnabled);
                                    ImGui.SameLine();
                                    ImGuiUtil.ImGui_HelpMarker(GuiResources.MapGuiText["ChatMessageLabelToolTip"]);

                                    ImGui.Dummy(new Vector2(0, 2f));
                                    ImGui.TextUnformatted(GuiResources.MapGuiText["TTSMessageLabel"]);
                                    ImGui.SameLine();
                                    ImGui.InputText("##A Rank B TTS Msg", ref _ttsBMessage, _inputTextMaxLength);
                                    ImGui.SameLine();
                                    ImGui.Checkbox("##A Rank B TTS Checkbox", ref _ttsBEnabled);
                                    ImGui.SameLine();
                                    ImGuiUtil.ImGui_HelpMarker(GuiResources.MapGuiText["TTSMessageLabelToolTip"]);

                                    ImGui.Checkbox("FlyText##B Rank", ref _flyTxtBEnabled);
                                    ImGui.SameLine();
                                    ImGuiUtil.ImGui_HelpMarker(GuiResources.MapGuiText["FlyTextToolTip"]);
                                    ImGui.SameLine();
                                    if (ImGui.Checkbox($"{GuiResources.MapGuiText["VisualHelpLabel"]}##B Rank", ref _pointToBRank))
                                    {
                                        _configuration.PointToBRank = _pointToBRank;
                                    }

                                    ImGuiUtil.ImGui_HoveredToolTip(GuiResources.MapGuiText["VisualHelpToolTip"]);

                                    ImGui.EndChild();
                                }

                                ImGui.EndTabItem();
                            }

                            if (ImGui.BeginTabItem(" S "))
                            {
                                _bottomPanelHeight = tabBarHeight + 98f * ImGuiHelpers.GlobalScale;

                                if (ImGui.BeginChild("##S child", ImGui.GetContentRegionAvail(), false,
                                        ImGuiWindowFlags.HorizontalScrollbar))
                                {
                                    ImGui.Dummy(new Vector2(0, 2f));
                                    ImGui.TextUnformatted(GuiResources.MapGuiText["ChatMessageLabel"]);
                                    ImGui.SameLine();
                                    ImGui.InputText("##A Rank S Chat Msg", ref _chatSMessage, _inputTextMaxLength);
                                    ImGui.SameLine();
                                    ImGui.Checkbox("##A Rank S Chat Checkbox", ref _chatSEnabled);
                                    ImGui.SameLine();
                                    ImGuiUtil.ImGui_HelpMarker(GuiResources.MapGuiText["ChatMessageLabelToolTip"]);

                                    ImGui.Dummy(new Vector2(0, 2f));
                                    ImGui.TextUnformatted(GuiResources.MapGuiText["TTSMessageLabel"]);
                                    ImGui.SameLine();
                                    ImGui.InputText("##A Rank S TTS Msg", ref _ttsSMessage, _inputTextMaxLength);
                                    ImGui.SameLine();
                                    ImGui.Checkbox("##A Rank S TTS Checkbox", ref _ttsSEnabled);
                                    ImGui.SameLine();
                                    ImGuiUtil.ImGui_HelpMarker(GuiResources.MapGuiText["TTSMessageLabelToolTip"]);

                                    ImGui.Checkbox("FlyText##S Rank", ref _flyTxtSEnabled);
                                    ImGui.SameLine();
                                    ImGuiUtil.ImGui_HelpMarker(GuiResources.MapGuiText["FlyTextToolTip"]);
                                    ImGui.SameLine();
                                    if (ImGui.Checkbox($"{GuiResources.MapGuiText["VisualHelpLabel"]}##S Rank", ref _pointToSRank))
                                    {
                                        _configuration.PointToSRank = _pointToSRank;
                                    }

                                    ImGuiUtil.ImGui_HoveredToolTip(GuiResources.MapGuiText["VisualHelpToolTip"]);

                                    /*ImGui.SameLine();
                                    ImGui.Checkbox("Save Spawn Data", ref _saveSpawnData);
                                    ImGui.SameLine();
                                    ImGuiUtil.ImGui_HelpMarker("Saves S Rank Information to desktop txt (ToDo - let's be honest, i'm never going to remember to do this)");*/

                                    ImGui.EndChild();
                                }

                                ImGui.EndTabItem();
                            }

                            if (ImGui.BeginTabItem(GuiResources.MapGuiText["SettingsTab"]))
                            {
                                _bottomPanelHeight = tabBarHeight + 98f * ImGuiHelpers.GlobalScale;
                                
                                ImGui.Dummy(new Vector2(0f, 2f));
                                if (ImGui.BeginTable("##settings table", 2, ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY))
                                {
                                    ImGui.TableSetupColumn("#settingsLabel", ImGuiTableColumnFlags.WidthFixed);
                                    ImGui.TableSetupColumn("#settingsValue", ImGuiTableColumnFlags.WidthStretch);

                                    if (!_huntManager.DontUseSynthesizer)
                                    {
                                        var tts = _huntManager.TTS;
                                        var voiceList = tts.GetInstalledVoices();
                                        var listOfVoiceNames = new string[voiceList.Count];
                                        for (int i = 0; i < voiceList.Count; i++)
                                        {
                                            listOfVoiceNames[i] = voiceList[i].VoiceInfo.Name;
                                        }

                                        var itemPos = Array.IndexOf(listOfVoiceNames, _ttsVoiceName);
                                        
                                        ImGui.TableNextColumn();
                                        ImGui.Text(GuiResources.MapGuiText["SelectVoiceLabel"]);

                                        ImGui.TableNextColumn();
                                        if (ImGui.Combo("##TTS Voice Combo", ref itemPos, listOfVoiceNames,
                                                listOfVoiceNames.Length))
                                        {
                                            tts.SelectVoice(listOfVoiceNames[itemPos]);
                                            _ttsVoiceName = listOfVoiceNames[itemPos];
                                            _huntManager.TTSName = _ttsVoiceName;
                                        }

                                        ImGui.TableNextColumn();
                                        ImGui.Text(GuiResources.MapGuiText["VoiceVolumeLabel"]);

                                        ImGui.TableNextColumn();
                                        ImGui.PushItemWidth(100f * ImGuiHelpers.GlobalScale);
                                        if (ImGui.DragInt("##TTS Voice Volume", ref _ttsVoiceVolume, 1, 1,
                                                100))
                                        {
                                            tts.Volume = _ttsVoiceVolume;
                                            _huntManager.TTSVolume = _ttsVoiceVolume;
                                        }
                                        
                                        ImGui.SameLine();

                                        if (ImGui.Button(GuiResources.MapGuiText["VoiceTestButton"]))
                                        {
                                            //creating new speechsynthesizer because it does not play audio asynchronously
                                            var tempTTS = new SpeechSynthesizer();
                                            tempTTS.SelectVoice(_huntManager.TTSName);
                                            tempTTS.Volume = _huntManager.TTSVolume;
                                            var prompt = tempTTS.SpeakAsync($"{tts.Voice.Name}");
                                            Task.Run(() =>
                                            {
                                                while (!prompt.IsCompleted) ;
                                                tempTTS.Dispose();
                                            });
                                        }
                                    }

                                    ImGui.TableNextColumn();
                                    ImGui.Text(GuiResources.MapGuiText["PointerSizeLabel"]);

                                    ImGui.TableNextColumn();
                                    if (ImGui.DragFloat("##Diamond Pointer Size Modifier", ref _diamondModifier, 0.01f, 1, 10, "%.2f"))
                                    {
                                        _configuration.PointerDiamondSizeModifier = _diamondModifier;
                                    }

                                    ImGuiUtil.ImGui_HoveredToolTip(GuiResources.MapGuiText["PointerSizeToolTip"]);
                                    ImGui.EndTable();
                                }

                                ImGui.EndTabItem();
                            }

                            if (ImGui.BeginTabItem(GuiResources.MapGuiText["AvailableTagsTab"]))
                            {
                                _bottomPanelHeight = tabBarHeight + 113 * ImGuiHelpers.GlobalScale;

                                if (ImGui.BeginChild("##tags child", ImGui.GetContentRegionAvail(), false,
                                        ImGuiWindowFlags.HorizontalScrollbar))
                                {
                                    ImGui.TextUnformatted($"{GuiResources.MapGuiText["AvailableTagsTab"]}:");
                                    ImGui.SameLine();
                                    ImGuiUtil.ImGui_HelpMarker(GuiResources.MapGuiText["AvailableTagsToolTip"]);
                                    var tags =
                                        "Hunt: <flag> <name> <rank> <hpp>\n\n" +
                                        "Cosmetic: <goldstar> <silverstar> <warning> <nocircle> <controllerbutton0> <controllerbutton1>\n" +
                                        " <priorityworld> <elementallevel> <exclamationrectangle> <notoriousmonster> <alarm> <fanfestival>";

                                    ImGui.InputTextMultiline("##cosmetic tag info", ref tags, 500,
                                        new Vector2(600f * ImGuiHelpers.GlobalScale, 80 * ImGuiHelpers.GlobalScale), ImGuiInputTextFlags.ReadOnly);
                                    ImGui.EndChild();
                                }

                                ImGui.EndTabItem();
                            }
                            ImGui.EndTabBar();
                        }

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem(GuiResources.MapGuiText["OnScreenMobInfoTab"]))
                    {
                        _bottomPanelHeight = tabBarHeight + 90f * ImGuiHelpers.GlobalScale;

                        ImGui.SetNextWindowContentSize(new Vector2(470 * ImGuiHelpers.GlobalScale, 0));

                        if (ImGui.BeginChild("##mobinfo child", ImGui.GetContentRegionAvail(), true, ImGuiWindowFlags.HorizontalScrollbar))
                        {
                            if (ImGui.BeginTable("Mob info Table", 2))
                            {

                                ImGui.TableNextColumn();
                                ImGui.Checkbox(GuiResources.MapGuiText["PriorityMobLabel"], ref _priorityMobEnabled);
                                ImGui.SameLine();
                                ImGuiUtil.ImGui_HelpMarker(GuiResources.MapGuiText["PriorityMobToolTip"]);
                                ImGui.DragFloat("X##Priority Mob", ref _priorityMobInfoPosXPercentage, 0.05f, 0, 100,
                                    "%.2f", ImGuiSliderFlags.NoRoundToFormat);
                                ImGui.DragFloat("Y##Priority Mob", ref _priorityMobInfoPosYPercentage, 0.05f, 0, 100,
                                    "%.2f", ImGuiSliderFlags.NoRoundToFormat);
                                ImGui.ColorEdit4($"{GuiResources.MapGuiText["MainTextColourLabel"]}##PriorityMain", ref _priorityMobTextColour,
                                    ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.PickerHueWheel);
                                ImGui.SameLine();
                                ImGui.ColorEdit4($"{GuiResources.MapGuiText["AlternateColourLabel"]}##PriorityAlt", ref _priorityMobTextColourAlt,
                                    ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.PickerHueWheel);
                                ImGui.SameLine();
                                ImGui.ColorEdit4($"{GuiResources.MapGuiText["Background"]}##PriorityAlt", ref _priorityMobColourBackground,
                                    ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.PickerHueWheel);
                                ImGui.SameLine();
                                ImGuiUtil.ImGui_HelpMarker(GuiResources.MapGuiText["MobInfoColourToolTip"]);

                                ImGui.TableNextColumn();
                                ImGui.Checkbox("Nearby Mob List", ref _nearbyMobListEnabled);
                                ImGui.SameLine();
                                ImGuiUtil.ImGui_HelpMarker(GuiResources.MapGuiText["NearbyMobListToolTip"] +
                                                           GuiResources.MapGuiText["DragWidgetToolTip"]);
                                ImGui.DragFloat("X##Nearby Mobs", ref _nearbyMobListPosXPercentage, 0.05f, 0, 100,
                                    "%.2f",
                                    ImGuiSliderFlags.NoRoundToFormat);
                                ImGui.DragFloat("Y##Nearby Mobs", ref _nearbyMobListPosYPercentage, 0.05f, 0, 100,
                                    "%.2f",
                                    ImGuiSliderFlags.NoRoundToFormat);
                                ImGui.ColorEdit4($"{GuiResources.MapGuiText["MainTextColourLabel"]}##Nearby Mobs", ref _nearbyMobListColour,
                                    ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.PickerHueWheel);
                                ImGui.SameLine();
                                ImGui.ColorEdit4($"{GuiResources.MapGuiText["AlternateColourLabel"]}##Nearby Alt", ref _nearbyMobListColourAlt,
                                    ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.PickerHueWheel);
                                ImGui.SameLine();
                                ImGui.ColorEdit4($"{GuiResources.MapGuiText["Background"]}##Nearby Mobs", ref _nearbyMobListColourBackground,
                                    ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.PickerHueWheel);
                                ImGui.SameLine();
                                ImGuiUtil.ImGui_HelpMarker(GuiResources.MapGuiText["MobInfoColourToolTip"]);

                                ImGui.EndTable();
                            }
                            ImGui.EndChild();
                        }

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem(GuiResources.MapGuiText["StatsTab"]))
                    {
                        _bottomPanelHeight = 120f * ImGuiHelpers.GlobalScale;

                        ImGuiUtil.DoStuffWithMonoFont(() =>
                            {
                                ImGui.TextUnformatted(GuiResources.MapGuiText["StatsTabMessage"]);
                                ImGui.TextUnformatted($"S: {_configuration.SFoundCount:00000}");
                                ImGui.TextUnformatted($"A: {_configuration.AFoundCount:00000}");
                                ImGui.TextUnformatted($"B: {_configuration.BFoundCount:00000}");
                                //yes these count duplicates, sue me
                            }
                        );
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Language"))
                    {
                        _bottomPanelHeight = 120f * ImGuiHelpers.GlobalScale;
                        var languages = GuiResources.GetAvailableLanguages();
                        var index = Array.IndexOf(languages, _guiLanguage);

                        ImGui.PushItemWidth(200f);
                        if (ImGui.Combo("##language selection", ref index, languages, languages.Length))
                        {
                            _guiLanguage = languages[index];
                        }
                        ImGui.PopItemWidth();
                        ImGui.SameLine();

                        if (ImGui.Button("OK##change language"))
                        {
                            GuiResources.LoadGuiText(_guiLanguage);
                        }
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Translate"))
                    {
                        _bottomPanelHeight = 120f * ImGuiHelpers.GlobalScale;
                        ImGui.TextWrapped("If you want to help translate this plugin");
                        var url = "https://github.com/imaginary-png/HuntHelper/tree/main/Translate";
                        ImGui.InputText(" -- click me", ref url, 100, ImGuiInputTextFlags.ReadOnly);
                        if (ImGui.IsItemClicked())
                        {
                            System.Diagnostics.Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                        }
                        ImGui.TextWrapped("or dm me @ img#7855");
                        ImGui.EndTabItem();
                    }
                    ImGui.EndTabBar();
                }
            }
            ImGui.PopStyleColor();
            ImGui.End();
        }


        public void SavePreset(int presetNumber)
        {
            if (presetNumber is <= 0 or > 2) return;
            var winSize = presetNumber == 1 ? _presetOneWindowSize : _presetTwoWindowSize;
            _configuration.SaveMainMapPreset(winSize, _mapWindowOpacityAsPercentage, _mapImageOpacityAsPercentage, _useMapImages, _mapWindowPos, presetNumber);
        }
        public void SavePresetByCommand(int presetNumber)
        {
            var winSize = _currentWindowSize;
            if (presetNumber == 1) _presetOneWindowSize = winSize;
            else if (presetNumber == 2) _presetOneWindowSize = winSize;
            else return;
            _configuration.SaveMainMapPreset(winSize, _mapWindowOpacityAsPercentage, _mapImageOpacityAsPercentage, _useMapImages, _mapWindowPos, presetNumber);
        }

        public void ApplyPreset(int presetNumber)
        {
            if (presetNumber is < 0 or > 2) return;
            var toApply = presetNumber == 1 ? _configuration.PresetOne : _configuration.PresetTwo;
            _currentWindowSize = toApply.WindowSize;
            _mapImageOpacityAsPercentage = toApply.MapOpacity;
            _mapWindowOpacityAsPercentage = toApply.WindowOpacity;
            _useMapImages = toApply.UseMap;
            Task.Run(() => ChangeMapWindowPosition(toApply));
        }

        private void ChangeMapWindowPosition(Preset toApply)
        {
            MapVisible = false;
            _mapWindowPos = toApply.WindowPosition;
            Thread.Sleep(50);
            MapVisible = true;
        }

        public void DrawSettingsWindow()
        {
            if (!SettingsVisible)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(300, 100), ImGuiCond.Always);
            if (ImGui.Begin("help, i've fallen over", ref _settingsVisible,
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGuiUtil.ImGui_CentreText(GuiResources.MapGuiText["DefaultSettingsWindowMessage"], _defaultTextColour);
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
            _configuration.TTSVolume = _ttsVoiceVolume;
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
            _configuration.EnableBackgroundScan = _enableBackgroundScan;
            _configuration.ShowOptionsWindow = _showOptionsWindow;
            _configuration.FlyTextAEnabled = _flyTxtAEnabled;
            _configuration.FlyTextBEnabled = _flyTxtBEnabled;
            _configuration.FlyTextSEnabled = _flyTxtSEnabled;
            _configuration.PointToARank = _pointToARank;
            _configuration.PointToBRank = _pointToBRank;
            _configuration.PointToSRank = _pointToSRank;
            _configuration.PointerDiamondSizeModifier = _diamondModifier;

            _configuration.Language = GuiResources.Language;

            _configuration.Save();
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
            _presetOneWindowSize = _configuration.PresetOne.WindowSize;
            _presetTwoWindowSize = _configuration.PresetTwo.WindowSize;
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
            _ttsVoiceVolume = _configuration.TTSVolume;
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
            _enableBackgroundScan = _configuration.EnableBackgroundScan;
            _showOptionsWindow = _configuration.ShowOptionsWindow;
            _flyTxtAEnabled = _configuration.FlyTextAEnabled;
            _flyTxtBEnabled = _configuration.FlyTextBEnabled;
            _flyTxtSEnabled = _configuration.FlyTextSEnabled;
            _pointToARank = _configuration.PointToARank;
            _pointToBRank = _configuration.PointToBRank;
            _pointToSRank = _configuration.PointToSRank;
            _diamondModifier = _configuration.PointerDiamondSizeModifier;

            //if voice name available on user's pc, set as tts voice. --else default already set.
            if (!_huntManager.DontUseSynthesizer && _huntManager.TTS.GetInstalledVoices().Any(v => v.VoiceInfo.Name == _configuration.TTSVoiceName))
            {
                _ttsVoiceName = _configuration.TTSVoiceName;
                _huntManager.TTS.SelectVoice(_ttsVoiceName);
                _huntManager.TTSName = _ttsVoiceName;
            }

            _huntManager.ACount = _configuration.AFoundCount;
            _huntManager.BCount = _configuration.BFoundCount;
            _huntManager.SCount = _configuration.SFoundCount;

            //gui language loading 
            //if missing file, default config lang back to client lang
            if (!GuiResources.GetAvailableLanguages().Contains(_configuration.Language))
            {
                _configuration.Language = GuiResources.Language;
            } //redundant
            if (_configuration.Language == string.Empty)
            {
                _guiLanguage = GuiResources.Language;
            }
            //if language in config differs from client language, reload correct text
            else if (_configuration.Language != GuiResources.Language)
            {
                GuiResources.LoadGuiText(_configuration.Language);
                _guiLanguage = _configuration.Language;
            }
        }


        private void ClientState_TerritoryChanged(ushort e)
        {
            _territoryName = MapHelpers.GetMapName(_dataManager, _clientState.TerritoryType);
            //_worldName = _clientState.LocalPlayer?.CurrentWorld?.GameData?.Name.ToString() ?? "Not Found";
            _territoryId = _clientState.TerritoryType;
        }

        #region Draw Sub Windows

        private void DrawDataBaseWindow()
        {
            if (!_showDatabaseListWindow) return;

            ImGui.SetNextWindowSize(new Vector2(450, 800));
            ImGui.Begin(GuiResources.MapGuiText["LoadedHuntDataWindowTitle"], ref _showDatabaseListWindow);
            if (ImGui.BeginTabBar("info"))
            {
                if (ImGui.BeginTabItem(GuiResources.MapGuiText["LoadedHuntDataDatabaseTab"]))
                {
                    ImGuiUtil.DoStuffWithMonoFont(() => ImGuiUtil.ImGui_CentreText(_huntManager.GetDatabaseAsString()));
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem(GuiResources.MapGuiText["LoadedHuntDataSpawnPointsTab"]))
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
            var recordingSpawnPos = _mapDataManager.IsRecording(mapID);
            foreach (var sp in spawnPoints)
            {
                var drawPos = CoordinateToPositionInWindow(sp.Position);

                drawList.AddCircleFilled(drawPos, _spawnPointIconRadius,
                    sp.Taken && recordingSpawnPos
                        ? ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0f, 0f, 1f)) //red for taken spawn point
                        : ImGui.ColorConvertFloat4ToU32(_spawnPointColour));
                DoubleClickToToggleSpawnPointTaken(drawPos, sp);
            }
        }

        private void DoubleClickToToggleSpawnPointTaken(Vector2 spPos, SpawnPointPosition sp)
        {
            if (Vector2.Distance(ImGui.GetMousePos(), spPos) < _spawnPointIconRadius && _mapDataManager.IsRecording(_clientState.TerritoryType))
            {
                if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)) sp.Taken = !sp.Taken;
            }
        }

        #region Mob


        private void UpdateMobInfo()
        {
            var nearbyMobs = new List<BattleNpc>();
            //sift through and add any hunt mobs to new list
            foreach (var obj in _objectTable)
            {
                if (obj is not BattleNpc mob) continue;
                if (!_huntManager.IsHunt(mob.NameId)) continue;
                nearbyMobs.Add(mob);
                _huntManager.AddToTrain(mob, _territoryId, MapHelpers.GetMapID(_dataManager, _territoryId), _territoryName, _mapZoneMaxCoordSize);
            }

            if (nearbyMobs.Count == 0)
            {
                _huntManager.CurrentMobs.Clear();
                return;
            }

            _huntManager.AddNearbyMobs(nearbyMobs, _mapZoneMaxCoordSize, _territoryId, MapHelpers.GetMapID(_dataManager, _territoryId),
                _ttsAEnabled, _ttsBEnabled, _ttsSEnabled, _ttsAMessage, _ttsBMessage, _ttsSMessage,
                _chatAEnabled, _chatBEnabled, _chatSEnabled, _chatAMessage, _chatBMessage, _chatSMessage,
                _flyTxtAEnabled, _flyTxtBEnabled, _flyTxtSEnabled);
            UpdateStats();

            if (!MapVisible) return;

            var mobs = _huntManager.GetCurrentMobs();
            foreach (var mob in mobs) DrawMobIcon(mob);

            DrawPriorityMobInfo();
            DrawNearbyMobInfo();

            //draws 'quest-like' type pointers thingies --only when map window active
            /*_huntManager.GetAllCurrentMobsWithRank().ForEach((item) =>
                PointToMobsBecauseBlind(item.Rank, item.Mob));*/
        }


        private void DrawMobIcon(BattleNpc mob)
        {
            var mobInGamePos = new Vector2(ConvertPosToCoordinate(mob.Position.X), ConvertPosToCoordinate(mob.Position.Z));
            var mobWindowPos = CoordinateToPositionInWindow(mobInGamePos);
            var drawlist = ImGui.GetWindowDrawList();
            drawlist.AddCircleFilled(mobWindowPos, _mobIconRadius, ImGui.ColorConvertFloat4ToU32(_mobColour));
            if (_mapDataManager.IsRecording(_territoryId) && mob.NameId is not Constants.SS_Ker and not Constants.SS_Forgiven_Rebellion) ConfirmTakenSpawnPoint(mobInGamePos);
            //draw mob icon tooltip
            if (Vector2.Distance(ImGui.GetMousePos(), mobWindowPos) < _mobIconRadius * _mouseOverDistanceModifier)
            {
                DrawMobIconToolTip(mob);
            }
        }

        private void ConfirmTakenSpawnPoint(Vector2 position)
        {
            var spawnPoints = _mapDataManager.SpawnPointsList.First(msp => msp.MapID == _territoryId);
            SpawnPointPosition? takenSp = null;
            var smallestDistance = 1f;
            foreach (var sp in spawnPoints.Positions)
            {
                var dist = Vector2.Distance(sp.Position, position);
                if (!(dist < smallestDistance)) continue;
                smallestDistance = dist;
                takenSp = sp;
            }
            var index = spawnPoints.Positions.IndexOf(takenSp!);
            if (index < 0) return;
            spawnPoints.Positions[index].Taken = true;

        }

        private void DrawMobIconToolTip(BattleNpc mob)
        {
            var text = new string[]
            {
                $"{mob.Name}",
                $"({ConvertPosToCoordinate(mob.Position.X)}, {ConvertPosToCoordinate(mob.Position.Z)})",
                $"{Math.Round(mob.CurrentHp * 1.0 / mob.MaxHp * 100, 2)}%%"
            };
            ImGui_ToolTip(text);
        }

        private void DrawPriorityMobInfo()
        {
            if (!_priorityMobEnabled) return;

            var (rank, mob) = _huntManager.GetPriorityMob();
            if (mob == null) return;

            ImGuiUtil.DoStuffWithMonoFont(() =>
            {
                SetCursorPosByPercentage(_priorityMobInfoPosXPercentage, _priorityMobInfoPosYPercentage);
                var info = new string[]
                {
                    $"   {rank}  |  {mob.Name}  |  {_huntManager.GetHPP(mob):0.00}%%  ",
                    $"({ConvertPosToCoordinate(mob.Position.X):0.00}, {ConvertPosToCoordinate(mob.Position.Z):0.00})"
                };

                //SetCursorPosByPercentage has moved the cursor (where next element will be drawn)
                //now before drawing child, work out position based on cursor pos, window pos (top left) and mouse pos
                //to have detection for mouseover tooltip
                var labelVector = ImGui.GetCursorPos() + ImGui.GetWindowPos() + new Vector2(140, 20);//adding half of child x,y size to get middle of element
                                                                                                     //fixed sizing.... mouse over tooltip in case long-ass mob name clips 
                if (ImGui.GetMousePos().X - labelVector.X is < 140 and > -140 &&  //Why didn't I try isitem/windowhovered? bcoz i'm stupid
                    ImGui.GetMousePos().Y - labelVector.Y is < 20 and > -20)
                {
                    ImGui_ToolTip(info);
                    MouseClickToSendChatFlag(mob);
                }


                ImGui.PushStyleColor(ImGuiCol.Border, _priorityMobColourBackground);
                ImGui.PushStyleColor(ImGuiCol.ChildBg, _priorityMobColourBackground);
                ImGui.BeginChild("Priorty Mob Label", new Vector2(280f, 40f), true, ImGuiWindowFlags.NoScrollbar);
                var colour = _useMapImages ? _priorityMobTextColourAlt : _priorityMobTextColour; //if using map image, use alt colour.
                ImGuiUtil.ImGui_CentreText(
                     $" {rank}  |  {mob.Name}  |  {_huntManager.GetHPP(mob):0.00}%%",
                     colour);
                ImGuiUtil.ImGui_CentreText(
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

            ImGuiUtil.DoStuffWithMonoFont(() =>
            {
                SetCursorPosByPercentage(_nearbyMobListPosXPercentage, _nearbyMobListPosYPercentage);
                ImGui.PushStyleColor(ImGuiCol.Border, _nearbyMobListColourBackground);
                ImGui.PushStyleColor(ImGuiCol.ChildBg, _nearbyMobListColourBackground);
                ImGui.BeginChild("Nearby Mob List Info", size, true, ImGuiWindowFlags.NoScrollbar);
                var colour = _useMapImages ? _nearbyMobListColourAlt : _nearbyMobListColour; //if using map image, use alt colour.
                ImGui.Separator();
                foreach (var (rank, mob) in nearbyMobs)
                {
                    var labelVector = ImGui.GetCursorPos() + ImGui.GetWindowPos() + new Vector2(size.X / 2, size.Y / nearbyMobs.Count / 2);

                    //MOUSE OVER TOOLTIP
                    if (ImGui.GetMousePos().X - labelVector.X is < 100 and > -100 && //this is a mess, but it works.
                        ImGui.GetMousePos().Y - labelVector.Y < size.Y / nearbyMobs.Count / 2 - 4f && ImGui.GetMousePos().Y - labelVector.Y > -(size.Y / nearbyMobs.Count / 2) - 5f)
                    {
                        ImGui_ToolTip(new string[]
                        {
                            $"   {rank} {GuiResources.MapGuiText["RankLabel"]} | {_huntManager.GetHPP(mob):0.00}%%  ",
                            $"{mob.Name}",
                            "----------------------",
                            GuiResources.MapGuiText["NearbyMobListFlagToolTip"]
                        });

                        MouseClickToSendChatFlag(mob);
                    }

                    //ACTUAL LABEL SHOWN ON SCREEN - how to format, so ugly - how to change font size? :(
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 2.5f);
                    ImGuiUtil.ImGui_CentreText($"{rank} | {mob.Name} | {_huntManager.GetHPP(mob):0}%% \n", colour);
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 2.5f);
                    ImGuiUtil.ImGui_CentreText($"({ConvertPosToCoordinate(mob.Position.X):0.0}, {ConvertPosToCoordinate(mob.Position.Z):0.0})", colour);
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5f);
                    ImGui.Separator();
                }
                ImGui.EndChild();
                ImGui.PopStyleColor(2);
            });


        }
        private void MouseClickToSendChatFlag(BattleNpc mob)
        {
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) ||
                ImGui.IsMouseClicked(ImGuiMouseButton.Middle) ||
                ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                _huntManager.SendChatMessage(true,
                    "<exclamationrectangle> <name> <flag> -- <rank> <exclamationrectangle>",
                    _territoryId, MapHelpers.GetMapID(_dataManager, _territoryId),
                    mob, _mapZoneMaxCoordSize);
            }
        }
        #endregion



        #region Player
        private void UpdatePlayerInfo()
        {
            //if this stuff ain't null, draw player position
            if (_clientState?.LocalPlayer?.Position != null)
            {
                var playerPos = CoordinateToPositionInWindow(
                    new Vector2(ConvertPosToCoordinate(_clientState!.LocalPlayer.Position.X),
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
            ImGui.TextUnformatted("Random Debug Info?!:");
            ImGui.BeginChild("debug");
            ImGui.Columns(2);
            ImGui.TextUnformatted($"Content1 Region: {ImGui.GetContentRegionAvail()}");
            ImGui.TextUnformatted($"Window Size: {ImGui.GetWindowSize()}");
            ImGui.TextUnformatted($"Window  Pos: {ImGui.GetWindowPos()}");

            if (_clientState?.LocalPlayer?.Position != null)
            {
                ImGui.TextUnformatted($"rotation: {_clientState!.LocalPlayer.Rotation}");
                var rotation = Math.Abs(_clientState.LocalPlayer.Rotation - Math.PI);
                ImGui.TextUnformatted($"rotation rad.: {Math.Round(rotation, 2):0.00}");
                ImGui.TextUnformatted($"rotation sin.: {Math.Round(Math.Sin(rotation), 2):0.00}");
                ImGui.TextUnformatted($"rotation cos.: {Math.Round(Math.Cos(rotation), 2):0.00}");
                ImGui.TextUnformatted($"Player Pos: (" +
                                   $"{MapHelpers.ConvertToMapCoordinate(_clientState.LocalPlayer.Position.X, _mapZoneMaxCoordSize):0.00}" +
                                   $"," +
                                   $" {MapHelpers.ConvertToMapCoordinate(_clientState.LocalPlayer.Position.Z, _mapZoneMaxCoordSize):0.00}" +
                                   $")");
                var playerPos = CoordinateToPositionInWindow(
                    new Vector2(ConvertPosToCoordinate(_clientState.LocalPlayer.Position.X),
                        ConvertPosToCoordinate(_clientState.LocalPlayer.Position.Z)));
                ImGui.TextUnformatted($"Player Pos: {playerPos}");
                ImGui.NextColumn();
                ImGuiUtil.ImGui_RightAlignText($"Map: {_territoryName} - {_territoryId}");
                ImGuiUtil.ImGui_RightAlignText($"Coord size: {_mapZoneMaxCoordSize} ");

                //priority mob stuff
                ImGuiUtil.ImGui_RightAlignText("Priority Mob:");
                var priorityMob = _huntManager.GetPriorityMob().Mob;
                if (priorityMob != null)
                {
                    ImGuiUtil.ImGui_RightAlignText($"Rank {_huntManager.GetPriorityMob().Rank} " +
                                                        $"{priorityMob.Name} " +
                                                        $"{Math.Round(1.0 * priorityMob.CurrentHp / priorityMob.MaxHp * 100, 2):0.00}% | " +
                                                        $"({ConvertPosToCoordinate(priorityMob.Position.X):0.00}, " +
                                                        $"{ConvertPosToCoordinate(priorityMob.Position.Z):0.00}) ");
                }

                var currentMobs = _huntManager.GetAllCurrentMobsWithRank();
                ImGuiUtil.ImGui_RightAlignText("--");
                ImGuiUtil.ImGui_RightAlignText("Other nearby mobs:");
                foreach (var (rank, mob) in currentMobs)
                {
                    ImGuiUtil.ImGui_RightAlignText("--");
                    ImGuiUtil.ImGui_RightAlignText($"Rank: {rank} | " +
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


        private void ImGui_ToolTip(string[] text)
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 50.0f);
            ImGui.Separator();

            foreach (var s in text)
            {
                ImGuiUtil.ImGui_CentreText(s, _defaultTextColour);
            }
            ImGui.Separator();
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().IndentSpacing * 0.5f);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }

        private void LoadMapImages()
        {
            if (!_loadingImagesAttempt.IsCompleted) return;
            if (!_useMapImages) return;
            _loadingImagesAttempt = Task.Run(() => _huntManager.LoadMapImages());
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
            return MapHelpers.ConvertToMapCoordinate(pos, _mapZoneMaxCoordSize);
        }

        private Vector2 CoordinateToPositionInWindow(Vector2 pos)
        {
            var x = (pos.X - 1) * SingleCoordSize + ImGui.GetWindowPos().X;
            var y = (pos.Y - 1) * SingleCoordSize + ImGui.GetWindowPos().Y;
            return new Vector2(x, y);
        }

        private static void SetCursorPosByPercentage(float percentX, float percentY)
        {
            var winSize = ImGui.GetWindowSize();
            var textHeight = ImGui.GetTextLineHeight();
            //cant' easily get width and idc to try
            ImGui.SetCursorPos(new Vector2(winSize.X * percentX / 100, winSize.Y * percentY / 100 - textHeight));
        }


        private void MapImageDownloadWindow()
        {
            ImGui.SetNextWindowSize(new Vector2(710, 240));
            if (ImGui.Begin(GuiResources.MapGuiText["ImageDownloadWindowTitle"], ref _useMapImages))
            {
                var url = Constants.RepoUrl;
                var imageDir = _huntManager.ImageFolderPath;
                if (_huntManager.DownloadingImages) //show this is in process of downloading
                {
                    ImGuiUtil.DoStuffWithMonoFont(() =>
                        ImGui.TextUnformatted(GuiResources.MapGuiText["ImageDownloadingMessage"]));
                }
                else if (_huntManager.HasDownloadErrors) //show this if any download errors
                {
                    ImGuiUtil.DoStuffWithMonoFont(() => //successfully (manually) tested with failed image.
                    {
                        ImGui.Text(GuiResources.MapGuiText["ImageDownloadErrorMessageOne"]);
                        _huntManager.DownloadErrors.ForEach(e => ImGui.Text(e));
                        ImGui.Text(GuiResources.MapGuiText["ImageDownloadErrorMessageTwo"]);
                        if (ImGui.Button(GuiResources.MapGuiText["ImageDownloadErrorButton"]))
                        {
                            _huntManager.HasDownloadErrors = false;
                            _huntManager.DownloadErrors.Clear();
                        }
                    });
                }
                else //should be the first thing shown, download prompt.
                {
                    ImGuiUtil.DoStuffWithMonoFont(() =>
                    {
                        ImGui.Dummy(Vector2.Zero);
                        ImGui.TextWrapped(GuiResources.MapGuiText["ImageDownloadPromptMessage"]);
                        ImGui.SameLine();
                        if (ImGui.Button($"{GuiResources.MapGuiText["ImageDownloadButton"]}##download")) _huntManager.DownloadImages(_mapDataManager.SpawnPointsList);
                        ImGui.Text(GuiResources.MapGuiText["ImageDownloadManualMessageOne"]);
                        ImGui.Text($"");
                        ImGui.Dummy(Vector2.Zero);

                    });
                }

                ImGui.Dummy(new Vector2(52, 0));
                ImGui.InputText("##url", ref url, 30, ImGuiInputTextFlags.ReadOnly);
                if (ImGui.IsItemHovered())
                {
                    if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    {
                        System.Diagnostics.Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    }
                }

                ImGui.Text(GuiResources.MapGuiText["ImageDownloadManualMessageTwo"]);
                ImGui.Dummy(new Vector2(52, 0));
                ImGui.InputText("##folder dir", ref imageDir, 30, ImGuiInputTextFlags.ReadOnly);
                ImGui.End();
            }
        }

        private void UpdateStats()
        {
            _configuration.AFoundCount = _huntManager.ACount;
            _configuration.BFoundCount = _huntManager.BCount;
            _configuration.SFoundCount = _huntManager.SCount;
        }

    }
}
