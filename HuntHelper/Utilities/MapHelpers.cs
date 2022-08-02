using System;
using System.Reflection.Metadata;
using Dalamud.Data;
using Dalamud.Game.ClientState;
using Lumina.Excel.GeneratedSheets;

namespace HuntHelper.Utilities;

public class MapHelpers
{
    public static string GetMapName(DataManager dataManager, uint mapID)
    {
        return dataManager.Excel.GetSheet<TerritoryType>()?.GetRow(mapID)?.PlaceName?.Value?.Name.ToString() ?? "location not found";
    }

    public static float ConvertToMapCoordinate(float pos, float zoneMaxCoordSize)
    {   
        return (float)Math.Floor(((zoneMaxCoordSize + 1.96) / 2 + (pos / 50)) * 100) / 100;
    }
}