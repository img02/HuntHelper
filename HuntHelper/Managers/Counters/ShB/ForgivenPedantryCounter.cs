namespace HuntHelper.Managers.Counters.ShB;

public class ForgivenPedantryCounter : CounterBase
{
    public ForgivenPedantryCounter() : base(Constants.ForgivenPedantry)
    {
        MapID = (ushort)HuntHelper.MapID.Kholusia;
        RegexPattern = Constants.ForgivenPedantryRegex;
    }
}