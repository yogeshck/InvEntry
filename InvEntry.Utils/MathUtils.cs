using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Utils
{
    public class MathUtils
    {
        public static decimal Normalize(decimal? value)
                => value.HasValue ? Math.Round(value.Value, 2) : 0M;

        public static string? NormalizeRounding(string criteria)
            => string.IsNullOrEmpty(criteria) ? null : $"Round({criteria}, 2)";
    }
}
