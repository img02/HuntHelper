﻿using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using HuntHelper.Managers.Hunts.Models;
using HuntHelper.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace HuntHelper.Managers.Hunts;

public class TrainList<T> : List<T>
{
    public event Action TrainChanged;
    public new void Add(T item)
    {
        base.Add(item);
        OnTrainChanged();
    }
    public new void AddRange(IEnumerable<T> collection)
    {
        base.AddRange(collection);
        OnTrainChanged();
    }
    public new void Remove(T item)
    {
        base.Remove(item);
        OnTrainChanged();
    }
    public new void Clear()
    {
        base.Clear();
        OnTrainChanged();
    }

    private void OnTrainChanged()
    {
        TrainChanged?.Invoke();
    }
}

public class TrainManager : IDisposable
{
    private readonly IChatGui _chatGui;
    private readonly IGameGui _gameGui;

    private readonly string _huntTrainFilePath;

    public readonly TrainList<HuntTrainMob> HuntTrain;
    public readonly List<HuntTrainMob> ImportedTrain;
    public bool RecordTrain = false;
    public bool ImportFromIPC = false;

    public TrainManager(IChatGui chatGui, IGameGui gameGui, string huntTrainFilePath)
    {
        HuntTrain = new TrainList<HuntTrainMob>();
        ImportedTrain = new List<HuntTrainMob>();

        _chatGui = chatGui;
        _gameGui = gameGui;

        _huntTrainFilePath = huntTrainFilePath;
        LoadHuntTrainRecord();

        HuntTrain.TrainChanged += HuntTrain_TrainChanged;
    }

    private void HuntTrain_TrainChanged()
    {
        SaveHuntTrainRecord();
    }

    public void Dispose()
    {
        HuntTrain.TrainChanged -= HuntTrain_TrainChanged;
    }
    public void AddMob(IBattleNpc mob, uint territoryid, uint mapid, uint instance, string mapName, float zoneMapCoordSize)
    {   //if already exists in train, return
        if (HuntTrain.Any(m => m.IsSameAs(mob.NameId, instance))) return;
        var position = new Vector2(MapHelpers.ConvertToMapCoordinate(mob.Position.X, zoneMapCoordSize),
            MapHelpers.ConvertToMapCoordinate(mob.Position.Z, zoneMapCoordSize));
        var trainMob = new HuntTrainMob(mob.Name.TextValue, mob.NameId, territoryid, mapid, instance, mapName, position, DateTime.UtcNow, false);
        HuntTrain.Add(trainMob);
    }

    public bool UpdateLastSeen(IBattleNpc mob, uint instance)
    {
        var existing = HuntTrain.FirstOrDefault(m => m.IsSameAs(mob.NameId, instance));
        if (existing == null) return false;
        existing.LastSeenUTC = DateTime.UtcNow;
        return true;
    }
    public void SendTrainFlag(int index, bool openMap, bool showInChat, ushort textColor = 24, ushort flagColour = 559, ushort countColour = 502) //make customizable in the future, maybe
    {
        if (openMap) OpenMap(HuntTrain[index], openMap);
        if (!showInChat) return;

        var sb = new SeStringBuilder();
        if (index == -1)
        {
            sb.AddUiForeground(textColor);
            sb.AddText("Nothing Left");
            sb.AddUiForegroundOff();
            _chatGui.Print(sb.BuiltString);
            return;
        }

        var instance = LocalizationUtil.GetInstanceGlyph(HuntTrain[index].Instance);
        var name = $"{HuntTrain[index].Name}{instance}";

        {
            sb.AddUiForeground(textColor);
            sb.AddIcon(BitmapFontIcon.ExclamationRectangle);
            sb.AddText(name + "---");
            sb.AddUiForegroundOff();

            sb.AddUiForeground(flagColour);
            sb.Append(HuntTrain[index].MapLink);
            sb.AddUiForegroundOff();

            sb.AddUiForeground(countColour);
            sb.AddText($" --- {index + 1}/{HuntTrain.Count}");
            sb.AddUiForegroundOff();

            _chatGui.Print(sb.BuiltString);
        }
    }

    public void OpenMap(HuntTrainMob mob, bool openMap)
    {
        if (!openMap) return;
        var mlp = (MapLinkPayload)mob.MapLink.Payloads[0];
        OpenMap(mlp);
    }

    public void OpenMap(AetheryteData aeth, HuntTrainMob mob)
    {
        var mlp = new MapLinkPayload(mob.TerritoryID, mob.MapID, aeth.Position.X, aeth.Position.Y);
        OpenMap(mlp);
    }

    public void OpenMap(MapLinkPayload mapLink)
    {
        _gameGui.OpenMapWithMapLink(mapLink);
    }

    public void Import(string importCode)
    {
        var temp = ExportImport.Import(importCode, ImportedTrain);
        Import(temp);
    }

    public void Import(IList<HuntTrainMob> trainMobs)
    {
        ImportedTrain.Clear(); //should be empty already
        ImportedTrain.AddRange(trainMobs);
        MapHelpers.LocaliseMobNames(ImportedTrain);
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
            if (HuntTrain.All(mob => mob.MobID != m.MobID || mob.Instance != m.Instance)) HuntTrain.Add(m);

            if (updateOldTime)
            {   //inefficient?
                var toUpdate = HuntTrain.FirstOrDefault(mob => mob.MobID == m.MobID && mob.Instance == m.Instance);
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

    public void TrainRemove(HuntTrainMob mob)
    {
        if (mob == null) return;
        HuntTrain.Remove(mob);
    }

    public void TrainUnkillAll() => HuntTrain.ForEach(m => m.Dead = false);
    public void TrainDelete() => HuntTrain.Clear();

    public void TrainMarkAsDead(int index)
    {
        HuntTrain[index].Dead = true;
        SaveHuntTrainRecord();
    }

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
        PluginLog.Verbose("Saving train data...");
    }

}
