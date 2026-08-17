using InvEntry.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InvEntry.Services.Printing
{

    public sealed class ZplLabelPrinter : ILabelPrinter
    {
        public Task<LabelPrintResult> PrintAsync(
            LabelPrintRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = BarCodePrint.ProcessBarCode(
                request.ProductCode,
                request.ProductName,
                request.VaPercent,
                request.ProductWeight,
                request.StoneWeight,
                request.ProductPurity,
                request.CompanyName);

            return Task.FromResult(result);
        }

    }
}