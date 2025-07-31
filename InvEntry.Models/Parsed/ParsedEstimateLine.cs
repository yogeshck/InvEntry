using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models.Parsed
{
    public class ParsedEstimateLine
    {
        public int? SlNo { get; set; }
        public string? HsnCode { get; set; }
        public string? ProductName { get; set; }
        public string? Purity { get; set; }
        public int Quantity { get; set; }
        public decimal? GrossWeight { get; set; }
    }
}

