using System.Collections.Generic;
using System.Numerics;
using Newtonsoft.Json;

namespace HuntHelper.Managers.Hunts.Models;

public class SirenHuntsMapData
{
    public uint MapID { get; init; }
    public IList<(Vector2, string)> SpawnPoints { get; init; }

    [JsonConstructor]
    public SirenHuntsMapData(uint mapId, IList<(Vector2, string)> spawnPoints)
    {
        MapID = mapId;
        SpawnPoints = spawnPoints;
    }
}