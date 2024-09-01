using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui.FlyText;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HuntHelper.Managers.Hunts.Models;
using HuntHelper.Managers.MapData.Models;
using HuntHelper.Utilities;
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

public class HuntManager : IDisposable
{
    //todo 6 new dawntrail maps? double check this later
    public static int HuntMapCount = 47;
    public readonly string ImageFolderPath;

    private readonly Dictionary<HuntRank, List<Mob>> _arrDict;
    private readonly Dictionary<HuntRank, List<Mob>> _hwDict;
    private readonly Dictionary<HuntRank, List<Mob>> _shbDict;
    private readonly Dictionary<HuntRank, List<Mob>> _sbDict;
    private readonly Dictionary<HuntRank, List<Mob>> _ewDict;
    private readonly Dictionary<HuntRank, List<Mob>> _dtDict;
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly IChatGui _chatGui;
    private readonly IFlyTextGui _flyTextGui;
    private readonly TrainManager _trainManager;

    private readonly List<(HuntRank Rank, IBattleNpc Mob)> _currentMobs;
    private readonly List<(HuntRank Rank, IBattleNpc Mob)> _previousMobs;

    private IBattleNpc? _priorityMob;
    private HuntRank _highestRank;

    public int ACount;
    public int SCount;
    public int BCount;

    public bool NotAllImagesFound { get; private set; } = false;
    public bool ImageFolderDoesntExist { get; private set; } = false;
    public bool ErrorPopUpVisible = false;
    public string ErrorMessage = string.Empty;



    public bool DontUseSynthesizer = false;
    public SpeechSynthesizer TTS { get; init; } //aint really used anymore except for setting default voice on load
    public string TTSName { get; set; }
    public int TTSVolume { get; set; }

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

    public List<(HuntRank Rank, IBattleNpc Mob)> CurrentMobs => _currentMobs;
    
    public event Action<HuntTrainMob> MarkSeen;
    

