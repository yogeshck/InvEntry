using InvEntry.Mappers.Invoices;
using InvEntry.Reports;
using InvEntry.ViewModels.Invoices;
using DevExpress.DataAccess.Sql;
using DevExpress.XtraReports.UI;
using System.Reflection;

namespace InvEntry.Test;

[TestFixture]
public sealed class InvoiceOldMetalSettlementTests
{
    [TestCase(0, 50000, false, false)]
    [TestCase(30000, 20000, true, false)]
    [TestCase(50000, 0, false, false)]
    [TestCase(60000, -10000, false, true)]
    public void OldPurchase_AdjustsSettlementButNotInvoice(
        decimal oldPurchase,
        decimal expectedSettlement,
        bool isReceivable,
        bool isRefund)
    {
        var model = Settlement(50000, oldPurchase);

        Assert.Multiple(() =>
        {
            Assert.That(model.InvoiceAmount, Is.EqualTo(50000));
            Assert.That(model.OldPurchaseAdjustment, Is.EqualTo(oldPurchase));
            Assert.That(
                model.OldPurchaseAppliedToInvoice,
                Is.EqualTo(Math.Min(50000M, oldPurchase)));
            Assert.That(model.AmountAfterDiscount, Is.EqualTo(expectedSettlement));
            Assert.That(model.IsReceivable, Is.EqualTo(isReceivable || oldPurchase == 0));
            Assert.That(model.IsRefund, Is.EqualTo(isRefund));
        });
    }

    [Test]
    public void Discount_ReducesSaleBeforeOldPurchaseSettlement()
    {
        var model = Settlement(50000, 60000);
        model.DiscountAmount = 2000;

        Assert.Multiple(() =>
        {
            Assert.That(model.InvoiceAmount - model.DiscountAmount, Is.EqualTo(48000));
            Assert.That(model.OldPurchaseAppliedToInvoice, Is.EqualTo(48000));
            Assert.That(model.AmountAfterDiscount, Is.EqualTo(-12000));
            Assert.That(model.RefundPayable, Is.EqualTo(12000));
        });
    }

    [Test]
    public void GoldAndSilver_AreCombinedOnce()
    {
        var model = new InvoiceSettlementViewModel
        {
            InvoiceAmount = 50000,
            OldGoldAdjustment = 20000,
            OldSilverAdjustment = 15000,
            NetSettlementAmount = 15000
        };

        Assert.Multiple(() =>
        {
            Assert.That(model.OldPurchaseAdjustment, Is.EqualTo(35000));
            Assert.That(model.OldPurchaseAppliedToInvoice, Is.EqualTo(35000));
            Assert.That(model.AmountAfterDiscount, Is.EqualTo(15000));
        });
    }

    [Test]
    public void AdvanceAndRd_AdjustRemainingReceivableWithoutDoubleDeduction()
    {
        var model = Settlement(50000, 30000);
        model.Receipts.Add(new() { PaymentMode = "Advance Adj", Amount = 5000 });
        model.Receipts.Add(new() { PaymentMode = "RD Adj", Amount = 2000 });
        model.Receipts.Add(new() { PaymentMode = "Cash", Amount = 13000 });

        Assert.Multiple(() =>
        {
            Assert.That(model.ReceivableAmount, Is.EqualTo(20000));
            Assert.That(model.TotalAdjustments, Is.EqualTo(7000));
            Assert.That(model.TotalReceived, Is.EqualTo(13000));
            Assert.That(model.ReceivableBalance, Is.Zero);
            Assert.That(model.CanFinalise, Is.True);
        });
    }

    [Test]
    public void Refund_RemainsIncompleteUntilExplicitPaymentIsEntered()
    {
        var model = Settlement(50000, 60000);

        Assert.That(model.CanFinalise, Is.False);

        model.Refunds.Add(new() { PaymentMode = "Cash", Amount = 10000 });

        Assert.Multiple(() =>
        {
            Assert.That(model.TotalRefunded, Is.EqualTo(10000));
            Assert.That(model.CanFinalise, Is.True);
        });
    }

