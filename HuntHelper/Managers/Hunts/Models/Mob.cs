using Newtonsoft.Json;

namespace HuntHelper.Managers.Hunts.Models;
//used for loading dictionaries from json data
public class Mob
{
    public string Name { get; init; }
    public string Rank { get; init; }
    public uint ModelID { get; init; }

    //plan was to allow user to disable certain mobs (like b ranks) - but you can just edit the jsons,
    //and it's such a specific low use case kind of thing --also not useful for me, so cbf.
    public bool IsEnabled { get; set; }
    
    [JsonConstructor]
    public Mob(string name, string rank, uint modelId, bool isEnabled)
    {
        Name = name;
        Rank = rank;
        ModelID = modelId;
        IsEnabled = isEnabled;
    }

}