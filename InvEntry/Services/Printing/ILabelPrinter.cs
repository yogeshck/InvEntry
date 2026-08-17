using InvEntry.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InvEntry.Services.Printing
{

    public interface ILabelPrinter
    {
        Task<LabelPrintResult> PrintAsync(
            LabelPrintRequest request,
            CancellationToken cancellationToken = default);
    }

}