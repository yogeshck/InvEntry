using InvEntry.Contracts.Invoices;
using InvEntry.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace InvEntry.Test;

[TestFixture]
public class OutstandingVoucherReceiptGridTests
{
    [TestCase("Cash", 1500, 1500, null)]
    [TestCase("CR DISCOUNT", 250, 250, null)]
    [TestCase("Credit", 4000, null, 4000)]
    public void DebitAndCredit_SplitAdjustedAmountByTransactionType(
        string transactionType, decimal adjusted, decimal? expectedDebit, decimal? expectedCredit)
    {
        var receipt = new InvoiceArReceipt { TransactionType = transactionType, AdjustedAmount = adjusted };

        Assert.Multiple(() =>
        {
            Assert.That(receipt.Debit, Is.EqualTo(expectedDebit));
            Assert.That(receipt.Credit, Is.EqualTo(expectedCredit));
        });
    }

    [Test]
    public void CreditRowFromInvoiceWorkflow_ShowsAmountLeftOnCredit()
    {
        // As saved by InvoiceWorkFlow.CreateCreditArReceipt (D-01292).
        var receipt = new InvoiceArReceipt
        {
            TransactionType = "Credit",
            BalBeforeAdj = 33166m,
            AdjustedAmount = 0m,
            BalanceAfterAdj = 33166m
        };

        Assert.Multiple(() =>
        {
            Assert.That(receipt.Credit, Is.EqualTo(33166m));
            Assert.That(receipt.Debit, Is.Null);
        });
    }

    [TestCase("Cash", "Cash")]
    [TestCase("GPAY", "GPAY")]
    [TestCase("Credit Card", "Credit Card")]
    [TestCase("Discount", "Discount")]
    [TestCase("CR DISCOUNT", "Discount")]
    public void AdjustmentVoucher_UsesThePaymentModeNotCash(string paymentType, string expectedMode)
    {
        Assert.That(InvoiceCollectionRules.VoucherMode(paymentType), Is.EqualTo(expectedMode));
    }

    [TestCase("Credit")]
    [TestCase("Advance Adj")]
    [TestCase("RD Adj")]
    public void SettlementsFromOtherAccounts_AreNotOfferedForCollection(string paymentType)
    {
        Assert.That(InvoiceCollectionRules.IsCollectable(paymentType), Is.False);
    }

    [Test]
    public void ReceiptsUpToTheBalance_AreValid()
    {
        var lines = new[]
        {
            new InvoiceCollectionLineModel { PaymentType = "Cash", Amount = 1000m },
            new InvoiceCollectionLineModel { PaymentType = "GPAY", Amount = 834m },
            new InvoiceCollectionLineModel { PaymentType = "Discount", Amount = 166m }
        };

        Assert.That(InvoiceCollectionRules.Validate(2000m, lines), Is.Null);
    }

    [Test]
    public void ReceiptsBeyondTheBalance_AreRejected()
    {
        // D-01303: 4,000 cash against a 2,749 balance left 1,251 unaccounted for.
        var lines = new[] { new InvoiceCollectionLineModel { PaymentType = "Cash", Amount = 4000m } };

        Assert.That(InvoiceCollectionRules.Validate(2749m, lines), Does.Contain("more than the balance"));
    }

    [TestCase("", 100, "choose a payment type")]
    [TestCase("Cash", 0, "greater than zero")]
    [TestCase("Advance Adj", 100, "cannot be collected here")]
    public void IncompleteLine_IsRejectedWithItsLineNumber(string paymentType, decimal amount, string expected)
    {
        var lines = new[]
        {
            new InvoiceCollectionLineModel { PaymentType = "Cash", Amount = 100m },
            new InvoiceCollectionLineModel { PaymentType = paymentType, Amount = amount }
        };

        var error = InvoiceCollectionRules.Validate(5000m, lines);

        Assert.That(error, Does.StartWith("Line 2:").And.Contain(expected));
    }

