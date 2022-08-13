namespace HuntHelper.Managers.Counters.EW;

public class RuinatorCounter : CounterBase
{
    public RuinatorCounter() : base(Constants.Ruinator)
    {
        MapID = (ushort)HuntHelper.MapID.MareLamentorum;
        RegexPattern = Constants.RuinatorRegex;
    }
}