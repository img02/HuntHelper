using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Plugin;
using HuntHelper.Managers.MapData.Models;
using Newtonsoft.Json;

namespace HuntHelper.Managers.MapData;

public class MapDataManager
{
    //dict storing map id and corresponding spawn points
    public List<MapSpawnPoints> SpawnPointsList { get; private set; }

    public bool ErrorPopUpVisible = false;
    public string ErrorMessage = string.Empty;

    private readonly DalamudPluginInterface pluginInterface;
    private string filePath;

    public MapDataManager(DalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
        SpawnPointsList = new List<MapSpawnPoints>();
        LoadSpawnPointData();
        filePath = Path.Combine(pluginInterface.AssemblyLocation.Directory?.FullName!, "./Data/SpawnPointData.json");
    }

    public void LoadSpawnPointData()
    {
        ErrorMessage = string.Empty;
       
        if (!File.Exists(filePath))
        {
            ErrorPopUpVisible = true;
            ErrorMessage = "Can't find ./Data/SpawnPointData.json...";
            return;
        }

        var data = JsonConvert.DeserializeObject<List<MapSpawnPoints>>(File.ReadAllText(filePath));
        if (data != null) SpawnPointsList = data;
    }

    public void SaveSpawnPointData()
    {
        var data = JsonConvert.SerializeObject(SpawnPointsList);
        File.WriteAllText(data,filePath);
    }

    //search for relevant map, and return list of spawn points, or if null return blank
    public List<SpawnPointPosition> GetSpawnPoints(ushort mapID)
    {
        return SpawnPointsList.FirstOrDefault(spawnPoints => spawnPoints.MapID == mapID)?.Positions ?? new List<SpawnPointPosition>();
    }

    private bool IsRecording(ushort mapID)
    {
        var msp = SpawnPointsList.FirstOrDefault(msp => msp.MapID == mapID);
        if (msp == null) return false;
        return msp.Recording;
    }
    public override string ToString()
    {
        var text = string.Empty;

        foreach (var map in SpawnPointsList)
        {
            text += $"{map.MapName} - {map.MapID}\n" +
                    $"-------------------\n";
            foreach (var sp in map.Positions)
            {
                text += $"({sp.Position.X}), ({sp.Position.Y})\n";
            }
            text += $"-----------------------------------------\n";
        }
        return text;
    }
}