using Dalamud.Plugin.Services;
using HuntHelper.Managers.Hunts.Models;
using Lumina.Data;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Speech.Synthesis;

namespace HuntHelper.Utilities;
/// <summary>
/// Rename this class... DataManagerUtil? ExcelUtil? idk
/// </summary>
public class MapHelpers
{
    public static IDataManager DataManager;

    public static void SetUp(IDataManager dataManager)
    {
        DataManager = dataManager;
    }

    public static string GetMapName( uint territoryID) //map id... territory id... confusing ...
    {
        return DataManager.Excel.GetSheet<TerritoryType>()?.GetRow(territoryID)?.PlaceName?.Value?.Name.ToString() ?? "location not found";
    }

    public static string GetMapNameInEnglish( uint territoryID)
    {
        var row = DataManager.Excel.GetSheet<TerritoryType>(Language.English)?.GetRow(territoryID)?.PlaceName.Row ?? 0;
        return DataManager.Excel.GetSheet<PlaceName>(Language.English)?.GetRow(row)?.Name.ToString() ?? "location not found";
    }

    public static uint GetMapID( uint territoryID) //createmaplink doesn't work with "Mor Dhona" :(
    {
        return DataManager!.GetExcelSheet<TerritoryType>()!.GetRow(territoryID)!.Map.Value!.RowId;
    }

    public static float ConvertToMapCoordinate(float pos, float zoneMaxCoordSize)
    {
        return (float)Math.Floor(((zoneMaxCoordSize + 1.96) / 2 + (pos / 50)) * 100) / 100;
    }

    public static void LocaliseMobNames(List<HuntTrainMob> trainList)
    {
        trainList.ForEach(m => m.Name = DataManager.Excel.GetSheet<BNpcName>()?.GetRow(m.MobID)?.Singular.ToString() ?? m.Name);
    }

}