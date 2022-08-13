namespace HuntHelper.Managers.Counters.EW;

public class SphatikaCounter : CounterBase
{
    public SphatikaCounter() : base(Constants.Sphatika)
    {
        MapID = (ushort) HuntHelper.MapID.Thavnair;
        RegexPattern = Constants.SphatikaRegex;
    }
}