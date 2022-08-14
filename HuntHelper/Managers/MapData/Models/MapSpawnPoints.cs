using Newtonsoft.Json;
using System.Collections.Generic;
using System.Numerics;

namespace HuntHelper.Managers.MapData.Models;

public class MapSpawnPoints
{
    public string MapName { get; set; }
    public ushort MapID { get; set; }
    //public List<Vector2> Positions { get; set; }
    public List<SpawnPointPosition> Positions { get; set; }
    public bool Recording { get; set; }

    public MapSpawnPoints()
    {
        MapName = string.Empty;
        Positions = new List<SpawnPointPosition>();
    }

    [JsonConstructor]
    public MapSpawnPoints(string mapName, ushort mapID, List<SpawnPointPosition> positions, bool recording)
    {
        MapName = mapName;
        MapID = mapID;
        Positions = positions;
        Recording = recording;
    }
}