using InvEntry.Contracts.Invoices;
using NUnit.Framework;

namespace DataAccess.Tests;

public sealed class InvoiceStatusTests
{
    [Test]
    public void Completed_invoice_status_is_canonical()
    {
        Assert.That(InvoiceStatus.Finalised, Is.EqualTo("FINALISED"));
        Assert.That(InvoiceStatus.IsFinalised("FINALISED"), Is.True);
        Assert.That(InvoiceStatus.IsFinalised("finalised"), Is.True);
    }

    [TestCase("FINAL")]
    [TestCase("FINALIZED")]
    [TestCase("DRAFT")]
    [TestCase("CANCELLED")]
    [TestCase(null)]
    public void Noncanonical_or_noncompleted_status_is_not_finalised(string? status)
    {
        Assert.That(InvoiceStatus.IsFinalised(status), Is.False);
    }
}
