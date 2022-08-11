using Dalamud.Plugin;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace HuntHelper.MapInfoManager;

public class MapDataManager
{
    //dict storing map id and corresponding spawn points
    public List<MapSpawnPoints> SpawnPointsList { get; private set; }

    public bool ErrorPopUpVisible = false;
    public string ErrorMessage = string.Empty;

    private readonly DalamudPluginInterface pluginInterface;
    private string filePath = "./Data/SpawnPointData.json";

    public MapDataManager(DalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
        SpawnPointsList = new List<MapSpawnPoints>();
        LoadSpawnPointData();
    }

    public void LoadSpawnPointData()
    {
        ErrorMessage = string.Empty;
        var combinedPath = Path.Combine(pluginInterface.AssemblyLocation.Directory?.FullName!, filePath);

        if (!File.Exists(combinedPath))
        {
            ErrorPopUpVisible = true;
            ErrorMessage = "Can't find ./Data/SpawnPointData.json...";
            return;
        }

        var data = JsonConvert.DeserializeObject<List<MapSpawnPoints>>(File.ReadAllText(combinedPath));
        if (data != null) SpawnPointsList = data;
    }

    //search for relevant map, and return list of spawn points, or if null return blank
    public List<Vector2> GetSpawnPoints(ushort mapID)
    {
        return SpawnPointsList.FirstOrDefault(spawnPoints => spawnPoints.MapID == mapID)?.Positions ?? new List<Vector2>();
    }

    public override string ToString()
    {
        var text = string.Empty;

        foreach (var map in SpawnPointsList)
        {
            text += $"{map.MapName} - {map.MapID}\n" +
                    $"-------------------\n";
            foreach (var v2 in map.Positions)
            {
                text += $"({v2.X}), ({v2.Y})\n";
            }
            text += $"-----------------------------------------\n";
        }
        return text;
    }
}