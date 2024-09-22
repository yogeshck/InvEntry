using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace InvEntry.Utils
{
    public sealed class NumberToWords
    {
        private NumberToWords() { }

        public static string Convert(object number)
        {
            if (number is null) return string.Empty;

            if (number is decimal decimalValue)
                return Convert(System.Convert.ToInt64(decimalValue));

            if (number is long longValue)
                return Convert(longValue);

            if(number is string stringValue && long.TryParse(stringValue, out var parsedVal))
                return Convert(parsedVal);

            if (number is float floatValue)
                return Convert(System.Convert.ToInt64(floatValue));

            if (number is Int32 intValue)
                return Convert(intValue);

            try
            {
                return Convert(System.Convert.ToInt64(number));
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string Convert(long number)
        {
            if (number == 0) return "ZERO";
            if (number < 0) return "minus " + Convert(Math.Abs(number));
            string words = "";
            if ((number / 1000000) > 0)
            {
                words += Convert(number / 100000) + " LAKES ";
                number %= 1000000;
            }
            if ((number / 1000) > 0)
            {
                words += Convert(number / 1000) + " THOUSAND ";
                number %= 1000;
            }
            if ((number / 100) > 0)
            {
                words += Convert(number / 100) + " HUNDRED ";
                number %= 100;
            }

            if (number > 0)
            {
                if (words != "") words += "AND ";
                var unitsMap = new[]
                {
            "ZERO", "ONE", "TWO", "THREE", "FOUR", "FIVE", "SIX", "SEVEN", "EIGHT", "NINE", "TEN", "ELEVEN", "TWELVE", "THIRTEEN", "FOURTEEN", "FIFTEEN", "SIXTEEN", "SEVENTEEN", "EIGHTEEN", "NINETEEN"
        };
                var tensMap = new[]
                {
            "ZERO", "TEN", "TWENTY", "THIRTY", "FORTY", "FIFTY", "SIXTY", "SEVENTY", "EIGHTY", "NINETY"
        };
                if (number < 20) words += unitsMap[number];
                else
                {
                    words += tensMap[number / 10];
                    if ((number % 10) > 0) words += " " + unitsMap[number % 10];
                }
            }
            return words;
        }
    }
}
