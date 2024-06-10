using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static Dalamud.Interface.Utility.Raii.ImRaii;
using System.Xml.Linq;
using System.Security.Cryptography;
using System.Net.Http;

namespace HuntHelper.Managers.NewExpansion
{
    // actually just realised this might be slightly pointless if plugins aren't available due to new expansion, oops
    // community made hunt data should be avail by end of first/second week, cablemonkey maps end of month? if still active
    
    internal static class SpawnDataGatherer
    {
        //&name= mobid=  &map= &mapid= &rank= &playerid= &x= &y= &z=
        //todo remove url when no longer used
        private static readonly string baseUrl = @"https://idklol-cqej.onrender.com/api/dawntrail?"; // freeeee tier render web service
        private static IList<MobFoundData> history = new List<MobFoundData>();
      
        public static void AddFoundMob(uint mobid, string name, Vector3 position, string rank, uint mapid, string mapName, ulong playerid)
        {

            var currTime = DateTime.UtcNow;

            ClearOldMobs(currTime);
            SubmitToApi(mobid, name, position, mapid, mapName, rank, currTime, playerid);
        }

        private static async void SubmitToApi(uint mobid, string name, Vector3 position, uint mapid, string mapName, string rank, DateTime currTime, ulong playerid)
        {
            if (InRecentHistory(mobid)) return;
            try
            {
                var foundMob = new MobFoundData(mobid, currTime);
                history.Add(foundMob);                               
                var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes($"{playerid}")));      
                // y = z, z = y. but i already swapped them. so y = y, z = z, z conversion is not correct btw
                var url = baseUrl + $"map={mapName}&mapid={mapid}&mobid={mobid}&name={name}&rank={rank}&playerid={hash}&x={position.X}&y={position.Y}&z={position.Z}"; 

                PluginLog.Debug($"Trying {url}");

                using HttpClient client = new HttpClient();
                var res = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
#if DEBUG
                PluginLog.Error($"{res.StatusCode} : {playerid}  : {hash}");

#endif
                if (!res.IsSuccessStatusCode) history.Remove(foundMob);
            }
            catch (Exception ex)
            {
                PluginLog.Error($"${ex.Message}");
                PluginLog.Warning($"Could not submit data: {mobid}:{name}:{position}:{mapid}");
            }
        }

        private static bool InRecentHistory(uint mobid)
        {         
            if (history.Any(m=> m.Id == mobid)) return true;
            return false;

        }

        private static void ClearOldMobs(DateTime timeToCompare)
        {

            var newHistory = new List<MobFoundData>();
            foreach (var item in history)
            {   //if mob less than 30 seconds old, keep in history.
                if (timeToCompare.Subtract(item.Date).TotalMinutes < 0.5) 
                    newHistory.Add(item);
            }
            history = newHistory;
        }

        private class MobFoundData
        {
            public uint Id { get; init; }
            public DateTime Date { get; init; }

            public MobFoundData(uint id, DateTime date)
            {
                Id = id;
                Date = date;
            }
        }
    }


}
