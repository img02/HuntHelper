using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using HuntHelper.Managers.Hunts;
using HuntHelper.Managers.Hunts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace HuntHelper.Managers;

public class IpcSystem : IDisposable
{
    private const uint HuntHelperApiVersion = 1;

    private readonly DalamudPluginInterface _pluginInterface;
    private readonly IFramework _framework;
    private readonly TrainManager _trainManager;

    private readonly ICallGateProvider<uint> _cgGetVersion;
    private readonly ICallGateProvider<List<MobRecord>> _cgGetTrainList;

    public IpcSystem(DalamudPluginInterface pluginInterface, IFramework framework, TrainManager trainManager)
    {
        _pluginInterface = pluginInterface;
        _framework = framework;
        _trainManager = trainManager;

        _cgGetVersion = pluginInterface.GetIpcProvider<uint>("HH.GetVersion");
        _cgGetTrainList = pluginInterface.GetIpcProvider<List<MobRecord>>("HH.GetTrainList");

        _cgGetVersion.RegisterFunc(GetVersion);
        _cgGetTrainList.RegisterFunc(GetTrainList);

        pluginInterface.GetIpcProvider<uint, bool>("HH.Enable").SendMessage(HuntHelperApiVersion);
    }

    public void Dispose()
    {
        _cgGetVersion.UnregisterFunc();
        _cgGetTrainList.UnregisterFunc();

        _pluginInterface.GetIpcProvider<bool>("HH.Disable");
    }

    private static uint GetVersion() => HuntHelperApiVersion;

    private List<MobRecord> GetTrainList() =>
        _framework.RunOnFrameworkThread(() =>
            _trainManager.HuntTrain.Select(AsMobRecord).ToList()
        ).Result;

    private static MobRecord AsMobRecord(HuntTrainMob mob) =>
        new MobRecord(mob.Name, mob.MobID, mob.TerritoryID, mob.MapID, mob.Instance, mob.Position, mob.Dead, mob.LastSeenUTC);

    private record struct MobRecord(
        string Name,
        uint MobID,
        uint TerritoryID,
        uint MapID,
        uint Instance,
        Vector2 Position,
        bool Dead,
        DateTime LastSeenUTC
    );
}
