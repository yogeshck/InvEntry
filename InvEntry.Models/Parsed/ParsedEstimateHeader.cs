using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models.Parsed
{
    public class ParsedEstimateHeader
    {
        public string? EstNbr { get; set; }
        public string? CustomerName { get; set; }
        public DateTime? InvDate { get; set; }
        public string? EstNotes { get; set; }
        public decimal? GrossRcbAmount { get; set; }
    }
}