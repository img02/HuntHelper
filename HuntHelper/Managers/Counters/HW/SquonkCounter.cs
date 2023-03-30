namespace HuntHelper.Managers.Counters.HW;

public class SquonkCounter : CounterBase
{
    public SquonkCounter() : base(Constants.Squonk)
    {
        MapID = (ushort)HuntHelper.MapID.TheSeaofClouds;
        RegexPattern = Constants.SquonkRegex;
    }
}