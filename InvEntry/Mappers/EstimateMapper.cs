using System;
using System.Collections.Generic;
using System.Linq;
using InvEntry.Models;
using InvEntry.Models.Parsed;

namespace InvEntry.Mappers
{
    public static class EstimateMapper
    {
        public static EstimateHeader ToEstimateHeader(ParsedEstimateHeader parsed)
        {
            return new EstimateHeader
            {
                EstNbr = parsed.EstNbr,
                EstDate = parsed.InvDate,
                EstNotes = parsed.EstNotes,
                GrossRcbAmount = parsed.GrossRcbAmount
            };
        }

        public static List<EstimateLine> ToEstimateLines(List<ParsedEstimateLine> parsedLines)
        {
            return parsedLines.Select(p => new EstimateLine
            {
                EstLineNbr = p.SlNo,
                HsnCode = p.HsnCode,
                ProductName = p.ProductName,
                ProductPurity = p.Purity,
                ProdQty = p.Quantity,
                ProdGrossWeight = p.GrossWeight
            }).ToList();
        }
    }
}

