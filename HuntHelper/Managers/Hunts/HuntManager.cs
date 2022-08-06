using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Speech.Synthesis;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using Dalamud.Plugin;
using ImGuiNET;
using ImGuiScene;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

namespace HuntHelper.Managers.Hunts;

public class HuntManager
{
    private readonly string _imageFolderPath;

    private readonly Dictionary<HuntRank, List<Mob>> _arrDict;
    private readonly Dictionary<HuntRank, List<Mob>> _hwDict;
    private readonly Dictionary<HuntRank, List<Mob>> _shbDict;
    private readonly Dictionary<HuntRank, List<Mob>> _sbDict;
    private readonly Dictionary<HuntRank, List<Mob>> _ewDict;
    private readonly Dictionary<String, TextureWrap> _mapImages;
    private readonly DalamudPluginInterface _pluginInterface;

    private readonly List<(HuntRank Rank, BattleNpc Mob)> _currentMobs;
    private List<(HuntRank Rank, BattleNpc Mob)> _previousMobs1;
    private readonly List<uint> _previousMobs;

    private BattleNpc? _priorityMob;
    private HuntRank _highestRank;

    public bool ImagesLoaded = false;
    public bool ErrorPopUpVisible = false;
    public string ErrorMessage = string.Empty;

    public SpeechSynthesizer TTS { get; init; }

    public List<(HuntRank Rank, BattleNpc Mob)> CurrentMobs => _currentMobs;

    public HuntManager(DalamudPluginInterface pluginInterface)
    {
        _arrDict = new Dictionary<HuntRank, List<Mob>>();
        _hwDict = new Dictionary<HuntRank, List<Mob>>();
        _shbDict = new Dictionary<HuntRank, List<Mob>>();
        _sbDict = new Dictionary<HuntRank, List<Mob>>();
        _ewDict = new Dictionary<HuntRank, List<Mob>>();
        _mapImages = new Dictionary<string, TextureWrap>();
        _currentMobs = new List<(HuntRank, BattleNpc)>();
        _previousMobs1 = new List<(HuntRank, BattleNpc)>();
        _previousMobs = new List<uint>();
        this._pluginInterface = pluginInterface;
        _imageFolderPath = Path.Combine(_pluginInterface.AssemblyLocation.Directory?.FullName!, "Images/Maps");
        TTS = new SpeechSynthesizer();
        LoadHuntData();
    }

    public (HuntRank Rank, BattleNpc? Mob) GetPriorityMob()
    {
        return (_highestRank, _priorityMob);
    }

    public List<(HuntRank, BattleNpc)> GetAllCurrentMobsWithRank()
    {
        return _currentMobs;
    }
    
    public void AddNearbyMobs(List<BattleNpc> nearbyMobs, bool a, bool b, bool s, string aMsg, string bMsg, string sMsg)
    {
        //compare with old list,
        _previousMobs1.Clear();
        _previousMobs1.AddRange(_currentMobs);
        _currentMobs.Clear();

        foreach (var mob in nearbyMobs)
        {
            _currentMobs.Add((GetHuntRank(mob.NameId), mob));
            var rank = GetHuntRank(mob.NameId);
            if (rank >= _highestRank)
            {
                _highestRank = rank;
                _priorityMob = mob;
            }
            //if already exists, skip tts
            if (_previousMobs1.Any(hunt => hunt.Mob.NameId == mob.NameId)) continue;
            //Do tts stuff
            switch (GetHuntRank(mob.NameId))
            {
                case HuntRank.A:
                    NewMobFoundTTS(GetHuntRank(mob.NameId), mob, a, aMsg);
                    break;
                case HuntRank.B:
                    NewMobFoundTTS(GetHuntRank(mob.NameId), mob, b, bMsg);
                    break;
                case HuntRank.S:
                case HuntRank.SS:
                    NewMobFoundTTS(GetHuntRank(mob.NameId), mob, s, sMsg);
                    break;
            }
        }
    }

