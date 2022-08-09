using System;
using System.Numerics;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Newtonsoft.Json;

namespace HuntHelper.Managers.Hunts.Models;

public class HuntTrainMob
{
    public string Name { get; init; }
    public string MapName { get; init; }
    public DateTime LastSeenUTC { get; init; }
    public Vector2 Position { get; set; }
    [JsonIgnore]
    public SeString MapLink { get; init; }
    public bool Dead { get;  set; }

    [JsonConstructor] //only display name, map- maybe lastseen as tooltip?       >>>> or to the side and minute since last seen XXm ago. <<<gud idea
    public HuntTrainMob(string name, string mapName, Vector2 position, DateTime lastSeenUTC, bool dead = false)
    {
        Name = name;
        MapName = mapName;
        Position = position;
        LastSeenUTC = lastSeenUTC;
        Dead = dead;

        MapLink = SeString.CreateMapLink(mapName, position.X, position.Y)!;
    }


    /// <summary>
    /// returns the time elapsed, in minutes, since the mob was last seen.
    /// </summary>
    /// <returns></returns>
    public int TimeSinceLastSeen()
    {
        return 1;
    }

    /// <summary>
    /// marks the mob as dead
    /// </summary>
    public void MarkDead()
    {
        Dead = true;
    }
}