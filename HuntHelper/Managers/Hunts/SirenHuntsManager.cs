using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using HuntHelper.Managers.Hunts.Models;
using HuntHelper.Managers.MapData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Numerics;

namespace HuntHelper.Managers.Hunts;

public class SirenHuntsManager
{
    private IDictionary<string, SirenHuntsPatchData> _sirenHuntsData;

    private readonly Configuration _config;
    private readonly IChatGui _chatGui;
    private readonly MapDataManager _mapDataManager;
    private readonly string _sirenHuntsFilePath;

    public SirenHuntsManager(
        Configuration config,
        IChatGui chatGui,
        MapDataManager mapDataManager,
        string sirenHuntsFilePath)
    {
        _config = config;
        _chatGui = chatGui;
        _mapDataManager = mapDataManager;
        _sirenHuntsFilePath = sirenHuntsFilePath;
        LoadSirenHuntsData();
    }

    public string GetSirenHuntsLink(IList<HuntTrainMob> mobPositions)
    {
        var link = GenerateLink(mobPositions);
        SendLinkToChat(link);
        return link;
    }

    private void SendLinkToChat(string link)
    {
        var linkMessage = new SeStringBuilder();
        linkMessage.AddUiForeground(67);
        linkMessage.AddText("[Hunt Helper] ");
        linkMessage.AddUiForegroundOff();

        if (link.Length == 0)
        {
            linkMessage.AddUiForeground(31);
            linkMessage.AddText("No link generated");
            linkMessage.AddUiForegroundOff();
        }
        else
        {
            linkMessage.AddUiForeground(58);
            linkMessage.AddText("Generated link: ");
            linkMessage.AddUiForeground(33);
            linkMessage.AddText(link);
            linkMessage.AddUiForegroundOff();
        }

        _chatGui.Print(linkMessage.BuiltString);
    }

    public string GenerateLink(IList<HuntTrainMob> mobPositions)
    {
        var pathSegments = new List<string>
            {
                GetUrlComponent("HW", mobPositions),
                GetUrlComponent("SB", mobPositions),
                GetUrlComponent("SHB", mobPositions),
                GetUrlComponent("EW", mobPositions)
            }
            .Where(component => 0 < component.Length)
            .ToList();

        if (pathSegments.Count == 0) return string.Empty;

        var marksPath = string.Join('&', pathSegments);
        var baseUrl = _config.SirenHuntsBaseUrl.TrimEnd('/');
        return $"{baseUrl}/{marksPath}";
    }

    private string GetUrlComponent(string patch, IList<HuntTrainMob> mobs)
    {
        var patchMobs = _sirenHuntsData[patch].MobOrder.Select(patchOrderMobName =>
            mobs.FirstOrDefault(mob =>
            {
                var mobName = mob.Name;
                if (0 < mob.Instance) mobName += $" {mob.Instance}";
                return string.Equals(mobName, patchOrderMobName, StringComparison.CurrentCultureIgnoreCase);
            })
        ).ToImmutableList();

        if (patchMobs.All(mob => mob == null)) return string.Empty;

        var urlGlyphs = patchMobs.Select(mob =>
        {
            if (mob == null)
                return "-";
            else
                return GetNearestSpawnGlyph(patch, mob);
        });

        return patch + ">" + string.Join("", urlGlyphs);
    }

    private string GetNearestSpawnGlyph(string patch, HuntTrainMob mob)
    {
        var patchMaps = _sirenHuntsData[patch].Maps;
        if (!patchMaps.ContainsKey(mob.TerritoryID)) return "?";

        return patchMaps[mob.TerritoryID].SpawnPoints
            .Select(spawnPoint =>
            {
                var distSq = (spawnPoint.Item1 - mob.Position).LengthSquared();
                return (distSq, spawnPoint.Item2);
            })
            .MinBy(pair => pair.Item1)
            .Item2;
    }

    private void LoadSirenHuntsData()
    {
        //directory should exist by default, as that's where spawn and mob data is stored.
        if (!File.Exists(_sirenHuntsFilePath)) throw new Exception("Siren Hunts file not found ;-;");
        var deserialized = JsonConvert.DeserializeObject<IDictionary<string, JObject>>(File.ReadAllText(_sirenHuntsFilePath));
        if (deserialized == null) throw new Exception("Failed to deserialize Siren Hunts data ;-;");

        _sirenHuntsData = deserialized.Select(kv =>
        {
            var patchData = kv.Value;
            var mobOrder = (patchData["MobOrder"] as IList<JToken>)!.Select(token => token.ToString())
                .ToImmutableList();
            var maps = (patchData["Maps"] as IDictionary<string, JToken?>)!.Select(kv =>
            {
                var map = _mapDataManager.SpawnPointsList
                    .Find(spawnPoint => string.Equals(spawnPoint.MapName, kv.Key, StringComparison.CurrentCultureIgnoreCase));
                var spawnPoints = (kv.Value as IDictionary<string, JToken>)!
                    .Select(kv =>
                    {
                        var pos = (kv.Value as JArray)!.Select(v => (float)v).ToList();
                        return (new Vector2(pos[0], pos[1]), kv.Key);
                    })
                    .ToImmutableList();

                return new SirenHuntsMapData(map!.MapID, spawnPoints);
            }).ToImmutableDictionary(mapData => mapData.MapID);

            return new SirenHuntsPatchData(kv.Key, mobOrder, maps);
        }).ToImmutableDictionary(patchData => patchData.Name);
    }
}