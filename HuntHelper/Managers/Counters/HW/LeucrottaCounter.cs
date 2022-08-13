namespace HuntHelper.Managers.Counters.HW
{
    public class LeucrottaCounter : CounterBase
    {
        public LeucrottaCounter() : base(Constants.Leucrotta)
        {
            MapID = (ushort)HuntHelper.MapID.AzysLla;
            RegexPattern = Constants.LeucrottaRegex;
        }
    }
}
