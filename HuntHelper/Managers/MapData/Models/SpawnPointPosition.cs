using System.Numerics;
using Newtonsoft.Json;

namespace HuntHelper.Managers.MapData.Models;

public class SpawnPointPosition
{
    [JsonIgnore]
    public Vector2 Position { get; init; }
    public bool Taken { get; set; }
    

    [JsonConstructor]
    public SpawnPointPosition(float X, float Y, bool taken)
    {
        Position = new Vector2(X,Y);
        Taken = taken;
    }
}