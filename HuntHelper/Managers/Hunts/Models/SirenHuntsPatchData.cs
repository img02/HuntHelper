using System.Collections.Generic;
using Newtonsoft.Json;

namespace HuntHelper.Managers.Hunts.Models;

public class SirenHuntsPatchData
{
    public string Name { get; init; }
    public IList<string> MobOrder { get; init; }
    public IDictionary<uint, SirenHuntsMapData> Maps { get; init; }

    [JsonConstructor]
    public SirenHuntsPatchData(string name, IList<string> mobOrder, IDictionary<uint, SirenHuntsMapData> maps)
    {
        Name = name;
        MobOrder = mobOrder;
        Maps = maps;
    }
}