using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    public static class IntExtensions
    {
        // Extension method to convert int to string
        public static string ToCustomString(this int number)
        {
            return number.ToString();
        }
    }
}