    private void NewMobFoundTTS(HuntRank rank, BattleNpc mob, bool enabled, string msg)
    {
        msg = msg.Replace("<rank>", $"{rank}-Rank", true, CultureInfo.InvariantCulture);
        msg = msg.Replace("<name>", $"{mob.Name}", true, CultureInfo.InvariantCulture);
        TTS.SpeakAsync(msg);
    }

    public List<BattleNpc> GetCurrentMobs()
    {
        return _currentMobs.Select(hunt => hunt.Mob).ToList();
    }

    public void LoadHuntData()
    {
        var ARRJsonFiles = new List<string>
        {
            "./data/ARR-A.json",
            "./data/ARR-B.json",
            "./data/ARR-S.json",
        };

        var HWJsonFiles = new List<string>
        {
            "./data/HW-A.json",
            "./data/HW-B.json",
            "./data/HW-S.json",
        };
        var SBJsonFiles = new List<string>
        {
            "./data/SB-A.json",
            "./data/SB-B.json",
            "./data/SB-S.json",
        };
        var ShBJsonFiles = new List<string>
        {
            "./data/ShB-A.json",
            "./data/ShB-B.json",
            "./data/ShB-S.json",
        };
        var EWJsonFiles = new List<string>
        {
            "./data/EW-A.json",
            "./data/EW-B.json",
            "./data/EW-S.json"
        };

        //messy... prob cleaner way
        LoadFilesIntoDic(_arrDict, ARRJsonFiles);
        LoadFilesIntoDic(_hwDict, HWJsonFiles);
        LoadFilesIntoDic(_sbDict, SBJsonFiles);
        LoadFilesIntoDic(_shbDict, ShBJsonFiles);
        LoadFilesIntoDic(_ewDict, EWJsonFiles);

    }

    public void SaveHuntData()
    {

    }

    public float GetMapZoneCoordSize(ushort mapID)
    {
        //EVERYTHING EXCEPT HEAVENSWARD HAS 41 COORDS, BUT FOR SOME REASON HW HAS 43, WHYYYYYY
        if (mapID is >= 397 and <= 402) return 43.1f;
        return 41f;
    }
    //override tostring?
    public string GetDatabaseAsString()
    {
        var text = string.Format("{0,-4} | {1,-26} | {2,-5} | {3,5}\n" +
                   "--------------------------------------------------\n", "Rank", "Name", " ID", "Enabled");
        text += DictToString(_arrDict);
        text += DictToString(_hwDict);
        text += DictToString(_sbDict);
        text += DictToString(_shbDict);
        text += DictToString(_ewDict);
        return text;
    }
    public bool IsHunt(uint modelID)
    {
        var exists = false;
        exists = _arrDict.Any(kvp => kvp.Value.Any(m => m.ModelID == modelID));
        if (!exists) exists = _hwDict.Any(kvp => kvp.Value.Any(m => m.ModelID == modelID));
        if (!exists) exists = _sbDict.Any(kvp => kvp.Value.Any(m => m.ModelID == modelID));
        if (!exists) exists = _shbDict.Any(kvp => kvp.Value.Any(m => m.ModelID == modelID));
        if (!exists) exists = _ewDict.Any(kvp => kvp.Value.Any(m => m.ModelID == modelID));
        return exists;
    }

    public bool LoadMapImages()
    {
        if (ImagesLoaded) return false;

        //DownloadMapImages();
        if (!Directory.Exists(_imageFolderPath)) return false;

        //change this later for ss folder
        var paths = Directory.EnumerateFiles(_imageFolderPath, "*", SearchOption.TopDirectoryOnly);

        foreach (var path in paths)
        {
            _mapImages.Add(GetMapNameFromPath(path), _pluginInterface.UiBuilder.LoadImage(path));
        }

        ImagesLoaded = true;
        return true;
    }

    public TextureWrap? GetMapImage(string mapName)
    {
        if (!_mapImages.ContainsKey(mapName)) return null;
        return _mapImages[mapName];
    }

    public void Dispose()
    {
        foreach (var kvp in _mapImages)
        {
            kvp.Value.Dispose();
        }
    }

