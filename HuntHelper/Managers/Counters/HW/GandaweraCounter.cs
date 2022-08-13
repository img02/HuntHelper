namespace HuntHelper.Managers.Counters.HW;

public class GandaweraCounter : CounterBase
{
    public GandaweraCounter() : base(Constants.Gandawera)
    {
        MapID = (ushort)HuntHelper.MapID.TheChurningMists;
        RegexPattern = Constants.GandaweraRegex;
    }
}