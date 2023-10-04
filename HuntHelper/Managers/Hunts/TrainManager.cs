using Dalamud.Data;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using HuntHelper.Managers.Hunts.Models;
using HuntHelper.Utilities;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Dalamud.Plugin.Services;

namespace HuntHelper.Managers.Hunts;

public class TrainManager
{
    private readonly IChatGui _chatGui;
    private readonly IGameGui _gameGui;
    private readonly IDataManager _dataManager;

    private readonly string _huntTrainFilePath;

    public readonly List<HuntTrainMob> HuntTrain;
    public readonly List<HuntTrainMob> ImportedTrain;
    public bool RecordTrain = false;

    public TrainManager(IChatGui chatGui, IGameGui gameGui, IDataManager dataManager, string huntTrainFilePath)
    {
        HuntTrain = new List<HuntTrainMob>();
        ImportedTrain = new List<HuntTrainMob>();

        _chatGui = chatGui;
        _gameGui = gameGui;
        _dataManager = dataManager;

        _huntTrainFilePath = huntTrainFilePath;
        LoadHuntTrainRecord();
    }

    public void AddMob(BattleNpc mob, uint territoryid, uint mapid, string mapName, float zoneMapCoordSize)
    {   //if already exists in train, return
        if (HuntTrain.Any(m => m.MobID == mob.NameId)) return;
        var position = new Vector2(MapHelpers.ConvertToMapCoordinate(mob.Position.X, zoneMapCoordSize),
            MapHelpers.ConvertToMapCoordinate(mob.Position.Z, zoneMapCoordSize));
        var trainMob = new HuntTrainMob(mob.Name.TextValue, mob.NameId, territoryid, mapid, mapName, position, DateTime.UtcNow, false);
        HuntTrain.Add(trainMob);
    }


    public bool UpdateLastSeen(BattleNpc mob)
    {
        var existing = HuntTrain.FirstOrDefault(m => m.MobID == mob.NameId);
        if (existing == null) return false;
        existing.LastSeenUTC = DateTime.UtcNow;
        return true;
    }
    public void SendTrainFlag(int index, bool openMap, ushort textColor = 24, ushort flagColour = 559, ushort countColour = 502) //make customizable in the future, maybe
    {
        var sb = new SeStringBuilder();

        if (index == -1)
        {
            sb.AddUiForeground(textColor);
            sb.AddText("Nothing Left");
            sb.AddUiForegroundOff();
            _chatGui.Print(sb.BuiltString);
            return;
        }

        sb.AddUiForeground(textColor);
        sb.AddIcon(BitmapFontIcon.ExclamationRectangle);
        sb.AddText(HuntTrain[index].Name + "---");
        sb.AddUiForegroundOff();

        sb.AddUiForeground(flagColour);
        sb.Append(HuntTrain[index].MapLink);
        sb.AddUiForegroundOff();

        sb.AddUiForeground(countColour);
        sb.AddText($" --- {index + 1}/{HuntTrain.Count}");
        sb.AddUiForegroundOff();

        _chatGui.Print(sb.BuiltString);

        if (!openMap) return;
        var mlp = (MapLinkPayload)HuntTrain[index].MapLink.Payloads[0];
        _gameGui.OpenMapWithMapLink(mlp);

    }

    public void OpenMap(HuntTrainMob mob, bool openMap)
    {
        if (!openMap) return;
        var mlp = (MapLinkPayload)mob.MapLink.Payloads[0];
        _gameGui.OpenMapWithMapLink(mlp);
    }

    public void Import(string importCode)
    {
        ImportedTrain.Clear(); //should be empty already
        var temp = ExportImport.Import(importCode, ImportedTrain);
        if (temp.Count > 0) ImportedTrain.AddRange(temp);
        LocaliseNames(ImportedTrain);
    }

    public void ImportTrainAll()
    {
        HuntTrain.Clear();
        HuntTrain.AddRange(ImportedTrain);
    }

    public void ImportTrainNew(bool updateOldTime)
    {
        foreach (var m in ImportedTrain)
        {
            if (HuntTrain.All(mob => mob.MobID != m.MobID)) HuntTrain.Add(m);

            if (updateOldTime)
            {   //inefficient?
                var toUpdate = HuntTrain.FirstOrDefault(mob => mob.MobID == m.MobID);
                if (toUpdate == null) continue;
                if (m.LastSeenUTC > toUpdate.LastSeenUTC) toUpdate.LastSeenUTC = m.LastSeenUTC;
            }
        }
    }

    public void TrainRemoveDead()
    {
        var toRemove = new List<HuntTrainMob>();
        foreach (var m in HuntTrain) if (m.Dead) toRemove.Add(m);
        foreach (var m in toRemove) HuntTrain.Remove(m);
    }
    //Unkill is deinitely a real word! :^
    public void TrainUnkillAll() => HuntTrain.ForEach(m => m.Dead = false);
    public void TrainDelete() => HuntTrain.Clear();


    public void LoadHuntTrainRecord()
    {
        //directory should exist by default, as that's where spawn and mob data is stored.
        if (!File.Exists(_huntTrainFilePath)) return;
        var deserialised = JsonConvert.DeserializeObject<List<HuntTrainMob>>(File.ReadAllText(_huntTrainFilePath));
        if (deserialised == null) return;
        HuntTrain.AddRange(deserialised);
    }

    public void SaveHuntTrainRecord()
    {
        var serialised = JsonConvert.SerializeObject(HuntTrain, Formatting.Indented);
        File.WriteAllText(_huntTrainFilePath, serialised);
    }


    private void LocaliseNames(List<HuntTrainMob> trainList)
    {
        trainList.ForEach(m => m.Name = _dataManager.Excel.GetSheet<BNpcName>()?.GetRow(m.MobID)?.Singular.ToString() ?? m.Name);
    }
}
