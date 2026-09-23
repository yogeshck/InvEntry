using InvEntry.Services.Printing;
using InvEntry.Utils;

namespace InvEntry.Test;

[TestFixture]
public class LabelPrinterSimulationTests
{
    private static readonly LabelPrintRequest Request = new(
        "GD2-0042", "Gold Ring", 12.5m, 8.250m, 0.125m, "916", "MATHA");

    [Test]
    public async Task Success_ReturnsSubmittedAndPreservesPreviewData()
    {
        var printer = new SimulatedLabelPrinter { Outcome = SimulatedPrintOutcome.Success };
        var result = await printer.PrintAsync(Request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(printer.LastRequest, Is.SameAs(Request));
            Assert.That(printer.LastZpl, Does.Contain("GD2-0042"));
            Assert.That(printer.LastZpl, Does.Contain("Gold Ring"));
            Assert.That(printer.LastZpl, Does.Contain("Purity: 916"));
        });
    }

    [Test]
    public async Task Failure_ReturnsExistingFailedResult()
    {
        var printer = new SimulatedLabelPrinter { Outcome = SimulatedPrintOutcome.Failure };
        var result = await printer.PrintAsync(Request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LabelPrintStatus.Failed));
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.Not.Empty);
        });
    }

    [Test]
    public void Exception_ModelsUnavailablePrinter()
    {
        var printer = new SimulatedLabelPrinter { Outcome = SimulatedPrintOutcome.Exception };
        Assert.That(async () => await printer.PrintAsync(Request),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public async Task RepeatedAttempt_ReusesSameSku()
    {
        var printer = new SimulatedLabelPrinter { Outcome = SimulatedPrintOutcome.Failure };
        await printer.PrintAsync(Request);
        printer.Outcome = SimulatedPrintOutcome.Success;
        var retry = await printer.PrintAsync(Request);

        Assert.Multiple(() =>
        {
            Assert.That(retry.Success, Is.True);
            Assert.That(printer.LastRequest!.ProductCode, Is.EqualTo("GD2-0042"));
            Assert.That(printer.LastZpl, Does.Contain("GD2-0042"));
        });
    }

    [Test]
    public void Factory_DefaultsToPhysicalMode()
    {
        Assert.That(LabelPrinterFactory.Create(null, null), Is.TypeOf<ZplLabelPrinter>());
        Assert.That(LabelPrinterFactory.Create("Physical", "Success"), Is.TypeOf<ZplLabelPrinter>());
    }

    [Test]
    public void Factory_CreatesConfiguredSimulationOutcome()
    {
        var printer = LabelPrinterFactory.Create("Simulation", "Failure");
        Assert.That(printer, Is.TypeOf<SimulatedLabelPrinter>());
        Assert.That(((SimulatedLabelPrinter)printer).Outcome,
            Is.EqualTo(SimulatedPrintOutcome.Failure));
    }
}
