using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HuntHelper.Managers.Hunts;
using HuntHelper.Utilities;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Numerics;

namespace HuntHelper.Gui
{
    internal class DebugUI
    {
        private readonly IAetheryteList _aeth;
        private readonly IDataManager _dataManager;
        private readonly IClientState _clientState;
        private readonly IObjectTable _objectTable;
        private readonly HuntManager _huntManager;

        private string _territoryName;
        private string _worldName;
        private ushort _territoryId;
        private uint _instance;
        private float _mapZoneMaxCoordSize;
        public float SingleCoordSize => ImGui.GetWindowSize().X / _mapZoneMaxCoordSize;

        private bool _randomDebugWindowVisible = false;
        public bool RandomDebugWindowVisisble = false;

        public DebugUI(IAetheryteList aeth, IDataManager dataManager, IClientState clientState, HuntManager huntManager, IObjectTable objectTable)
        {
            _aeth = aeth;
            _dataManager = dataManager;
            _clientState = clientState;
            _huntManager = huntManager;
            _objectTable = objectTable;
            UpdateLocalStuff();
        }

        public unsafe void Draw()
        {
            if (!RandomDebugWindowVisisble)
            {
                return;
            }

            UpdateLocalStuff();

            ImGui.SetNextWindowSize(new Vector2(375, 330), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(375, 330), new Vector2(float.MaxValue, float.MaxValue));
            if (ImGui.Begin("Debug stuff", ref RandomDebugWindowVisisble))
            {
                LocalObjects();
                PositionalInfo();
                Aetherytes();
            }
            ImGui.End();
        }

        private void LocalObjects()
        {
            ImGui.Text($"Territory: {_territoryName}{_instance}");
            ImGui.Text($"Territory ID: {_clientState.TerritoryType}");

            //PLAYER POS
            var v3 = _clientState.LocalPlayer?.Position ?? new Vector3(0, 0, 0);
            ImGui.Text($"pos: X: ({ConvertPosToCoordinate(v3.X)}, Y: {ConvertPosToCoordinate(v3.X)})\n");
            ImGui.NewLine();

            if (ImGui.CollapsingHeader("nearby hunts & object table debug"))
            {
                ImGui.BeginGroup();
                var nearby = _huntManager.GetCurrentMobs();
                ImGui.Text("Nearby Mobs");
                nearby.ForEach(m => ImGui.TextUnformatted($"{m.Name} - {m.NameId}"));
                ImGui.Text("============");

                var (rank, mob) = _huntManager.GetPriorityMob();
                ImGui.Text("priority:");
                ImGui.TextUnformatted($"{rank} - {mob?.Name}");
                ImGui.Text("============");
                ImGui.Text("");

                var hunt = "";
                foreach (var obj in _objectTable)
                {
                    if (obj is not IBattleNpc bobj) continue;
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
                ImGui.NewLine();
                ImGui.EndGroup();
            }

        }
        private void PositionalInfo()
        {
            if (ImGui.CollapsingHeader("positional debug"))
            {
                ImGui.BeginGroup();
                ImGui.TextUnformatted("Random Debug Info?!:");
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
                    ImGuiUtil.ImGui_RightAlignText($"Map: {_territoryName}{_instance} - {_territoryId}");
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
                }
                else
                {
                    ImGui.Text("Can't find local player");
                }
                ImGui.Columns(1);
                ImGui.NewLine();
                ImGui.EndGroup();
            }
        }

        /// <summary>
        /// sets to clipboard a new aetheryteData object line (used in TeleportManager.cs)
        /// </summary>
        private unsafe void Aetherytes()
        {
            var tele = Telepo.Instance();
            if (ImGui.CollapsingHeader("aetherytes"))
            {
                ImGui.BeginGroup();
                foreach (var a in _aeth)
                {
                    //ImGui.Text($"data: {a.AetheryteData}");
                    var d = a.AetheryteData;
                    var pn = _dataManager.Excel.GetSheet<Aetheryte>()?.GetRow(d.Id)?.PlaceName.Value;
                    //var name = _dataManager.Excel.GetSheet<PlaceName>().GetRow(pn).Name;
                    ImGui.PushFont(UiBuilder.MonoFont);
                    ImGui.Text($"{pn!.Name,26} | {"aetheryteid:",6}{a.AetheryteId,4} | {"subIndex:",6}{a.SubIndex,4} | {"territory:",6}{a.TerritoryId,4}");
                    ImGui.PopFont();

                    if (ImGui.Button($"clickme##{a.AetheryteId}"))
                    {
                        ImGui.SetClipboardText($"new AetheryteData() {{ AetheryteID = {a.AetheryteId}, SubIndex = {a.SubIndex}, TerritoryID = {a.TerritoryId}, Position = new Vector2()}}, //{pn.Name}");
                    }
                    ImGui.Separator();
                }
                ImGui.BeginGroup();
            }
        }




        private unsafe void UpdateLocalStuff()
        {
            _territoryName = MapHelpers.GetMapName(_clientState.TerritoryType);
            _worldName = _clientState.LocalPlayer?.CurrentWorld?.GameData?.Name.ToString() ?? "Not Found";
            _territoryId = _clientState.TerritoryType;
            _instance = (uint)UIState.Instance()->PublicInstance.InstanceId;
            _mapZoneMaxCoordSize = _huntManager.GetMapZoneCoordSize(_territoryId);
        }

        private float ConvertPosToCoordinate(float coord) { return MapHelpers.ConvertToMapCoordinate(coord, _mapZoneMaxCoordSize); }

        private Vector2 CoordinateToPositionInWindow(Vector2 pos)
        {
            var x = (pos.X - 1) * SingleCoordSize + ImGui.GetWindowPos().X;
            var y = (pos.Y - 1) * SingleCoordSize + ImGui.GetWindowPos().Y;
            return new Vector2(x, y);
        }
    }
}
