using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Utils
{
    public class MathUtils
    {
        public static decimal Normalize(decimal? value, int precision = 2)
                => value.HasValue ? Math.Round(value.Value, precision) : 0M;

        public static string? NormalizeRounding(string criteria)
            => string.IsNullOrEmpty(criteria) ? null : $"Round({criteria}, 2)";

        public static string? NormalizeRounding(string criteria, int precision = 2)
            => string.IsNullOrEmpty(criteria) ? null : $"Round({criteria}, {precision})";
    }
}