    public bool IsMobInCurrentMobList(uint mobID)
    {
        return _currentMobs.Any(hunt => hunt.Mob.NameId == mobID);
    }

    public void RemoveFromCurrentMobsList(List<uint> removalList)
    {
        foreach (var mobID in removalList)
        {
            var toRemove = _currentMobs.First(hunt => hunt.Mob.NameId == mobID);
            _currentMobs.Remove(toRemove);
        }
    }

    private string GetMapNameFromPath(string path)
    { //all files end with '-data.jpg', img source - http://cablemonkey.us/huntmap2/
        var pathRemoved = path.Remove(0, _imageFolderPath.Length + 1).Replace("_", " ");
        return pathRemoved.Remove(pathRemoved.Length - 9);
    }

    private HuntRank GetHuntRank(uint modelID)
    {
        //just default to B if for some reason mob can't be found - shouldn't happen tho...
        var rank = HuntRank.B;
        var found = false;
        if (!found)
        {   //ugly and repetitive
            var kvp = SearchDictionaryForModelID(_arrDict, modelID);
            if (!kvp.Equals(default(KeyValuePair<HuntRank, List<Mob>>))) return kvp.Key;
            kvp = SearchDictionaryForModelID(_hwDict, modelID);
            if (!kvp.Equals(default(KeyValuePair<HuntRank, List<Mob>>))) return kvp.Key;
            kvp = SearchDictionaryForModelID(_sbDict, modelID);
            if (!kvp.Equals(default(KeyValuePair<HuntRank, List<Mob>>))) return kvp.Key;
            kvp = SearchDictionaryForModelID(_shbDict, modelID);
            if (!kvp.Equals(default(KeyValuePair<HuntRank, List<Mob>>))) return kvp.Key;
            kvp = SearchDictionaryForModelID(_ewDict, modelID);
            if (!kvp.Equals(default(KeyValuePair<HuntRank, List<Mob>>))) return kvp.Key;
        }
        return rank;
    }

    private KeyValuePair<HuntRank, List<Mob>> SearchDictionaryForModelID(Dictionary<HuntRank, List<Mob>> dict,
        uint modelID)
    {
        return dict.FirstOrDefault(kvp => kvp.Value.Any(m => m.ModelID == modelID));
    }

    private string DictToString(Dictionary<HuntRank, List<Mob>> dic)
    {
        var text = string.Empty;

        foreach (var kvp in dic)
        {
            foreach (var mob in kvp.Value)
            {
                text += string.Format($"{mob.Rank,-4} | {mob.Name,-26} | {mob.ModelID,5} | {mob.IsEnabled,5}\n");
            }

            text += "\n";
        }

        return text += "\n--------------------------------------------------\n";
    }

    private void LoadFilesIntoDic(Dictionary<HuntRank, List<Mob>> dict, List<string> filePaths)
    {
        foreach (var path in filePaths)
        {
            if (!File.Exists(Path.Combine(_pluginInterface.AssemblyLocation.Directory?.FullName!, path)))
            {
                ErrorPopUpVisible = true;
                ErrorMessage += $"File {path} missing... Please replace missing file(s).\n";
                return;
            }
        }
        var A = JsonConvert.DeserializeObject<List<Mob>>(File.ReadAllText(Path.Combine(_pluginInterface.AssemblyLocation.Directory?.FullName!, filePaths[0])));
        var B = JsonConvert.DeserializeObject<List<Mob>>(File.ReadAllText(Path.Combine(_pluginInterface.AssemblyLocation.Directory?.FullName!, filePaths[1])));
        var S = JsonConvert.DeserializeObject<List<Mob>>(File.ReadAllText(Path.Combine(_pluginInterface.AssemblyLocation.Directory?.FullName!, filePaths[2])));

        if (A != null) dict.Add(HuntRank.A, A);
        if (B != null) dict.Add(HuntRank.B, B);
        if (S != null) dict.Add(HuntRank.S, S);
    }









}