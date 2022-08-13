namespace HuntHelper.Managers.Counters.SB;

public class OkinaCounter : CounterBase
{
    public OkinaCounter() : base(Constants.Okina)
    {
        MapID = (ushort)HuntHelper.MapID.TheRubySea;
        RegexPattern = Constants.OkinaRegex;
    }
}