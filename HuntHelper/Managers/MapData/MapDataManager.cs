using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using Dalamud.Logging;
using Dalamud.Plugin;
using HuntHelper.Managers.MapData.Models;
using HuntHelper.Utilities;
using ImGuiNET;
using Newtonsoft.Json;

namespace HuntHelper.Managers.MapData;

public class MapDataManager
{
    //dict storing map id and corresponding spawn points
    public List<MapSpawnPoints> SpawnPointsList { get; private set; }
    public List<MapSpawnPoints> ImportedList { get; private set; }

    public bool ErrorPopUpVisible = false;
    public string ErrorMessage = string.Empty;

    //private readonly DalamudPluginInterface _pluginInterface;
    private readonly string _filePath;

    public MapDataManager(string filePath)
    {
        //this._pluginInterface = pluginInterface;
        _filePath = filePath;
        SpawnPointsList = new List<MapSpawnPoints>();
        ImportedList = new List<MapSpawnPoints>();
        LoadSpawnPointData();
    }

    public void LoadSpawnPointData()
    {
        ErrorMessage = string.Empty;
       
        if (!File.Exists(_filePath))
        {
            ErrorPopUpVisible = true;
            ErrorMessage = $"Can't find {_filePath}";
            return;
        }

        var data = JsonConvert.DeserializeObject<List<MapSpawnPoints>>(File.ReadAllText(_filePath));
        if (data != null) SpawnPointsList = data;
    }

    public void SaveSpawnPointData()
    {
        var data = JsonConvert.SerializeObject(SpawnPointsList, Formatting.Indented);
        File.WriteAllText(_filePath, data);
    }

    //search for relevant map, and return list of spawn points, or if null return blank
    public List<SpawnPointPosition> GetSpawnPoints(ushort mapID)
    {
        return SpawnPointsList.FirstOrDefault(spawnPoints => spawnPoints.MapID == mapID)?.Positions ?? new List<SpawnPointPosition>();
    }

    public bool IsRecording(ushort mapID)
    {
        var msp = SpawnPointsList.FirstOrDefault(msp => msp.MapID == mapID);
        if (msp == null) return false;
        return msp.Recording;
    }

    public void ClearTakenSpawnPoints(ushort mapid)
    {
        var map = SpawnPointsList.FirstOrDefault(msp => msp.MapID == mapid);
        if (map == null) return;
        map.Positions.ForEach(sp => sp.Taken = false);
    }

    public void ClearAllTakenSpawnPoints()
    {
        SpawnPointsList.ForEach(msp =>
        {
            if (msp.Recording)
            {
                msp.Recording = false;
                msp.Positions.ForEach(sp => sp.Taken = false);
            }
        });
    }

    public void Import(string importCode)
    {
        ImportedList.Clear();
        var tempList = ExportImport.Import(importCode, ImportedList);
        if (tempList.Count > 0) ImportedList.AddRange(tempList);
    }

    public void ImportOverwrite()
    {
        foreach (var msp in ImportedList)
        {
            var index = SpawnPointsList.FindIndex(item => item.MapID == msp.MapID);
            SpawnPointsList[index] = msp;
        }
        ImportedList.Clear();
    }

    public void ImportOnlyNew()
    {
        foreach (var msp in ImportedList)
        {
            var index = SpawnPointsList.FindIndex(item => item.MapID == msp.MapID);
            var toUpdate = SpawnPointsList[index].Positions;
            for (int i = 0; i < msp.Positions.Count; i++)
            {   //update new taken positions
                if (msp.Positions[i].Taken) toUpdate[i].Taken = true;
            }
        }
        ImportedList.Clear();
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