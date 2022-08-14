using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.FlyText;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Logging;
using Dalamud.Plugin;
using HuntHelper.Managers.Hunts.Models;
using HuntHelper.Managers.MapData.Models;
using HuntHelper.Utilities;
using ImGuiScene;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HuntHelper.Managers.Hunts;

public class HuntManager
{
    public readonly string ImageFolderPath;

    private readonly Dictionary<HuntRank, List<Mob>> _arrDict;
    private readonly Dictionary<HuntRank, List<Mob>> _hwDict;
    private readonly Dictionary<HuntRank, List<Mob>> _shbDict;
    private readonly Dictionary<HuntRank, List<Mob>> _sbDict;
    private readonly Dictionary<HuntRank, List<Mob>> _ewDict;
    private readonly Dictionary<String, TextureWrap> _mapImages;
    private readonly DalamudPluginInterface _pluginInterface;
    private readonly ChatGui _chatGui;
    private readonly FlyTextGui _flyTextGui;
    private readonly TrainManager _trainManager;

    private readonly List<(HuntRank Rank, BattleNpc Mob)> _currentMobs;
    private readonly List<(HuntRank Rank, BattleNpc Mob)> _previousMobs;

    private BattleNpc? _priorityMob;
    private HuntRank _highestRank;

    public bool ImagesLoaded { get; private set; } = false;
    public bool ErrorPopUpVisible = false;
    public string ErrorMessage = string.Empty;

    public SpeechSynthesizer TTS { get; init; } //aint really used anymore except for setting default voice on load
    public string TTSName { get; set; }

    #region chat/flytext colours - make customizable later? prob not.
    private readonly ushort _aTextColour = 12;
    private readonly ushort _bTextColour = 34;
    private readonly ushort _sTextColour = 506;
    private readonly ushort _aFlyTextColour = 10;
    private readonly ushort _bFlyTextColour = 33;
    private readonly ushort _sFlyTextColour = 16;
    #endregion

    public List<HuntTrainMob> HuntTrain { get; init; }
    public List<HuntTrainMob> ImportedTrain { get; init; }

    public List<(HuntRank Rank, BattleNpc Mob)> CurrentMobs => _currentMobs;

    public HuntManager(DalamudPluginInterface pluginInterface, TrainManager trainManager, ChatGui chatGui, FlyTextGui flyTextGui)
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
        _flyTextGui = flyTextGui;
        _trainManager = trainManager;

        HuntTrain = new List<HuntTrainMob>();
        ImportedTrain = new List<HuntTrainMob>();

