using System;

namespace InvEntry.Services.Printing;

public static class LabelPrinterFactory
{
    public static ILabelPrinter Create(string? mode, string? simulationOutcome)
    {
        if (!string.Equals(mode?.Trim(), "Simulation", StringComparison.OrdinalIgnoreCase))
            return new ZplLabelPrinter();

        var outcome = Enum.TryParse<SimulatedPrintOutcome>(
            simulationOutcome?.Trim(),
            ignoreCase: true,
            out var configuredOutcome)
            ? configuredOutcome
            : SimulatedPrintOutcome.Success;

        return new SimulatedLabelPrinter { Outcome = outcome };
    }
}
