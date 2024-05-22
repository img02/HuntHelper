using Dalamud;
using Dalamud.IoC;
using Dalamud.Plugin.Services;
using HuntHelper.Managers.Hunts.Models;
using Lumina.Data;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;

namespace HuntHelper.Utilities;
/// <summary>
/// Rename this class... DataManagerUtil? ExcelUtil? idk
/// </summary>
public class MapHelpers
{
    private static Dictionary<string, string> ChineseToEnglish = new Dictionary<string, string>()
    {
        {"中拉诺西亚","Middle La Noscea"},{"拉诺西亚低地","Lower La Noscea"},{"东拉诺西亚","Eastern La Noscea"},{"西拉诺西亚","Western La Noscea"},{"拉诺西亚高地","Upper La Noscea"},{"拉诺西亚外地","Outer La Noscea"},//La Noscea
        {"黑衣森林中央林区","Central Shroud"},{"黑衣森林东部林区","East Shroud"},{"黑衣森林南部林区","South Shroud"},{"黑衣森林北部林区","North Shroud"},//Shroud
        {"西萨纳兰","Western Thanalan"},{"中萨纳兰","Central Thanalan"},{"东萨纳兰","Eastern Thanalan"},{"南萨纳兰","Southern Thanalan"},{"北萨纳兰","Northern Thanalan"},//Thanalan
        {"库尔札斯中央高地","Coerthas Central Highlands"},{"摩杜纳","Mor Dhona"},//2.0
        {"阿巴拉提亚云海","The Sea of Clouds"},{"魔大陆阿济兹拉","Azys Lla"},{"龙堡参天高地","The Dravanian Forelands"},{"翻云雾海","The Churning Mists"},{"龙堡内陆低地","The Dravanian Hinterlands"},{"库尔札斯西部高地","Coerthas Western Highlands"},//3.0
        {"基拉巴尼亚边区","The Fringes"},{"红玉海","The Ruby Sea"},{"基拉巴尼亚山区","The Peaks"},{"延夏","Yanxia"},{"基拉巴尼亚湖区","The Lochs"},{"太阳神草原","The Azim Steppe"},//4.0
        {"雷克兰德","Lakeland"},{"珂露西亚岛","Kholusia"},{"安穆·艾兰","Amh Araeng"},{"伊尔美格","Il Mheg"},{"拉凯提卡大森林","The Rak'tika Greatwood"},{"黑风海","The Tempest"},//5.0
        {"迷津","Labyrinthos"},{"萨维奈岛","Thavnair"},{"加雷马","Garlemald"},{"叹息海","Mare Lamentorum"},{"厄尔庇斯","Elpis"},{"天外天垓","Ultima Thule"}//6.0
    };

    private static List<ClientLanguage> languages = new List<ClientLanguage> { ClientLanguage.English, ClientLanguage.Japanese, ClientLanguage.German, ClientLanguage.French };

    public static IDataManager DataManager;

    public static void SetUp(IDataManager dataManager)
    {
        DataManager = dataManager;
    }

    public static string GetMapName(uint territoryID) //map id... territory id... confusing ...
    {
        return DataManager.Excel.GetSheet<TerritoryType>()?.GetRow(territoryID)?.PlaceName?.Value?.Name.ToString() ?? "location not found";
    }
    public static string GetMapNameInEnglish(uint territoryID,ClientLanguage clientLanguage)
    {
        var row = DataManager.Excel.GetSheet<TerritoryType>(Language.English)?.GetRow(territoryID)?.PlaceName.Row ?? 0;
        if (languages.Contains(clientLanguage)){
            return DataManager.Excel.GetSheet<PlaceName>(Language.English)?.GetRow(row)?.Name.ToString() ?? "location not found";
        }
        var mapName = DataManager.Excel.GetSheet<PlaceName>()?.GetRow(row)?.Name.ToString() ?? "location not found";
        return ChineseToEnglish.ContainsKey(mapName) ? ChineseToEnglish[mapName] : "location not found";
    }

    public static uint GetMapID(uint territoryID) //createmaplink doesn't work with "Mor Dhona" :(
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