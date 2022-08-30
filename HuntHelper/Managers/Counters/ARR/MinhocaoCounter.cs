namespace HuntHelper.Managers.Counters.ARR;

public class MinhocaoCounter : CounterBase
{

    public MinhocaoCounter() : base(Constants.Minhocao)
    {
        MapID = (ushort)HuntHelper.MapID.NorthernThanalan;
        RegexPattern = Constants.MinhocaoRegex;
    }
}