    [Test]
    public void EntryGrid_IsEditableAndBoundToTheNewReceipts()
    {
        XNamespace grid = "http://schemas.devexpress.com/winfx/2008/xaml/grid";
        var view = XDocument.Load(FindViewXaml());

        var entryGrid = view.Descendants(grid + "GridControl")
            .Single(g => (string?)g.Attribute("ItemsSource") == "{Binding NewReceipts}");
        var fields = entryGrid.Descendants(grid + "GridColumn")
            .Select(c => (string?)c.Attribute("FieldName")).Where(x => x is not null).ToList();
        var buttons = view.Descendants().Where(e => e.Name.LocalName == "Button")
            .Select(b => (string?)b.Attribute("Command")).ToList();

        Assert.Multiple(() =>
        {
            Assert.That((string?)entryGrid.Descendants(grid + "TableView").Single().Attribute("AllowEditing"), Is.EqualTo("True"));
            Assert.That(fields, Is.EqualTo(new[] { "PaymentType", "Amount", "Reference" }));
            Assert.That(buttons, Does.Contain("{Binding AddReceiptLineCommand}"));
            Assert.That(buttons, Does.Contain("{Binding SaveReceiptsCommand}"));
            Assert.That(buttons, Does.Not.Contain("{Binding ApplyAdjustmentCommand}"));
        });
    }

    [Test]
    public void EntryLine_ChangingAnAmount_IsNotified()
    {
        var line = new InvEntry.ViewModels.CollectionEntryLine { PaymentType = "Cash" };
        var changed = RecordChanges(line);

        line.Amount = 500m;

        Assert.That(changed, Does.Contain(nameof(InvEntry.ViewModels.CollectionEntryLine.Amount)));
        Assert.That(line.ToModel().Amount, Is.EqualTo(500m));
    }

    [Test]
    public void ChangingAdjustedAmount_NotifiesDebitAndCreditColumns()
    {
        var receipt = new InvoiceArReceipt { TransactionType = "Cash" };
        var changed = RecordChanges(receipt);

        receipt.AdjustedAmount = 900m;

        Assert.That(changed, Is.SupersetOf(new[] { nameof(InvoiceArReceipt.Debit), nameof(InvoiceArReceipt.Credit) }));
        Assert.That(receipt.Debit, Is.EqualTo(900m));
    }

    [Test]
    public void ChangingTransactionType_NotifiesDebitAndCreditColumns()
    {
        var receipt = new InvoiceArReceipt { TransactionType = "Cash", AdjustedAmount = 900m };
        var changed = RecordChanges(receipt);

        receipt.TransactionType = "Credit";

        Assert.That(changed, Is.SupersetOf(new[] { nameof(InvoiceArReceipt.Debit), nameof(InvoiceArReceipt.Credit) }));
        Assert.That(receipt.Credit, Is.EqualTo(900m));
    }

    [Test]
    public void ReceiptGridColumns_AreBoundToReceiptProperties()
    {
        // An UnboundType on a column makes DevExpress ignore the row property of the same name,
        // which left the Credit column (and its total) permanently blank.
        XNamespace grid = "http://schemas.devexpress.com/winfx/2008/xaml/grid";
        var view = XDocument.Load(FindViewXaml());

        var receiptGrid = view.Descendants(grid + "GridControl")
            .Single(g => (string?)g.Attribute("ItemsSource") == "{Binding SelectedInvoice.ReceiptLines}");
        var columns = receiptGrid.Descendants(grid + "GridColumn").ToList();

        Assert.Multiple(() =>
        {
            foreach (string field in new[] { nameof(InvoiceArReceipt.Debit), nameof(InvoiceArReceipt.Credit) })
            {
                var column = columns.SingleOrDefault(c => (string?)c.Attribute("FieldName") == field);
                Assert.That(column, Is.Not.Null, $"{field} column missing");
                Assert.That(column?.Attribute("UnboundType"), Is.Null, $"{field} column must be bound");
                Assert.That(column?.Attribute("UnboundDataType"), Is.Null, $"{field} column must be bound");
            }
        });
    }

    private static List<string?> RecordChanges(INotifyPropertyChanged source)
    {
        var changed = new List<string?>();
        source.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        return changed;
    }

    private static string FindViewXaml([CallerFilePath] string testSourcePath = "")
    {
        var directory = new FileInfo(testSourcePath).Directory;
        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, "InvEntry", "Views", "InvoiceWithVouchersView.xaml");
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }
        throw new FileNotFoundException("InvEntry/Views/InvoiceWithVouchersView.xaml");
    }
}
