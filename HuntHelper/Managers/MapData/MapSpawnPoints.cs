using Newtonsoft.Json;
using System.Collections.Generic;
using System.Numerics;

namespace HuntHelper.MapInfoManager;

public class MapSpawnPoints
{
    public string MapName { get; set; }
    public ushort MapID { get; set; }
    public List<Vector2> Positions { get; set; }

    public MapSpawnPoints()
    {
        MapName = string.Empty;
        Positions = new List<Vector2>();
    }

    [JsonConstructor]
    public MapSpawnPoints(string mapName, ushort mapID, List<Vector2> positions)
    {
        MapName = mapName;
        MapID = mapID;
        Positions = positions;
    }
}