using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface;
using Dalamud.Plugin;
using HuntHelper.Utilities;
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
    private readonly ChatGui _chatGui;

    private readonly List<(HuntRank Rank, BattleNpc Mob)> _currentMobs;
    private readonly List<(HuntRank Rank, BattleNpc Mob)> _previousMobs;

    private BattleNpc? _priorityMob;
    private HuntRank _highestRank;

    public bool ImagesLoaded = false;
    public bool ErrorPopUpVisible = false;
    public string ErrorMessage = string.Empty;

    public SpeechSynthesizer TTS { get; init; } //aint really used anymore except for setting default voice on load
    public string TTSName { get; set; }

    public List<(HuntRank Rank, BattleNpc Mob)> CurrentMobs => _currentMobs;

    public HuntManager(DalamudPluginInterface pluginInterface, ChatGui chatGui)
    {
        _arrDict = new Dictionary<HuntRank, List<Mob>>();
        _hwDict = new Dictionary<HuntRank, List<Mob>>();
        _shbDict = new Dictionary<HuntRank, List<Mob>>();
        _sbDict = new Dictionary<HuntRank, List<Mob>>();
        _ewDict = new Dictionary<HuntRank, List<Mob>>();
        _mapImages = new Dictionary<string, TextureWrap>();
        _currentMobs = new List<(HuntRank, BattleNpc)>();
        _previousMobs = new List<(HuntRank, BattleNpc)>();
        _pluginInterface = pluginInterface;
        _chatGui = chatGui;

        _imageFolderPath = Path.Combine(_pluginInterface.AssemblyLocation.Directory?.FullName!, "Images/Maps");
        TTS = new SpeechSynthesizer();
        TTSName = TTS.Voice.Name;
        LoadHuntData();
    }

    public (HuntRank Rank, BattleNpc? Mob) GetPriorityMob()
    {
        if (_priorityMob == null) return (_highestRank, null);
        if (_currentMobs.Count == 0) return (_highestRank, null);
        if (!IsHunt(_priorityMob.NameId))
        {
            _highestRank = HuntRank.B;
            foreach (var hunt in _currentMobs) PriorityCheck(hunt.Mob);
        }
        return (_highestRank, _priorityMob);
    }
    public List<(HuntRank, BattleNpc)> GetAllCurrentMobsWithRank()
    {
        return _currentMobs;
    }

    //bit much, grew big because I don't plan
    public void AddNearbyMobs(List<BattleNpc> nearbyMobs, float zoneMapCoordSize,
        bool aTTS, bool bTTS, bool sTTS, string aTTSmsg, string bTTSmsg, string sTTSmsg,
        bool chatA, bool chatB, bool chatS, string chatAmsg, string chatBmsg, string chatSmsg, string placeName)
    {
        //compare with old list
        //move old mob set out
        _previousMobs.Clear();
        _previousMobs.AddRange(_currentMobs);
        _currentMobs.Clear();

        foreach (var mob in nearbyMobs)
        {   //add in new mobs to current list. 
            _currentMobs.Add((GetHuntRank(mob.NameId), mob));

            PriorityCheck(mob);

            //if exists in old mob set, skip tts + chat
            if (_previousMobs.Any(hunt => hunt.Mob.NameId == mob.NameId)) continue;

            //Do tts and chat stuff
            switch (GetHuntRank(mob.NameId))
            {
                case HuntRank.A:
                    NewMobFoundTTS(aTTS, aTTSmsg, mob);
                    SendChatMessage(chatA, chatAmsg, placeName, mob, zoneMapCoordSize);
                    break;
                case HuntRank.B:
                    NewMobFoundTTS(bTTS, bTTSmsg, mob);
                    SendChatMessage(chatB, chatBmsg, placeName, mob, zoneMapCoordSize);
                    break;
                case HuntRank.S:
                case HuntRank.SS:
                    NewMobFoundTTS(sTTS, sTTSmsg, mob);
                    SendChatMessage(chatS, chatSmsg, placeName, mob, zoneMapCoordSize);
                    break;
            }
        }
    }

    #region rework later?

    private void SendChatMessage(bool enabled, string msg, string placeName, BattleNpc mob, float zoneCoordSize)
    {
        if (!enabled) return;

        var rank = GetHuntRank(mob.NameId);
        var hpp = GetHPP(mob);

        //pattern for matching, (?i) = case insensitive
        string pattern = "(?i)(<flag>|<rank>|<name>|<hpp>)";
        //splits the string based on above 
        var splitMsg = Regex.Split(msg, pattern);

        var sb = new SeStringBuilder();
        foreach (var s in splitMsg)
        {
            switch (s)
            {
                case "<flag>": //Why doesn't SeStringBuilder.AddMapLink have an overload that takes in placename, while SeString.CreateMapLink does? :( cause null possible?
                    var maplink = SeString.CreateMapLink(placeName, MapHelpers.ConvertToMapCoordinate(mob.Position.X, zoneCoordSize),
                        MapHelpers.ConvertToMapCoordinate(mob.Position.Z, zoneCoordSize));
                    sb.AddUiForeground(64); //white
                    sb.Append(maplink ??
                              new SeString(new TextPayload(
                                      $"|Error: couldn't create map link for: {placeName} - Please report what zone this occurred in.|"))
                                  .Append(new IconPayload(BitmapFontIcon.NoCircle)));
                    sb.AddUiForegroundOff();
                    break;
                case "<rank>":
                    //idk just test random numbers lmao
                    if (rank == HuntRank.A) sb.AddUiForeground("A-Rank", 12); //red / pinkish
                    if (rank == HuntRank.B) sb.AddUiForeground("B-Rank", 34); //blue
                    if (rank == HuntRank.S) sb.AddUiForeground("S-Rank", 506); //gold 
                    break;
                case "<name>":
                    if (rank == HuntRank.A) sb.AddUiForeground($"{mob.Name}", 12); //red / pinkish
                    if (rank == HuntRank.B) sb.AddUiForeground($"{mob.Name}", 34); //blue
                    if (rank == HuntRank.S) sb.AddUiForeground($"{mob.Name}", 506); //gold 
                    break;
                case "<hpp>": //change colour based on initial hp? meh
                    if (Math.Abs(hpp - 100) < 1) sb.AddUiForeground($"{hpp:0}%", 67); //green
                    if (Math.Abs(hpp - 100) is <= 30 and >= 1) sb.AddUiForeground($"{hpp:0}%", 573); //yellow
                    if (Math.Abs(hpp - 100) is > 30) sb.AddUiForeground($"{hpp:0}%", 531); //red
                    break;
                default:
                    sb.AddText(s);
                    break;
            }
        }
        _chatGui.Print(sb.BuiltString);
    }

    private void NewMobFoundTTS(bool enabled, string msg, BattleNpc mob)
    {
        if (!enabled) return;
        var message = FormatMessageFlags(msg, mob);
        //changed to creating a new tts each time because SpeakAsync just queues up to play...
        var tts = new SpeechSynthesizer();
        tts.SelectVoice(TTSName);
        tts.SpeakAsync(message);
    }

    private string FormatMessageFlags(string msg, BattleNpc mob)
    {
        msg = msg.Replace("<rank>", $"{GetHuntRank(mob.NameId)}-Rank", true, CultureInfo.InvariantCulture);
        msg = msg.Replace("<name>", $"{mob.Name}", true, CultureInfo.InvariantCulture);
        msg = msg.Replace("<hpp>", $"{GetHPP(mob):0}", true, CultureInfo.InvariantCulture);
        return msg;
    }

    #endregion

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

        if (!Directory.Exists(_imageFolderPath))
        {
            //if dir doesn't exist, try downloading images
            //then if still doesn't exist, return false
            //DownloadMapImages();
            return false;
        }

        //change this later for ss folder? or just draw ss on screen - have to update spawn point drawing and jsons
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

    public double GetHPP(BattleNpc mob)
    {
        return Math.Round(((1.0 * mob.CurrentHp) / mob.MaxHp) * 100, 2);
    }

    private void PriorityCheck(BattleNpc mob)
    {
        var rank = GetHuntRank(mob.NameId);
        if (rank >= _highestRank)
        {
            _highestRank = rank;
            _priorityMob = mob;
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