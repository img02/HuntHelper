using ImGuiNET;
using System;
using System.Numerics;
using System.Threading;
using Dalamud.Data;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Lumina.Excel.GeneratedSheets;

namespace HuntHelper
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    class PluginUI : IDisposable
    {
        private Configuration configuration;

        private ImGuiScene.TextureWrap goatImage;

        private ClientState ClientState;
        private ObjectTable ObjectTable;
        private DataManager DataManager;

        private String TerritoryName;
        private uint TerritoryID;


        // this extra bool exists for ImGui, since you can't ref a property
        private bool visible = false;
        public bool Visible
        {
            get { return this.visible; }
            set { this.visible = value; }
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

        // passing in the image here just for simplicity
        public PluginUI(Configuration configuration, ImGuiScene.TextureWrap goatImage, ClientState clientState, ObjectTable objectTable, DataManager dataManager)
        {
            //add hunt manager to this class

            this.configuration = configuration;
            this.goatImage = goatImage;

            this.ClientState = clientState;
            this.ObjectTable = objectTable;
            this.DataManager = dataManager;

            TerritoryName = String.Empty;
            ClientState_TerritoryChanged(null, 0);

            ClientState.TerritoryChanged += ClientState_TerritoryChanged;
        }



        public void Dispose()
        {
            this.goatImage.Dispose();
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


            DrawTestWindow();
        }

        public void DrawMainWindow()
        {
            if (!Visible)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(375, 330), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(375, 330), new Vector2(float.MaxValue, float.MaxValue));
            if (ImGui.Begin("My Amazing Window!", ref this.visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
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

        private float thickness = 8f;
        private Vector2 pos = new Vector2(500, 500);
        //private Vector2 lineEnding = new Vector2(0, 0);
        public void DrawTestWindow()
        {
            if (!TestVisible)
            {
                return;
            }
            
            ImGui.SetNextWindowSize(new Vector2(375, 330), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(375, 330), new Vector2(float.MaxValue, float.MaxValue));
            //sets window pos
            ImGui.SetNextWindowPos(Vector2.Divide(pos,2f), ImGuiCond.FirstUseEver);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0,0));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize,0f);


            if (ImGui.Begin("Test Window!", ref this.testVisible,
                    ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoTitleBar))
            {
                //update pos to stay in place when window moves. equation is 'current window pos' (topleft) + 'half of window height'.
                pos = Vector2.Add(ImGui.GetWindowPos(), new Vector2(ImGui.GetWindowSize().Y/2)); 

                var drawlist = ImGui.GetWindowDrawList();
                
                /*ImGui.DragFloat("Thickness", ref thickness);
                ImGui.DragFloat2("pos", ref pos);

                drawlist.AddCircle(
                    //Vector2.Divide(ImGui.GetWindowSize(),2),
                    //ImGui.GetWindowPos() + Vector2.Divide(ImGui.GetWindowSize(), 2),
                    pos,
                    100,
                    ImGui.ColorConvertFloat4ToU32(new Vector4(0.9f, .16f, 0.567f, 1f)),
                    0,
                    thickness);*/

                /*drawlist.AddCircleFilled(
                   //Vector2.Divide(ImGui.GetWindowSize(),2),
                   ImGui.GetWindowPos() + Vector2.Divide(ImGui.GetWindowSize(), 2),
                   100,
                   ImGui.ColorConvertFloat4ToU32(new Vector4(0.9f, .16f, 0.567f, 1f)));

               drawlist.AddCircleFilled(
                   //Vector2.Divide(ImGui.GetWindowSize(),2),
                   ImGui.GetWindowPos() + Vector2.Divide(ImGui.GetWindowSize(), 1.5f),
                   100,
                   ImGui.ColorConvertFloat4ToU32(new Vector4(0.4f, 0.5f, 0.567f, 1f)));*/

                #region Drawing Mob and Player circles

                var gridRatioX = ImGui.GetContentRegionAvail().X / 41;
                var gridRatioY = ImGui.GetContentRegionAvail().Y / 41;

                foreach (var obj in this.ObjectTable)
                {
                    if (obj is BattleNpc bnpc)
                    {
                        if (bnpc.MaxHp < 10000) continue;
                        if (bnpc.BattleNpcKind != BattleNpcSubKind.Enemy) continue;

                        //Todo - make these easier to read...
                        var mobPos = new Vector2(ImGui.GetWindowPos().X + gridRatioX * ((float)Utilities.MapHelpers.ConvertToMapCoordinate(obj.Position.X)-1),
                            ImGui.GetWindowPos().Y + gridRatioY * ((float)Utilities.MapHelpers.ConvertToMapCoordinate(obj.Position.Z)-1));

                        drawlist.AddCircleFilled(mobPos, 8, ImGui.ColorConvertFloat4ToU32(new Vector4(0.4f, 1f, 0.567f, 1f)));
                    }
                }

                //if this stuff ain't null, draw player position
                if (ClientState?.LocalPlayer?.Position != null)
                {
                    //Todo - make these easier to read...
                    var playerPos = new Vector2(
                        ImGui.GetWindowPos().X + gridRatioX *
                        ((float) Utilities.MapHelpers.ConvertToMapCoordinate(ClientState.LocalPlayer.Position.X) - 1),
                        ImGui.GetWindowPos().Y + gridRatioY *
                        ((float) Utilities.MapHelpers.ConvertToMapCoordinate(ClientState.LocalPlayer.Position.Z) - 1));

                    //Todo - make these easier to read...
                    //green - Player Position Circle Marker --DRAWN BELOW
                    var playerCircleRadius = 4;


                    //aoe detection circle = ~2 in-game coords. --DRAWN BELOW
var detectionRadius = 2 * (ImGui.GetContentRegionAvail().X / 41);
                    

                    // DRAW A LINE STARTING FROM PLAYERPOS CENTER, AND GOING OUT DEPENDING ON PLAYER ROTATION 
                    //SOMETHING TO DO WTH PI AND FLOATS AND SHIT. IDK. TRIG. W/E.

                    ImGui.Text($"window padding: {ImGui.GetStyle().WindowPadding}");

                    //var rotation = Math.Abs(ClientState.LocalPlayer.Rotation * (180 / Math.PI) - 180);
                    var rotation = Math.Abs(ClientState.LocalPlayer.Rotation- Math.PI);

                    //idk trig man... it's been years...
                    ImGui.Text($"rotation: {ClientState.LocalPlayer.Rotation}");
                    ImGui.Text($"rotation deg.: {Math.Round(rotation,2)}");
                    ImGui.Text($"rotation sin.: {Math.Round(Math.Sin(rotation),2)}");
                    ImGui.Text($"rotation cos.: {Math.Round(Math.Cos(rotation),2)}");
                    ImGui.Text($"rotation rad.: {Math.Abs(ClientState.LocalPlayer.Rotation - (Math.PI))}");
                    
                   
                    var lineEnding = 
                        new Vector2( 
                            playerPos.X + (float)(detectionRadius * Math.Sin(rotation)),
                            playerPos.Y - (float)(detectionRadius * Math.Cos(rotation)));
                    
                    #region Player Icon Drawing - order: direction, detection, player

                    drawlist.AddLine(playerPos, lineEnding, ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.3f, 0.3f, 1f)), 2);
                    drawlist.AddCircleFilled(playerPos, playerCircleRadius,
                        ImGui.ColorConvertFloat4ToU32(new Vector4(.5f, 1f, 0.567f, 1f)));
                    drawlist.AddCircle(playerPos, detectionRadius,
                        ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 0f, 1f)), 0, 1f);

                    #endregion

                    ImGui.Text($"Line Ending: {lineEnding.X}, {lineEnding.Y}");
                    ImGui.Text($"Player Pos: {playerPos.X}, {playerPos.Y}");
                    #endregion
                }

                // ImGui.Image(goatImage.ImGuiHandle, new Vector2(goatImage.Width, goatImage.Height ));
            }

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
                var configValue = this.configuration.SomePropertyToBeSavedAndWithADefault;
                if (ImGui.Checkbox("Random Config Bool", ref configValue))
                {
                    this.configuration.SomePropertyToBeSavedAndWithADefault = configValue;
                    // can save immediately on change, if you don't want to provide a "Save and Close" button
                    this.configuration.Save();
                }
            }
            ImGui.End();
        }


        private void ClientState_TerritoryChanged(object? sender, ushort e)
        {
            //TerritoryName = DataManager.Excel.GetSheet<TerritoryType>()?.GetRow(this.ClientState.TerritoryType)?.PlaceName?.Value?.Name.ToString() ?? "location not found";
            TerritoryName = Utilities.MapHelpers.GetMapName(DataManager, this.ClientState.TerritoryType);
            TerritoryID = ClientState.TerritoryType;

        }

        //redundant...
        private double ConvertPosToCoordinate(float pos)
        {
            return Utilities.MapHelpers.ConvertToMapCoordinate(pos);
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
