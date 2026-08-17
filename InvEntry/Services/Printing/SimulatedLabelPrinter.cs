using InvEntry.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InvEntry.Services.Printing
{
    public sealed class SimulatedLabelPrinter : ILabelPrinter
    {
        public SimulatedPrintOutcome Outcome { get; set; } =
            SimulatedPrintOutcome.Success;

        public async Task<LabelPrintResult> PrintAsync(
            LabelPrintRequest request,
            CancellationToken cancellationToken = default)
        {
            // Simulate the time taken to print.
            await Task.Delay(800, cancellationToken);

            return Outcome switch
            {
                SimulatedPrintOutcome.Success =>
                    new LabelPrintResult(
                        LabelPrintStatus.Submitted),

                SimulatedPrintOutcome.PrinterOffline =>
                    new LabelPrintResult(
                        LabelPrintStatus.Failed,
                        "The label printer is offline."),

                SimulatedPrintOutcome.PaperOut =>
                    new LabelPrintResult(
                        LabelPrintStatus.Failed,
                        "The printer is out of labels."),

                SimulatedPrintOutcome.RibbonOut =>
                    new LabelPrintResult(
                        LabelPrintStatus.Failed,
                        "The printer ribbon is empty."),

                SimulatedPrintOutcome.InvalidZpl =>
                    new LabelPrintResult(
                        LabelPrintStatus.Failed,
                        "The generated label data is invalid."),

                SimulatedPrintOutcome.Timeout =>
                    await SimulateTimeoutAsync(cancellationToken),

                SimulatedPrintOutcome.UnexpectedException =>
                    throw new InvalidOperationException(
                        "Simulated printer communication exception."),

                _ => new LabelPrintResult(
                    LabelPrintStatus.Failed,
                    "Unknown simulated printing error.")
            };
        }

        private static async Task<LabelPrintResult> SimulateTimeoutAsync(
            CancellationToken cancellationToken)
        {
            await Task.Delay(3000, cancellationToken);

            return new LabelPrintResult(
                LabelPrintStatus.Failed,
                "The printer did not respond within the expected time.");
        }
    }


    public enum SimulatedPrintOutcome
    {
        Success,
        PrinterOffline,
        PaperOut,
        RibbonOut,
        InvalidZpl,
        Timeout,
        UnexpectedException
    }
}