    [Test]
    public void FinaliseRequest_CarriesPersistedOldPurchaseCheckValue()
    {
        var model = Settlement(50000, 60000);
        model.Refunds.Add(new() { PaymentMode = "Bank", Amount = 10000 });

        var request = InvoiceRequestMapper.ToFinaliseRequest(42, model);

        Assert.Multiple(() =>
        {
            Assert.That(request.InvoiceGkey, Is.EqualTo(42));
            Assert.That(request.OldPurchaseAdjustment, Is.EqualTo(60000));
            Assert.That(request.Refunds.Single().Amount, Is.EqualTo(10000));
        });
    }

    [Test]
    public void OldPurchaseChanges_DoNotAlterSaleTaxOrInvoiceValue()
    {
        var withoutOldPurchase = InvoiceSaleAmountsCalculator.Calculate(
            48543.69M, 1.5M, 1.5M, 0M, 0M);
        var withOldPurchase = InvoiceSaleAmountsCalculator.Calculate(
            48543.69M, 1.5M, 1.5M, 0M, 0M);

        var firstSettlement = Settlement(withoutOldPurchase.GrossInvoiceAmount, 0M);
        var secondSettlement = Settlement(withOldPurchase.GrossInvoiceAmount, 60000M);

        Assert.Multiple(() =>
        {
            Assert.That(withOldPurchase.TaxableAmount, Is.EqualTo(withoutOldPurchase.TaxableAmount));
            Assert.That(withOldPurchase.CgstAmount, Is.EqualTo(withoutOldPurchase.CgstAmount));
            Assert.That(withOldPurchase.SgstAmount, Is.EqualTo(withoutOldPurchase.SgstAmount));
            Assert.That(withOldPurchase.GrossInvoiceAmount, Is.EqualTo(withoutOldPurchase.GrossInvoiceAmount));
            Assert.That(secondSettlement.InvoiceAmount, Is.EqualTo(firstSettlement.InvoiceAmount));
            Assert.That(secondSettlement.AmountAfterDiscount, Is.EqualTo(-10000M));
        });
    }

    [Test]
    public void PersistedAdjustment_UsesConfiguredOmPurchaseConventionAndInvoiceReference()
    {
        var invoice = new DataAccess.Models.InvoiceHeader
        {
            Gkey = 42,
            InvNbr = "INV-42",
            InvDate = new DateTime(2026, 9, 27),
            CustGkey = 7,
            AmountPayable = 50000M
        };

        var voucher = InvokePrivate<DataAccess.Models.Voucher>(
            "CreateOldPurchaseAdjustmentVoucher",
            invoice,
            50000M,
            1,
            "OM000123");

        var receipt = InvokePrivate<DataAccess.Models.InvoiceArReceipt>(
            "CreateOldPurchaseAdjustmentReceipt",
            invoice,
            50000M,
            1,
            50000M,
            0M,
            "OM000123");

        Assert.Multiple(() =>
        {
            Assert.That(voucher.TransType, Is.EqualTo("Journal"));
            Assert.That(voucher.VoucherType, Is.EqualTo("OM Purchase"));
            Assert.That(voucher.TransAmount, Is.EqualTo(50000M));
            Assert.That(voucher.RefDocGkey, Is.EqualTo(42));
            Assert.That(voucher.RefDocNbr, Is.EqualTo("INV-42"));
            Assert.That(voucher.TransDesc, Does.Contain("OM000123"));

            Assert.That(receipt.InvoiceGkey, Is.EqualTo(42));
            Assert.That(receipt.InvoiceReceivableAmount, Is.EqualTo(50000M));
            Assert.That(receipt.AdjustedAmount, Is.EqualTo(50000M));
            Assert.That(receipt.BalBeforeAdj, Is.EqualTo(50000M));
            Assert.That(receipt.BalanceAfterAdj, Is.Zero);
            Assert.That(receipt.TransactionType, Is.EqualTo("OM Purchase"));
            Assert.That(receipt.OtherReference, Is.EqualTo("OM000123"));
        });
    }