    public HuntManager(IDalamudPluginInterface pluginInterface, TrainManager trainManager, IChatGui chatGui, IFlyTextGui flyTextGui, int ttsVolume)
    {
        _arrDict = new Dictionary<HuntRank, List<Mob>>();
        _hwDict = new Dictionary<HuntRank, List<Mob>>();
        _shbDict = new Dictionary<HuntRank, List<Mob>>();
        _sbDict = new Dictionary<HuntRank, List<Mob>>();
        _ewDict = new Dictionary<HuntRank, List<Mob>>();
        _dtDict = new Dictionary<HuntRank, List<Mob>>();
        _currentMobs = new List<(HuntRank, IBattleNpc)>();
        _previousMobs = new List<(HuntRank, IBattleNpc)>();
        _pluginInterface = pluginInterface;
        _chatGui = chatGui;
        _flyTextGui = flyTextGui;
        _trainManager = trainManager;
        TTSVolume = ttsVolume;

        HuntTrain = new List<HuntTrainMob>();
        ImportedTrain = new List<HuntTrainMob>();

        ImageFolderPath = Path.Combine(_pluginInterface.AssemblyLocation.Directory?.FullName!, @"Images\Maps\");

        // Starting this makes the plugin fail to load - on non-windows
        try
        {
            TTS = new SpeechSynthesizer();
            TTSName = TTS.Voice.Name;
        }
        catch
        {
            DontUseSynthesizer = true;
        }

        LoadHuntData();
        CheckImageStatus();
    }

    public void CheckImageStatus()
    {
        //PluginLog.Warning($"allfound:{NotAllImagesFound} | folder:{ImageFolderDoesntExist}");
        if (!Directory.Exists(ImageFolderPath))
        {
            ImageFolderDoesntExist = true;
            return;
        }

        var files = Directory.EnumerateFiles(ImageFolderPath).ToList();
        if (files.Count != HuntMapCount)
        {
            if (DownloadingImages) return; //wait until all images downloaded
            if (files.Count > 0) NotAllImagesFound = true;
        }
        else NotAllImagesFound = false;

    }

    public (HuntRank Rank, IBattleNpc? Mob) GetPriorityMob()
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
    public List<(HuntRank Rank, IBattleNpc Mob)> GetAllCurrentMobsWithRank()
    {
        return _currentMobs;
    }

    public void AddToTrain(IBattleNpc mob, uint territoryid, uint mapid, uint instance, string mapName, float zoneMapCoordSize)
    {
        //if mob already exists, update last seen - even if not recording
        if (_trainManager.UpdateLastSeen(mob, instance)) return;
        if (!_trainManager.RecordTrain) return;
        //only record A ranks
#if !DEBUG //record all ranks while debugging coz weird ppl still kill ARR A ranks which makes it hard to find hunts to test with.
        if (GetHuntRank(mob.NameId) != HuntRank.A) return;
#endif
        _trainManager.AddMob(mob, territoryid, mapid, instance, mapName, zoneMapCoordSize);
    }

    //bit much, grew big because I don't plan
    public void AddNearbyMobs(List<IBattleNpc> nearbyMobs, float zoneMapCoordSize, uint territoryId, uint mapid,
        bool aTTS, bool bTTS, bool sTTS, string aTTSmsg, string bTTSmsg, string sTTSmsg,
        bool chatA, bool chatB, bool chatS, string chatAmsg, string chatBmsg, string chatSmsg,
        bool flyTxtA, bool flyTxtB, bool flyTxtS, uint instance)
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
            
            MarkSeen?.Invoke(mob.ToHuntTrainMob(territoryId, mapid, instance, MapHelpers.GetMapName(territoryId), zoneMapCoordSize));

            //Do tts and chat stuff
            var rank = GetHuntRank(mob.NameId);
            switch (rank)
            {
                case HuntRank.A:
                    NewMobFoundTTS(aTTS, aTTSmsg, mob);
                    SendChatMessage(chatA, chatAmsg, territoryId, mapid, instance, mob, zoneMapCoordSize);
                    SendFlyText(rank, mob, flyTxtA);
                    ACount++;
                    break;
                case HuntRank.B:
                    NewMobFoundTTS(bTTS, bTTSmsg, mob);
                    SendChatMessage(chatB, chatBmsg, territoryId, mapid, instance, mob, zoneMapCoordSize);
                    SendFlyText(rank, mob, flyTxtB);
                    BCount++;
                    break;
                case HuntRank.S:
                case HuntRank.SS: //don't think ss is actually used lol
                    NewMobFoundTTS(sTTS, sTTSmsg, mob);
                    SendChatMessage(chatS, chatSmsg, territoryId, mapid, instance, mob, zoneMapCoordSize);
                    SendFlyText(rank, mob, flyTxtS);
                    SCount++;
                    break;
            }
        }
    }

    #region rework later?

    //sent fly text in-game on the player  -- move these sestring colours from here and chatmsg to consts or something
    private void SendFlyText(HuntRank rank, IBattleNpc mob, bool enabled)
    {
        if (!enabled) return;
        var rankSB = new SeStringBuilder();
        var nameSB = new SeStringBuilder();
        switch (rank)
        {
            case HuntRank.A:            //didn't hyphen 'rank' here.. think it looks better
                rankSB.AddUiForeground("A RANK", _aTextColour); // pinkish-red - same as chat msg
                nameSB.AddUiForeground($"{mob.Name}", _aFlyTextColour); //tinted pinkish-red
                _flyTextGui.AddFlyText(FlyTextKind.DamageCritDh, 1, 1, 1, rankSB.BuiltString, nameSB.BuiltString, 16, 2, 0);//last 2 nums don't seem to change anything
                break;
            case HuntRank.B:
                rankSB.AddUiForeground("B RANK", _bTextColour); // blue - same as chat msg
                nameSB.AddUiForeground($"{mob.Name}", _bFlyTextColour); //tinted blue
                _flyTextGui.AddFlyText(FlyTextKind.DamageCritDh, 1, 1, 1, rankSB.BuiltString, nameSB.BuiltString, 16, 2, 0);
                break;
            case HuntRank.S:
            case HuntRank.SS:
                rankSB.AddUiForeground("S RANK", _sFlyTextColour); // dark-red - different from chat msg (goldish) because it stands out more.
                nameSB.AddUiForeground($"{mob.Name}", _sTextColour); //same gold as chat msg
                _flyTextGui.AddFlyText(FlyTextKind.DamageCritDh, 1, 1, 1, rankSB.BuiltString, nameSB.BuiltString, 16, 2, 0);
                break;
        }
    }

    public void SendChatMessage(bool enabled, string msg, uint territoryId, uint mapid, uint instance, IBattleNpc mob, float zoneCoordSize)
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
                    var name = $"{mob.Name}{LocalizationUtil.GetInstanceGlyph(instance)}";
                    if (rank == HuntRank.A) sb.AddUiForeground(name, _aTextColour); //red / pinkish
                    if (rank == HuntRank.B) sb.AddUiForeground(name, _bTextColour); //blue
                    if (rank == HuntRank.S) sb.AddUiForeground(name, _sTextColour); //gold 
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

    private void NewMobFoundTTS(bool enabled, string msg, IBattleNpc mob)
    {
        if (!enabled) return;
        var message = FormatMessageFlags(msg, mob);
        //changed to creating a new tts each time because SpeakAsync just queues up to play...
        if (DontUseSynthesizer) return;
        var tts = new SpeechSynthesizer();
        tts.SelectVoice(TTSName);
        tts.Volume = TTSVolume;
        var prompt = tts.SpeakAsync(message);
        Task.Run(() =>
            { //this works but looks weird?
                while (!prompt.IsCompleted) ;
                tts.Dispose();
            });
    }

    private string FormatMessageFlags(string msg, IBattleNpc mob)
    {
        msg = msg.Replace("<rank>", $"{GetHuntRank(mob.NameId)}-Rank", true, CultureInfo.InvariantCulture);
        msg = msg.Replace("<name>", $"{mob.Name}", true, CultureInfo.InvariantCulture);
        msg = msg.Replace("<hpp>", $"{GetHPP(mob):0}", true, CultureInfo.InvariantCulture);
        return msg;
    }

    #endregion

    public List<IBattleNpc> GetCurrentMobs()
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
        var DTJsonFiles = new List<string>
        {
            "./data/DT-A.json",
            "./data/DT-B.json",
            "./data/DT-S.json"
        };


        LoadFilesIntoDic(new[] {
            (_arrDict, ARRJsonFiles),
            (_hwDict, HWJsonFiles),
            (_sbDict, SBJsonFiles),
            (_shbDict, ShBJsonFiles),
            (_ewDict, EWJsonFiles),
            (_dtDict, DTJsonFiles )
        });
    }

    public float GetMapZoneCoordSize(ushort mapID)
    {
        //EVERYTHING EXCEPT HEAVENSWARD HAS A SCALE OF 100, BUT FOR SOME REASON HW HAS 95, WHYYYYYY
        if (mapID is >= 397 and <= 402) return 95f;
        return 100f;
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
        text += DictToString(_dtDict);
        return text;
    }
    public bool IsHunt(uint modelID)
    {
        var exists = _dtDict.Any(kvp => kvp.Value.Any(m => m.ModelID == modelID));
        if (!exists) exists = _ewDict.Any(kvp => kvp.Value.Any(m => m.ModelID == modelID));
        if (!exists) exists = _shbDict.Any(kvp => kvp.Value.Any(m => m.ModelID == modelID));
        if (!exists) exists = _sbDict.Any(kvp => kvp.Value.Any(m => m.ModelID == modelID));
        if (!exists) exists = _hwDict.Any(kvp => kvp.Value.Any(m => m.ModelID == modelID));
        if (!exists) exists = _arrDict.Any(kvp => kvp.Value.Any(m => m.ModelID == modelID));
        return exists;
    }

    public void Dispose()
    {
        _trainManager.SaveHuntTrainRecord();
        if (!DontUseSynthesizer)
            TTS.Dispose();
    }

    public double GetHPP(IBattleNpc mob)
    {
        return Math.Round(((1.0 * mob.CurrentHp) / mob.MaxHp) * 100, 2);
    }

    private void PriorityCheck(IBattleNpc mob)
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

    public HuntRank GetHuntRank(uint modelID)
    {
        //just default to B if for some reason mob can't be found - shouldn't happen tho...
        var rank = HuntRank.B;
        var found = false;

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
        kvp = SearchDictionaryForModelID(_dtDict, modelID);
        if (!kvp.Equals(default(KeyValuePair<HuntRank, List<Mob>>))) return kvp.Key;

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

    private void LoadFilesIntoDic((Dictionary<HuntRank, List<Mob>> dict, List<string> filePaths)[] toLoad)
    {
        foreach (var (dict, filePaths) in toLoad) LoadFilesIntoDic(dict, filePaths);
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


    private bool reload = false;
    public IDalamudTextureWrap? GetMapImage(string mapName)
    {
        // won't reload if image updated but texture used in last 2 secs
        //https://github.com/goatcorp/Dalamud/blob/ee362acf70d47dd30c46da931f55958010fbf502/Dalamud/Interface/Internal/TextureManager.cs#L416
        //https://github.com/goatcorp/Dalamud/blob/ee362acf70d47dd30c46da931f55958010fbf502/Dalamud/Interface/Internal/TextureManager.cs#L425
        //https://github.com/goatcorp/Dalamud/blob/ee362acf70d47dd30c46da931f55958010fbf502/Dalamud/Interface/Internal/TextureManager.cs#L36

        if (reload) return null;

        var fileName = mapName.Replace(" ", "_") + ("-data.jpg");
        //Default because Empty can override user window opacity 
        return Plugin.TextureProvider.GetFromFile(ImageFolderPath + fileName).GetWrapOrDefault();
    }

    public bool DownloadingImages = false;
    public bool HasDownloadErrors = false;
    public List<string> DownloadErrors = new List<string>();

    public async void DownloadImages(List<MapSpawnPoints> spawnpointdata, Configuration _configuration)
    {
        DownloadingImages = true;
        try
        {   // if redownloading, delete all files first.
            if (Directory.Exists(ImageFolderPath)) Directory.Delete(ImageFolderPath, true);
            Directory.CreateDirectory(ImageFolderPath);
            ImageFolderDoesntExist = false;
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
        else
        {
            UpdateImageVer(_configuration);

            // for updating current map image drawn, if redownloaded
            reload = true;
            Task.Run(async () => { await Task.Delay(2222); reload = false; });
        }
        DownloadingImages = false;
    }

    private async void UpdateImageVer(Configuration config)
    {
        if (!Directory.Exists(ImageFolderPath)) return;
        var files = Directory.EnumerateFiles(ImageFolderPath).ToList();

        if (files.Count == HuntMapCount) config.MapImagesVer = await MapHelpers.GetMapImageVer();
    }

    public bool IsDawntrailHunt(uint mobId)
    {
        return _dtDict.Any(kvp => kvp.Value.Any(m => m.ModelID == mobId));
    }

    public string GetMobNameInEnglish(uint mobId)
    {
        string searchList(List<Mob> list)
        {
            var m = list.FirstOrDefault(m => m.ModelID == mobId)!.Name;
            //PluginLog.Error(m);
            return m;
        }

        var kvp = SearchDictionaryForModelID(_arrDict, mobId);
        if (!kvp.Equals(default(KeyValuePair<HuntRank, List<Mob>>))) return searchList(kvp.Value);
        kvp = SearchDictionaryForModelID(_hwDict, mobId);
        if (!kvp.Equals(default(KeyValuePair<HuntRank, List<Mob>>))) return searchList(kvp.Value);
        kvp = SearchDictionaryForModelID(_sbDict, mobId);
        if (!kvp.Equals(default(KeyValuePair<HuntRank, List<Mob>>))) return searchList(kvp.Value);
        kvp = SearchDictionaryForModelID(_shbDict, mobId);
        if (!kvp.Equals(default(KeyValuePair<HuntRank, List<Mob>>))) return searchList(kvp.Value);
        kvp = SearchDictionaryForModelID(_ewDict, mobId);
        if (!kvp.Equals(default(KeyValuePair<HuntRank, List<Mob>>))) return searchList(kvp.Value);
        kvp = SearchDictionaryForModelID(_dtDict, mobId);
        if (!kvp.Equals(default(KeyValuePair<HuntRank, List<Mob>>))) return searchList(kvp.Value);

        return "mob id not found";
    }

}