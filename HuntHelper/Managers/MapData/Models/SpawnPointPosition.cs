using Newtonsoft.Json;
using System.Numerics;

namespace HuntHelper.Managers.MapData.Models;

public class SpawnPointPosition
{
    [JsonIgnore]
    public Vector2 Position { get; init; }
    public bool Taken { get; set; }

    public float X { get; set; }
    public float Y { get; set; }


    [JsonConstructor]
    public SpawnPointPosition(float x, float y, bool taken)
    {
        Position = new Vector2(x, y);
        X = x;
        Y = y;
        Taken = taken;
    }
}