    [Test]
    public void ReceiptSummary_UsesFullPersistedOldPurchaseAndActualRefundVoucher()
    {
        using var report = new InvPrint25();
        var dataSourceField = typeof(InvPrint25).GetField(
            "sqlDataSource1",
            BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new AssertionException("Invoice report data source was not found.");
        var dataSource = (SqlDataSource)(dataSourceField.GetValue(report)
            ?? throw new AssertionException("Invoice report data source was null."));

        var receiptQuery = dataSource.Queries
            .OfType<CustomSqlQuery>()
            .Single(x => x.Name == "INVOICE_AR_RECEIPTS")
            .Sql;

        Assert.Multiple(() =>
        {
            Assert.That(receiptQuery, Does.Contain("OLD_GOLD_AMOUNT"));
            Assert.That(receiptQuery, Does.Contain("OLD_SILVER_AMOUNT"));
            Assert.That(receiptQuery, Does.Contain("then 'Refund to Customer'"));
            Assert.That(receiptQuery, Does.Contain("from \"dbo\".\"VOUCHER\""));
            Assert.That(receiptQuery, Does.Contain("\"voucher_type\" = 'Refund'"));
            Assert.That(receiptQuery, Does.Contain("'OM Purchase' as \"transaction_type\""));
            Assert.That(receiptQuery, Does.Contain("OLD_METAL_TRANSACTION"));
            Assert.That(receiptQuery, Does.Contain(
                "not in ('Discount','Credit','OM Purchase')"));
            Assert.That(receiptQuery, Does.Contain("1 as \"sort_order\""));
            Assert.That(receiptQuery, Does.Contain("order by \"sort_order\""));
        });
    }

    [Test]
    public void InvoiceReport_UsesSaleTaxableAmountAndNetsRefundInSummary()
    {
        using var report = new InvPrint25();
        var taxableCell = GetPrivateField<XRTableCell>(report, "taxableAmt");
        var totalLabel = GetPrivateField<XRLabel>(report, "RctTotalLbl");
        var taxableExpression = taxableCell.ExpressionBindings
            .Single(x => x.PropertyName == "Text")
            .Expression;
        var dataSource = GetPrivateField<SqlDataSource>(report, "sqlDataSource1");
        var totalQuery = dataSource.Queries
            .OfType<CustomSqlQuery>()
            .Single(x => x.Name == "InvRctSumry")
            .Sql;

        Assert.Multiple(() =>
        {
            Assert.That(taxableExpression, Is.EqualTo("[INV_TAXABLE_AMOUNT]"));
            Assert.That(taxableExpression, Does.Not.Contain("OLD_GOLD_AMOUNT"));
            Assert.That(taxableExpression, Does.Not.Contain("OLD_SILVER_AMOUNT"));
            Assert.That(totalLabel.Text, Is.EqualTo("Net Settlement : "));
            Assert.That(totalQuery, Does.Contain(
                "then -\"INVOICE_AR_RECEIPTS\".\"adjusted_amount\""));
        });
    }

    private static T InvokePrivate<T>(string methodName, params object?[] arguments)
    {
        var method = typeof(DataAccess.Workflows.InvoiceWorkflow).GetMethod(
            methodName,
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new AssertionException($"Method {methodName} was not found.");

        return (T)(method.Invoke(null, arguments)
            ?? throw new AssertionException($"Method {methodName} returned null."));
    }

    private static T GetPrivateField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(
            fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new AssertionException($"Field {fieldName} was not found.");

        return (T)(field.GetValue(instance)
            ?? throw new AssertionException($"Field {fieldName} was null."));
    }

    private static InvoiceSettlementViewModel Settlement(
        decimal invoiceAmount,
        decimal oldPurchase)
    {
        return new InvoiceSettlementViewModel
        {
            InvoiceAmount = invoiceAmount,
            OldGoldAdjustment = oldPurchase,
            OldSilverAdjustment = 0M,
            NetSettlementAmount = invoiceAmount - oldPurchase
        };
    }
}
