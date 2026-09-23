using InvEntry.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace InvEntry.Services.Printing;

public sealed class SimulatedLabelPrinter : ILabelPrinter
{
    public SimulatedPrintOutcome Outcome { get; set; } = SimulatedPrintOutcome.Success;

    public LabelPrintRequest? LastRequest { get; private set; }
    public string? LastZpl { get; private set; }

    public Task<LabelPrintResult> PrintAsync(
        LabelPrintRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastRequest = request;
        LastZpl = BarCodePrint.CreateZpl(
            request.ProductCode,
            request.ProductName,
            request.VaPercent,
            request.ProductWeight,
            request.StoneWeight,
            request.ProductPurity,
            request.CompanyName);

        var result = Outcome switch
        {
            SimulatedPrintOutcome.Success =>
                new LabelPrintResult(LabelPrintStatus.Submitted),

            SimulatedPrintOutcome.Failure =>
                new LabelPrintResult(
                    LabelPrintStatus.Failed,
                    "Simulated label printing failure."),

            SimulatedPrintOutcome.Exception =>
                throw new InvalidOperationException(
                    "Simulated label printer is unavailable."),

            _ => new LabelPrintResult(
                LabelPrintStatus.Failed,
                "Unknown simulated printing error.")
        };

        return Task.FromResult(result);
    }
}

public enum SimulatedPrintOutcome
{
    Success,
    Failure,
    Exception
}
