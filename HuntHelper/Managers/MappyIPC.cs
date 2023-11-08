using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Ipc.Exceptions;
using HuntHelper.Utilities;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace HuntHelper.Managers
{
    public class MappyIPC
    {
        private List<string> SpawnMarkers = new();
        private List<string> MobMarkers = new();


        //https://github.com/MidoriKami/Mappy/blob/master/Mappy/Controllers/IpcController.cs#L66

        public ICallGateSubscriber<uint, Vector2, uint, string, string, string>? AddWorldMarkerIpcFunction = null;
        //public ICallGateSubscriber<uint, Vector2, uint, string, string, string>? AddTextureMarkerIpcFunction = null;
        private readonly ICallGateSubscriber<uint, Vector2, uint, string, string, string>? AddMapCoordinateMarkerIpcFunction = null;
        private readonly ICallGateSubscriber<string, bool>? RemoveMarkerIpcFunction = null;
        //public ICallGateSubscriber<string, Vector2, bool>? UpdateMarkerIpcFunction = null;

        public ICallGateSubscriber<Vector2, Vector2, uint, Vector4, float, string>? AddTextureLineIpcFunction = null;
        private readonly ICallGateSubscriber<Vector2, Vector2, uint, Vector4, float, string>? AddMapCoordLineIpcFunction = null;

        private readonly ICallGateSubscriber<Vector2, float, uint, Vector4, int, string>? AddMapCoordCircleFilled = null;
        private readonly ICallGateSubscriber<Vector2, float, uint, Vector4, int, string>? AddTextureCircleFilled = null;
        private readonly ICallGateSubscriber<Vector2, float, uint, Vector4, int, float, string>? AddMapCoordCircle = null;
        private readonly ICallGateSubscriber<Vector2, float, uint, Vector4, int, float, string>? AddTextureCircle = null;
        private readonly ICallGateSubscriber<string, bool>? RemoveCircle = null;

        private readonly ICallGateSubscriber<string, bool>? RemoveLineIpcFunction = null;
        private readonly ICallGateSubscriber<bool>? IsReadyIpcFunction = null;

        private DalamudPluginInterface _pluginInterface;
        private Configuration _config;


        public MappyIPC(DalamudPluginInterface pluginInterface, Configuration config)
        {
            _pluginInterface = pluginInterface;
            _config = config;

            // Copy/Paste this to subscribe to these functions, be sure to check for IPCNotReady exceptions ;)
            AddWorldMarkerIpcFunction = _pluginInterface.GetIpcSubscriber<uint, Vector2, uint, string, string, string>("Mappy.World.AddMarker");
            //AddTextureMarkerIpcFunction = Service.PluginInterface.GetIpcSubscriber<uint, Vector2, uint, string, string, string>("Mappy.Texture.AddMarker");
            AddMapCoordinateMarkerIpcFunction = _pluginInterface.GetIpcSubscriber<uint, Vector2, uint, string, string, string>("Mappy.MapCoord.AddMarker");
            RemoveMarkerIpcFunction = _pluginInterface.GetIpcSubscriber<string, bool>("Mappy.RemoveMarker");
            //UpdateMarkerIpcFunction = Service.PluginInterface.GetIpcSubscriber<string, Vector2, bool>("Mappy.UpdateMarker");

            AddTextureLineIpcFunction = _pluginInterface.GetIpcSubscriber<Vector2, Vector2, uint, Vector4, float, string>("Mappy.Texture.AddLine");
            AddMapCoordLineIpcFunction = _pluginInterface.GetIpcSubscriber<Vector2, Vector2, uint, Vector4, float, string>("Mappy.MapCoord.AddLine");

            AddMapCoordCircleFilled = _pluginInterface.GetIpcSubscriber<Vector2, float, uint, Vector4, int, string>("Mappy.Mapcoord.AddCircleFilled");
            AddTextureCircleFilled  = _pluginInterface.GetIpcSubscriber<Vector2, float, uint, Vector4, int, string>("Mappy.Texture.AddCircleFilled");

            AddMapCoordCircle = _pluginInterface.GetIpcSubscriber<Vector2, float, uint, Vector4, int, float, string>("Mappy.Mapcoord.AddCircle");
            AddTextureCircle  = _pluginInterface.GetIpcSubscriber<Vector2, float, uint, Vector4, int, float, string>("Mappy.Texture.AddCircle");

            RemoveCircle = _pluginInterface.GetIpcSubscriber<string, bool>("Mappy.RemoveCircle");


            RemoveLineIpcFunction = _pluginInterface.GetIpcSubscriber<string, bool>("Mappy.RemoveLine");
            IsReadyIpcFunction = _pluginInterface.GetIpcSubscriber<bool>("Mappy.IsReady");
        }
        
        public bool IsReady()
        {
            try
            {
                return IsReadyIpcFunction!.InvokeFunc();
            }
            catch (IpcNotReadyError e)
            {
                //PluginLog.Verbose("Mappy plugin not found.");
                return false;
            }
        }

        #region no longer used -- used for drawing a single maps spawn points
        public bool HasSpawnPoints() => SpawnMarkers.Count > 0;

        /// <Exception cref="IpcNotReadyError">If Mappy plugin not installed or not ready</exception>
        public bool AddSpawnPointMarker(Vector2 coordinates)
        {
            if (!IsReady())
            {
                //PluginLog.Verbose("Could not add marker, Mappy IPC not ready.");
                return false;
            }

            var colour = _config.MappyUseCustomColours ? _config.SpawnPointColour : LineColour;

            //draws an X
            var id = AddMapCoordLineIpcFunction!.InvokeFunc(coordinates + new Vector2(-LineOffset, -LineOffset),
                coordinates + new Vector2(LineOffset, LineOffset), 0, colour, LineThickness);

            var id2 = AddMapCoordLineIpcFunction.InvokeFunc(coordinates + new Vector2(-LineOffset, LineOffset),
                coordinates + new Vector2(LineOffset, -LineOffset), 0, colour, LineThickness);

            SpawnMarkers.Add(id);
            SpawnMarkers.Add(id2);
            return true;
        }
        #endregion

        /// <Exception cref="IpcNotReadyError">If Mappy plugin not installed or not ready</exception>
        public bool AddSpawnPointMarker(Vector2 coordinates, uint mapId)
        {
            if (!IsReady())
            {
                //PluginLog.Verbose("Could not add marker, Mappy IPC not ready.");
                return false;
            }

            var colour = _config.MappyUseCustomColours ? _config.SpawnPointColour : LineColour;

            //draws an X
            var id = AddMapCoordLineIpcFunction!.InvokeFunc(coordinates + new Vector2(-LineOffset, -LineOffset),
                coordinates + new Vector2(LineOffset, LineOffset), mapId, colour, LineThickness);

            var id2 = AddMapCoordLineIpcFunction.InvokeFunc(coordinates + new Vector2(-LineOffset, LineOffset),
                coordinates + new Vector2(LineOffset, -LineOffset), mapId, colour, LineThickness);
                       
            SpawnMarkers.Add(id);
            SpawnMarkers.Add(id2);
            return true;
        }

        private float LineOffset = 0.15f;
        private float LineThickness = 5;
        //private Vector4 LineColour = new Vector4(1f, 0.4f, 0.4f, 1f);
        private Vector4 LineColour = new Vector4(0.35f, 0.35f, 1f, 1f);
                
        public void ClearSpawnPointMarkers()
        {
            try
            {
                PluginLog.Verbose("Clearing spawn points");
                SpawnMarkers.ForEach(m => RemoveMarkerIpcFunction!.InvokeFunc(m));
                SpawnMarkers.Clear();
            }
            catch (IpcNotReadyError)
            { }
        }       

        /// <Exception cref="IpcNotReadyError">If Mappy plugin not installed or not ready</exception>
        public void AddMobMarkerWithCircle(Vector2 centre, float radius, Vector4 colour, int segments)
        {
            var id = AddMapCoordCircleFilled!.InvokeFunc(centre, radius, 0, colour, segments);
            MobMarkers.Add(id);
        }

        /// <summary>
        /// Why did I call this 'RemoveMobMakers' but 'ClearSpawnPointMarkers'? i do not know.
        /// </summary        
        public void RemoveMobMarkers()
        {
            try
            {
                MobMarkers.ForEach(m =>
                {
                    RemoveMarkerIpcFunction!.InvokeFunc(m);
                });
                MobMarkers.Clear();
            }
            catch (IpcNotReadyError) { }
        }

        /// <summary>
        /// If 'Use Mappy' disabled, this will immediately return.
        /// </summary>
        public void OnCurrentMobChange(IList<(HuntRank, BattleNpc)> mobs, float zoneCoordSize)
        {
            /*
             * Cute :https://xivapi.com/docs/Icons?set=icons082000
			-	082057 pink bomb thing
			-	082058 fall guys blue thingy
			-	
			
			https://xivapi.com/docs/Icons?set=icons071000
			-	071024 yellow dot - meh
			
			https://xivapi.com/docs/Icons?set=icons060000
			-	060408 green pin / dot
			-	(next to it, a bunch of redish aoe circle thingies
			
			https://xivapi.com/docs/Icons?set=icons076000
			-	076171 --bunch of emote faces lmao. 
			-	076402 gpose sticker tjings
			-	076952 cute stickers              
             */

            if (!_config.UseMappy) return;
            try
            {
#if DEBUG
                PluginLog.Error("adding mob marker");
#endif
                RemoveMobMarkers();
                foreach (var (rank, mob) in mobs)
                {
                    var (colour,segments, radius) = GetMobCircleStuff(rank);
                    var posX = MapHelpers.ConvertToMapCoordinate(mob.Position.X, zoneCoordSize);
                    var posY = MapHelpers.ConvertToMapCoordinate(mob.Position.Z, zoneCoordSize);
                    colour = _config.MappyUseCustomColours ? _config.MobColour : colour;
                    AddMobMarkerWithCircle(new Vector2(posX, posY), radius, colour, segments);
                }
            }
            catch (IpcNotReadyError) { }
        }

        private (Vector4, int, float) GetMobCircleStuff(HuntRank rank) => rank switch
        {
            HuntRank.A => (ColourA, 5, 12f),
            HuntRank.B => (ColourA, 20, 8f),
            HuntRank.S => (ColourA, 4, 14f),
            HuntRank.SS=> (ColourA, 3, 16f),            
            _ => throw new NotImplementedException(),
        };

        private Vector4 ColourA = new Vector4(1f, 0.2f, 0.2f, 1f);
        //hard to see...
        private Vector4 ColourB = new Vector4(136 / 255f, 220 / 255f, 221 / 255f, 1f);
        private Vector4 ColourS = new Vector4(1f, 226 / 255f, 0f, 1f);
        private Vector4 ColourSS = new Vector4(1f, 226 / 255f, 0f, 1f);

        #region unused icon stuff

        /// <Exception cref="IpcNotReadyError">If Mappy plugin not installed or not ready</exception>
        public void AddMobMarkerWithIcon(uint IconId, Vector2 MapCoordinates, uint MapId = 0, string Tooltip = "", string Description = "")
        {
            var id = AddWorldMarkerIpcFunction!.InvokeFunc(IconId, MapCoordinates, 0, Tooltip, Description);
            MobMarkers.Add(id);
        }

        private uint GetMobMarkerIcon(HuntRank rank) => rank switch
        {
            HuntRank.A => MobMarkerIDA,
            HuntRank.B => MobMarkerIDB,
            HuntRank.S => MobMarkerIDS,
            HuntRank.SS => MobMarkerIDS,
            _ => throw new NotImplementedException()
        };

        // 060004 lil red fella
        private uint MobMarkerIDA = 060939; // cruise chaser
        private uint MobMarkerIDB = 060940; // oppressor
        private uint MobMarkerIDS = 060941; // brute justice

        //private uint SpawnMarkerID = 015026; //target tag thingy
        //private uint SpawnMarkerID = 060201; // clouds
        //private uint SpawnMarkerID = 060421; // lil blue diamond thing
        private uint SpawnMarkerID = 060624; // cool red diamond thing - 060626
        #endregion


        public void RemoveAll()
        {
            ClearSpawnPointMarkers();
            RemoveMobMarkers();
        }
    }
}
