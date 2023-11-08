using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Ipc.Exceptions;
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
        //public ICallGateSubscriber<Vector2, Vector2, uint, Vector4, float, string>? AddTextureLineIpcFunction = null;
        //public ICallGateSubscriber<Vector2, Vector2, uint, Vector4, float, string>? AddMapCoordLineIpcFunction = null;
        private readonly ICallGateSubscriber<string, bool>? RemoveLineIpcFunction = null;
        private readonly ICallGateSubscriber<bool>? IsReadyIpcFunction = null;

        private DalamudPluginInterface _pluginInterface;


        public MappyIPC(DalamudPluginInterface pluginInterface)
        {
            _pluginInterface = pluginInterface;

            // Copy/Paste this to subscribe to these functions, be sure to check for IPCNotReady exceptions ;)
            AddWorldMarkerIpcFunction = _pluginInterface.GetIpcSubscriber<uint, Vector2, uint, string, string, string>("Mappy.World.AddMarker");
            //AddTextureMarkerIpcFunction = Service.PluginInterface.GetIpcSubscriber<uint, Vector2, uint, string, string, string>("Mappy.Texture.AddMarker");
            AddMapCoordinateMarkerIpcFunction = _pluginInterface.GetIpcSubscriber<uint, Vector2, uint, string, string, string>("Mappy.MapCoord.AddMarker");
            RemoveMarkerIpcFunction = _pluginInterface.GetIpcSubscriber<string, bool>("Mappy.RemoveMarker");
            //UpdateMarkerIpcFunction = Service.PluginInterface.GetIpcSubscriber<string, Vector2, bool>("Mappy.UpdateMarker");
            //AddTextureLineIpcFunction = Service.PluginInterface.GetIpcSubscriber<Vector2, Vector2, uint, Vector4, float, string>("Mappy.Texture.AddLine");
            //AddMapCoordLineIpcFunction = Service.PluginInterface.GetIpcSubscriber<Vector2, Vector2, uint, Vector4, float, string>("Mappy.MapCoord.AddLine");
            RemoveLineIpcFunction = _pluginInterface.GetIpcSubscriber<string, bool>("Mappy.RemoveLine");
            IsReadyIpcFunction = _pluginInterface.GetIpcSubscriber<bool>("Mappy.IsReady");
        }



        /// <Exception cref="IpcNotReadyError">If Mappy plugin not installed or not ready</exception>
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

        public bool HasSpawnPoints() => SpawnMarkers.Count > 0;


        /// <Exception cref="IpcNotReadyError">If Mappy plugin not installed or not ready</exception>
        public bool AddSpawnPointMarker(Vector2 coordinates)
        {
            if (!IsReady())
            {
                //PluginLog.Verbose("Could not add marker, Mappy IPC not ready.");
                return false;
            }
            /*
             

            https://xivapi.com/docs/Icons
            https://xivapi.com/docs/Icons?set=fates
            https://xivapi.com/docs/Icons?set=icons016000
             
             
             */
            var id = AddMapCoordinateMarkerIpcFunction!.InvokeFunc(SpawnMarkerID, coordinates, 0, "", "");
            SpawnMarkers.Add(id);
            //PluginLog.Log("added");
            return true;
        }

        /// <Exception cref="IpcNotReadyError">If Mappy plugin not installed or not ready</exception>
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
        public void AddMobMarker(uint IconId, Vector2 MapCoordinates, uint MapId = 0, string Tooltip = "", string Description = "")
        {
            var id = AddWorldMarkerIpcFunction!.InvokeFunc(IconId, MapCoordinates, 0, Tooltip, Description);
            MobMarkers.Add(id);
        }

        /// <summary>
        /// Why did I call this 'RemoveMobMakers' but 'ClearSpawnPointMarkers'? i do not know.
        /// </summary
        /// <exception cref="IpcNotReadyError">If Mappy plugin not installed or not ready</exception>
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

        public void OnCurrentMobChange(IList<(HuntRank, BattleNpc)> mobs)
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

            try
            {
#if DEBUG
                PluginLog.Error("adding mob marker");
#endif
                RemoveMobMarkers();
                foreach (var (rank, mob) in mobs)
                {
                    var marker = GetMobMarkerID(rank);
                    AddMobMarker(marker, new Vector2(mob.Position.X, mob.Position.Z), 0, $"{mob.Name}  |  {rank}\t\t");
                }
            }
            catch (IpcNotReadyError) { }
        }

        private uint GetMobMarkerID(HuntRank rank) => rank switch
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


    }
}
