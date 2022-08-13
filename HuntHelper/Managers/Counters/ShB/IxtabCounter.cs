namespace HuntHelper.Managers.Counters.ShB;

public class IxtabCounter : CounterBase
{
    public IxtabCounter() : base(Constants.Ixtab)
    {
        MapID = (ushort)HuntHelper.MapID.TheRaktikaGreatwood;
        RegexPattern = Constants.IxtabRegex;
    }
}