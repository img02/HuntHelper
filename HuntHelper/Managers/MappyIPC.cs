using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace HuntHelper.Managers
{
    public class MappyIPC
    {
        private List<string> SpawnMarkers = new();
        private List<string> MobMarkers = new();


        //https://github.com/MidoriKami/Mappy/blob/master/Mappy/Controllers/IpcController.cs#L66

        //public ICallGateSubscriber<uint, Vector2, uint, string, string, string>? AddWorldMarkerIpcFunction = null;
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
            //AddWorldMarkerIpcFunction = Service.PluginInterface.GetIpcSubscriber<uint, Vector2, uint, string, string, string>("Mappy.World.AddMarker");
            //AddTextureMarkerIpcFunction = Service.PluginInterface.GetIpcSubscriber<uint, Vector2, uint, string, string, string>("Mappy.Texture.AddMarker");
            AddMapCoordinateMarkerIpcFunction = _pluginInterface.GetIpcSubscriber<uint, Vector2, uint, string, string, string>("Mappy.MapCoord.AddMarker");
            RemoveMarkerIpcFunction = _pluginInterface.GetIpcSubscriber<string, bool>("Mappy.RemoveMarker");
            //UpdateMarkerIpcFunction = Service.PluginInterface.GetIpcSubscriber<string, Vector2, bool>("Mappy.UpdateMarker");
            //AddTextureLineIpcFunction = Service.PluginInterface.GetIpcSubscriber<Vector2, Vector2, uint, Vector4, float, string>("Mappy.Texture.AddLine");
            //AddMapCoordLineIpcFunction = Service.PluginInterface.GetIpcSubscriber<Vector2, Vector2, uint, Vector4, float, string>("Mappy.MapCoord.AddLine");
            RemoveLineIpcFunction = _pluginInterface.GetIpcSubscriber<string, bool>("Mappy.RemoveLine");
            IsReadyIpcFunction = _pluginInterface.GetIpcSubscriber<bool>("Mappy.IsReady");
        }

        public bool HasSpawnPoints() => SpawnMarkers.Count > 0;

        public bool AddSpawnPointMarker(Vector2 coordinates)
        {
            //uint IconId, Vector2 MapCoordinates, uint MapId, string Tooltip, string Description
            if (!IsReadyIpcFunction!.InvokeFunc())
            {
                return false;
                PluginLog.Log("mappy not ready");
            }
            /*
             
            https://xivapi.com/docs/Icons
            https://xivapi.com/docs/Icons?set=fates
            https://xivapi.com/docs/Icons?set=icons016000
             
             
             */
            AddMapCoordinateMarkerIpcFunction!.InvokeFunc(015026, coordinates,  0,"","" );
            PluginLog.Log("added");
            return true;
        }

        public bool ClearSpawnPointMarkers()
        {
            SpawnMarkers.Clear();
            return false;
        }

        public bool AddMobMarker(uint IconId, Vector2 MapCoordinates, uint MapId, string Tooltip, string Description)
        {
            return false;
        }

        public bool RemoveMobMarkers()
        {
            return false; 
        }
    }
}
