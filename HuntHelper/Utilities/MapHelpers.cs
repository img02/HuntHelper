using Dalamud.Data;
using Lumina.Data;
using Lumina.Excel.GeneratedSheets;
using System;
using Dalamud.Plugin.Services;

namespace HuntHelper.Utilities;

public class MapHelpers
{
    public static string GetMapName(IDataManager dataManager, uint territoryID) //map id... territory id... confusing ...
    {
        return dataManager.Excel.GetSheet<TerritoryType>()?.GetRow(territoryID)?.PlaceName?.Value?.Name.ToString() ?? "location not found";
    }

    public static string GetMapNameInEnglish(IDataManager dataManager, uint territoryID)
    {
        var row = dataManager.Excel.GetSheet<TerritoryType>(Language.English)?.GetRow(territoryID)?.PlaceName.Row ?? 0;
        return dataManager.Excel.GetSheet<PlaceName>(Language.English)?.GetRow(row)?.Name.ToString() ?? "location not found";
    }

    public static uint GetMapID(IDataManager dataManager, uint territoryID) //createmaplink doesn't work with "Mor Dhona" :(
    {
        return dataManager!.GetExcelSheet<TerritoryType>()!.GetRow(territoryID)!.Map.Value!.RowId;
    }

    public static float ConvertToMapCoordinate(float pos, float zoneMaxCoordSize)
    {
        return (float)Math.Floor(((zoneMaxCoordSize + 1.96) / 2 + (pos / 50)) * 100) / 100;
    }
}