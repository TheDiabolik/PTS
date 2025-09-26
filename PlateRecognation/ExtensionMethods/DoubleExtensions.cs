using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    public static class DoubleExtensions
    {
        public static string ToFormattedString(this double value, string format = "G")
        {
            return value.ToString(format);
        }
    }
}
