using System;
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

    public static double ConvertToMapCoordinate(float pos)
    {
        return (Math.Floor((21.48 + (pos / 50)) * 100)) / 100;
    }


}