using System;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using ImGuizmoNET;
using Newtonsoft.Json;

namespace HuntHelper.Entity;

public class Mob
{
    private BattleNpc? bNpc;

    public string Name { get; init; }
    public string Rank { get; init; }
    public uint ModelID { get; init; }

    public Vector2 Position => new Vector2(bNpc?.Position.X ?? 0, bNpc?.Position.Y ?? 0);
    public string Hpp => UpdateHpp();

    public Mob(BattleNpc mob, string rank)
    {
        bNpc = mob;
        Name = mob.Name.ToString();
        Rank = rank;
        ModelID = bNpc.NameId;
    }

    [JsonConstructor]
    public Mob(string name, string rank, uint modelId)
    {
        Name = name;
        Rank = rank;
        ModelID = modelId;
    }


    private string UpdateHpp()
    {
        if (bNpc == null) return "0%";

        var hpp = bNpc.CurrentHp / (bNpc.MaxHp * 1.0);
        return $"{Math.Round(hpp, 2)}%";
    }
}