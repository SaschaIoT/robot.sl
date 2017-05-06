using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace robot.sl.Helper
{
    public static class DecimalExtension
    {
        public static int SafeConvertToInt32(this long value)
        {
            if (value <= int.MinValue)
            {
                return int.MinValue;   
            }
            else if (value >= int.MaxValue)
            {
                return int.MaxValue;
            }

            return Convert.ToInt32(value);
        }
    }
}
