using Dalamud.Data;
using Lumina.Excel.GeneratedSheets;
using System;

namespace HuntHelper.Utilities;

public class MapHelpers
{
    public static string GetMapName(DataManager dataManager, uint territoryID) //map id... territory id... confusing ...
    {
        return dataManager.Excel.GetSheet<TerritoryType>()?.GetRow(territoryID)?.PlaceName?.Value?.Name.ToString() ?? "location not found";
    }

    public static uint GetMapID(DataManager dataManager, uint territoryID) //createmaplink doesn't work with "Mor Dhona" :(
    {
        return dataManager!.GetExcelSheet<TerritoryType>()!.GetRow(territoryID)!.Map.Value!.RowId;
    }

    public static float ConvertToMapCoordinate(float pos, float zoneMaxCoordSize)
    {
        return (float)Math.Floor(((zoneMaxCoordSize + 1.96) / 2 + (pos / 50)) * 100) / 100;
    }
}