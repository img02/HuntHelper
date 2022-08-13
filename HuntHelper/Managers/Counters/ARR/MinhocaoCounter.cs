using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;

namespace HuntHelper.Managers.Counters.ARR;

public class MinhocaoCounter : CounterBase
{
    public MinhocaoCounter() : base(Constants.Minhocao)
    {
        MapID = (ushort)HuntHelper.MapID.NorthernThanalan;
        RegexPattern = Constants.MinhocaoRegex;
    }
}