        ImageFolderPath = Path.Combine(_pluginInterface.AssemblyLocation.Directory?.FullName!, @"Images\Maps\");
        TTS = new SpeechSynthesizer();
        TTSName = TTS.Voice.Name;

        LoadHuntData();
        //LoadHuntTrainRecord();
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

    public void AddToTrain(BattleNpc mob, uint territoryid, uint mapid, string mapName, float zoneMapCoordSize)
    {
        if (!_trainManager.RecordTrain) return;
        //only record A ranks
        if (GetHuntRank(mob.NameId) != HuntRank.A) return;
        //skip if already recorded, ideally ID would be safer. 
        _trainManager.AddMob(mob, territoryid, mapid, mapName, zoneMapCoordSize);
    }

    //bit much, grew big because I don't plan
    public void AddNearbyMobs(List<BattleNpc> nearbyMobs, float zoneMapCoordSize, uint territoryId, uint mapid,
        bool aTTS, bool bTTS, bool sTTS, string aTTSmsg, string bTTSmsg, string sTTSmsg,
        bool chatA, bool chatB, bool chatS, string chatAmsg, string chatBmsg, string chatSmsg,
        bool flyTxtA, bool flyTxtB, bool flyTxtS)
    {
        //compare with old list
        //move old mob set out
        _previousMobs.Clear();
        _previousMobs.AddRange(_currentMobs);
        _currentMobs.Clear();
        ResetPriorityMob();

        foreach (var mob in nearbyMobs)
        {   //add in new mobs to current list. 
            _currentMobs.Add((GetHuntRank(mob.NameId), mob));

            PriorityCheck(mob);

            //if exists in old mob set, skip tts + chat
            if (_previousMobs.Any(hunt => hunt.Mob.NameId == mob.NameId)) continue;

            //Do tts and chat stuff
            var rank = GetHuntRank(mob.NameId);
            switch (rank)
            {
                case HuntRank.A:
                    NewMobFoundTTS(aTTS, aTTSmsg, mob);
                    SendChatMessage(chatA, chatAmsg, territoryId, mapid, mob, zoneMapCoordSize);
                    SendFlyText(rank, mob, flyTxtA);
                    break;
                case HuntRank.B:
                    NewMobFoundTTS(bTTS, bTTSmsg, mob);
                    SendChatMessage(chatB, chatBmsg, territoryId, mapid, mob, zoneMapCoordSize);
                    SendFlyText(rank, mob, flyTxtB);
                    break;
                case HuntRank.S:
                case HuntRank.SS:
                    NewMobFoundTTS(sTTS, sTTSmsg, mob);
                    SendChatMessage(chatS, chatSmsg, territoryId, mapid, mob, zoneMapCoordSize);
                    SendFlyText(rank, mob, flyTxtS);
                    break;
            }
        }
    }

    #region rework later?

    //sent fly text in-game on the player  -- move these sestring colours from here and chatmsg to consts or something
    private void SendFlyText(HuntRank rank, BattleNpc mob, bool enabled)
    {
        if (!enabled) return;
        var rankSB = new SeStringBuilder();
        var nameSB = new SeStringBuilder();
        switch (rank)
        {
            case HuntRank.A:            //didn't hyphen 'rank' here.. think it looks better
                rankSB.AddUiForeground("A RANK", _aTextColour); // pinkish-red - same as chat msg
                nameSB.AddUiForeground($"{mob.Name}", _aFlyTextColour); //tinted pinkish-red
                _flyTextGui.AddFlyText(FlyTextKind.NamedDirectHit, 1, 1, 1, rankSB.BuiltString, nameSB.BuiltString, 16, 2);//last 2 nums don't seem to change anything
                break;
            case HuntRank.B:
                rankSB.AddUiForeground("B RANK", _bTextColour); // blue - same as chat msg
                nameSB.AddUiForeground($"{mob.Name}", _bFlyTextColour); //tinted blue
                _flyTextGui.AddFlyText(FlyTextKind.NamedDirectHit, 1, 1, 1, rankSB.BuiltString, nameSB.BuiltString, 16, 2);
                break;
            case HuntRank.S:
            case HuntRank.SS:
                rankSB.AddUiForeground("S RANK", _sFlyTextColour); // dark-red - different from chat msg (goldish) because it stands out more.
                nameSB.AddUiForeground($"{mob.Name}", _sTextColour); //same gold as chat msg
                _flyTextGui.AddFlyText(FlyTextKind.NamedDirectHit, 1, 1, 1, rankSB.BuiltString, nameSB.BuiltString, 16, 2);
                break;
        }
    }

    public void SendChatMessage(bool enabled, string msg, uint territoryId, uint mapid, BattleNpc mob, float zoneCoordSize)
    {
        if (!enabled) return;

        var rank = GetHuntRank(mob.NameId);
        var hpp = GetHPP(mob);

        //pattern for matching, (?i) = case insensitive
        string pattern = "(?i)(<flag>|<rank>|<name>|<hpp>" +
                         "|<goldstar>|<silverstar>|<warning>|<nocircle>" +
                         "|<controllerbutton0>|<controllerbutton1>" +
                         "|<priorityworld>|<elementallevel>" +
                         "|<exclamationrectangle>|<notoriousmonster>" +
                         "|<alarm>|<fanfestival>)";
        //splits the string based on above 
        var splitMsg = Regex.Split(msg, pattern);

        var sb = new SeStringBuilder();
        foreach (var s in splitMsg)
        {
            switch (s)
            {
                case "<flag>": //Why doesn't SeStringBuilder.AddMapLink have an overload that takes in placename, while SeString.CreateMapLink does? :( cause null possible?
                    var maplink = SeString.CreateMapLink(territoryId, mapid, MapHelpers.ConvertToMapCoordinate(mob.Position.X, zoneCoordSize),
                        MapHelpers.ConvertToMapCoordinate(mob.Position.Z, zoneCoordSize));
                    sb.AddUiForeground(64); //white
                    sb.Append(maplink);
                    sb.AddUiForegroundOff();
                    break;
                case "<rank>":
                    //idk just test random numbers lmao
                    if (rank == HuntRank.A) sb.AddUiForeground("A-Rank", _aTextColour); //red / pinkish
                    if (rank == HuntRank.B) sb.AddUiForeground("B-Rank", _bTextColour); //blue
                    if (rank == HuntRank.S) sb.AddUiForeground("S-Rank", _sTextColour); //gold 
                    break;
                case "<name>":
                    if (rank == HuntRank.A) sb.AddUiForeground($"{mob.Name}", _aTextColour); //red / pinkish
                    if (rank == HuntRank.B) sb.AddUiForeground($"{mob.Name}", _bTextColour); //blue
                    if (rank == HuntRank.S) sb.AddUiForeground($"{mob.Name}", _sTextColour); //gold 
                    break;
                case "<hpp>": //change colour based on initial hp? meh
                    if (Math.Abs(hpp - 100) < 1) sb.AddUiForeground($"{hpp:0}%", 67); //green - ~100% hp
                    if (Math.Abs(hpp - 100) is <= 30 and >= 1) sb.AddUiForeground($"{hpp:0}%", 573); //yellow 70+%
                    if (Math.Abs(hpp - 100) is > 30) sb.AddUiForeground($"{hpp:0}%", 531); //red - below 70%hp
                    break;
                case "<goldstar>":
                    sb.AddIcon(BitmapFontIcon.GoldStar); //think i went a bit overboard lmao
                    break;
                case "<silverstar>":
                    sb.AddIcon(BitmapFontIcon.SilverStar);
                    break;
                case "<warning>":
                    sb.AddIcon(BitmapFontIcon.Warning);
                    break;
                case "<nocircle>":
                    sb.AddIcon(BitmapFontIcon.NoCircle);
                    break;
                case "<controllerbutton0>":
                    sb.AddIcon(BitmapFontIcon.ControllerButton0);
                    break;
                case "<controllerbutton1>":
                    sb.AddIcon(BitmapFontIcon.ControllerButton1);
                    break;
                case "<priorityworld>":
                    sb.AddIcon(BitmapFontIcon.PriorityWorld);
                    break;
                case "<elementallevel>":
                    sb.AddIcon(BitmapFontIcon.ElementalLevel);
                    break;
                case "<exclamationrectangle>":
                    sb.AddIcon(BitmapFontIcon.ExclamationRectangle);
                    break;
                case "<notoriousmonster>":
                    sb.AddIcon(BitmapFontIcon.NotoriousMonster);
                    break;
                case "<alarm>":
                    sb.AddIcon(BitmapFontIcon.Alarm);
                    break;
                case "<fanfestival>":
                    sb.AddIcon(BitmapFontIcon.FanFestival);
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
        var prompt = tts.SpeakAsync(message);
        Task.Run(() =>
            { //this works but looks weird?
                while (!prompt.IsCompleted) ;
                tts.Dispose();
            });
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
        _trainManager.SaveHuntTrainRecord();
        TTS.Dispose();
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

    private void ResetPriorityMob()
    {
        _highestRank = HuntRank.B;
        _priorityMob = null;
    }

    private string GetMapNameFromPath(string path)
    { //all files end with '-data.jpg', img source - http://cablemonkey.us/huntmap2/
        var pathRemoved = path.Remove(0, ImageFolderPath.Length).Replace("_", " ");
        return pathRemoved.Remove(pathRemoved.Length - 9);
    }

    public HuntRank GetHuntRank(uint modelID)
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

    public void LoadMapImages()
    {
        if (ImagesLoaded) return;
        //if images/map folder doesn't exist, or is empty
        if (!Directory.Exists(ImageFolderPath)) return;

        var files = Directory.EnumerateFiles(ImageFolderPath).ToList();
        if (!files.Any() || files.Count != 41) return; //wait until all images downloaded
        
        var paths = Directory.EnumerateFiles(ImageFolderPath, "*", SearchOption.TopDirectoryOnly);
        foreach (var path in paths)
        {
            var name = GetMapNameFromPath(path);
            if (_mapImages.ContainsKey(name)) continue;
            _mapImages.Add(name, _pluginInterface.UiBuilder.LoadImage(path));
        }
        ImagesLoaded = true;
        return;
    }

    public bool DownloadingImages = false;
    public bool HasDownloadErrors = false;
    public List<string> DownloadErrors = new List<string>();

    //only called from GUI
    public async void DownloadImages(List<MapSpawnPoints> spawnpointdata)
    {
        DownloadingImages = true;
        try
        {
            Directory.CreateDirectory(ImageFolderPath); //create dir where images will be stored
        }
        catch (Exception ex)
        {
            DownloadErrors.Add(ex.Message);
        }

        //use spawnpoint data to get map names and generate urls.. coz lazy to retype
        var names = spawnpointdata.Select(x => x.MapName).ToList();
        var urls = names.Select(n => n = Constants.BaseImageUrl + n.Replace(" ", "_") + "-data.jpg").ToList();
        
        //then async download each image and save to file
        var downloader = new ImageDownloader(urls, ImageFolderPath);
        var results = await downloader.BeginDownloadAsync();
        DownloadErrors.AddRange(results);
        if (DownloadErrors.Count > 0)
        {
            HasDownloadErrors = true;
            PluginLog.Error("Error:");
            DownloadErrors.ForEach(e => PluginLog.Error(e));
        }
        DownloadingImages = false;
    }

}