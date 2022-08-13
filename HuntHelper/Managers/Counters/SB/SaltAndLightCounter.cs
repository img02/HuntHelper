namespace HuntHelper.Managers.Counters.SB;

public class SaltAndLightCounter : CounterBase
{
    public SaltAndLightCounter() : base(Constants.SaltAndLight)
    {
        MapID = (ushort)HuntHelper.MapID.TheLochs;
        RegexPattern = Constants.SaltAndLightRegex;
    }
}