using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
