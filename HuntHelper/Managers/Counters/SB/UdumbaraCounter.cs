namespace HuntHelper.Managers.Counters.SB;

public class UdumbaraCounter : CounterBase
{
    public UdumbaraCounter() : base(Constants.Udumbara)
    {
        MapID = (ushort)HuntHelper.MapID.TheFringes;
        RegexPattern = Constants.UdumbaraRegex;
    }
}