using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Services.Printing
{

    public sealed record LabelPrintRequest(
        string ProductCode,
        string ProductName,
        decimal VaPercent,
        decimal ProductWeight,
        decimal StoneWeight,
        string ProductPurity,
        string CompanyName);
}