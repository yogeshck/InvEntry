using InvEntry.ViewModels.Invoices;

namespace InvEntry.Test;

[TestFixture]
public sealed class InvoiceSettlementPaymentValidationTests
{
    [Test]
    public void AddPayment_WithNoRows_AddsOneRow()
    {
        var model = Settlement();

        model.AddReceiptCommand.Execute(null);

        Assert.That(model.Receipts, Has.Count.EqualTo(1));
    }

    [TestCase(null, 0)]
    [TestCase("Cash", 0)]
    [TestCase(null, 100)]
    public void AddPayment_WithIncompleteRow_DoesNotAddAnother(
        string? paymentMode,
        decimal amount)
    {
        var model = Settlement();
        var incomplete = new InvEntry.Models.Settlements.InvoiceSettlementLine
        {
            PaymentMode = paymentMode,
            Amount = amount
        };
        model.Receipts.Add(incomplete);

        model.AddReceiptCommand.Execute(null);

        Assert.Multiple(() =>
        {
            Assert.That(model.Receipts, Has.Count.EqualTo(1));
            Assert.That(model.ReceiptRequiringCompletion, Is.SameAs(incomplete));
            Assert.That(model.ValidationMessage, Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public void AddPayment_WithValidRow_AddsNextRow()
    {
        var model = Settlement();
        model.Receipts.Add(new() { PaymentMode = "Cash", Amount = 100 });

        model.AddReceiptCommand.Execute(null);

        Assert.That(model.Receipts, Has.Count.EqualTo(2));
    }

    [Test]
    public void AddPayment_WithValidAndBlankRows_DoesNotAddThirdRow()
    {
        var model = Settlement();
        model.Receipts.Add(new() { PaymentMode = "Cash", Amount = 100 });
        model.Receipts.Add(new());

        model.AddReceiptCommand.Execute(null);

        Assert.That(model.Receipts, Has.Count.EqualTo(2));
    }

    [Test]
    public void AddPayment_WithMultipleValidRows_AddsOneRow()
    {
        var model = Settlement();
        model.Receipts.Add(new() { PaymentMode = "Cash", Amount = 40 });
        model.Receipts.Add(new() { PaymentMode = "Bank", Amount = 60 });

        model.AddReceiptCommand.Execute(null);

        Assert.That(model.Receipts, Has.Count.EqualTo(3));
    }

    [Test]
    public void Finalise_WithIncompleteRow_IsRejectedAndRowIsRetained()
    {
        var model = Settlement();
        var incomplete = new InvEntry.Models.Settlements.InvoiceSettlementLine();
        model.Receipts.Add(incomplete);

        var result = model.ValidateForFinalise();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(model.Receipts, Has.Count.EqualTo(1));
            Assert.That(model.Receipts[0], Is.SameAs(incomplete));
            Assert.That(model.ValidationMessage, Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public void Finalise_WithValidPayment_RemainsSuccessful()
    {
        var model = Settlement();
        model.Receipts.Add(new() { PaymentMode = "Cash", Amount = 100 });

        Assert.That(model.ValidateForFinalise(), Is.True);
    }

    private static InvoiceSettlementViewModel Settlement()
    {
        return new InvoiceSettlementViewModel
        {
            InvoiceAmount = 100,
            NetSettlementAmount = 100
        };
    }
}
