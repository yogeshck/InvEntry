using InvEntry.Models.Settlements;
using InvEntry.ViewModels.Invoices;

namespace InvEntry.Test;

[TestFixture]
public sealed class InvoiceSettlementAdvanceAccountTests
{
    private static readonly AdvanceAccountOption Advance = new()
    {
        AdvanceAccountGkey = 7,
        AccountNbr = "ADV-000007",
        AccountType = "OPEN",
        SettlementMode = "Advance Adj",
        BalanceAmount = 6000M
    };

    private static readonly AdvanceAccountOption OrderAdvance = new()
    {
        AdvanceAccountGkey = 8,
        AccountNbr = "ADV-000008",
        AccountType = "ORDER",
        SettlementMode = "Advance Adj",
        BalanceAmount = 2000M,
        CustomerOrderNbr = "CO-00012"
    };

    private static readonly AdvanceAccountOption Rd = new()
    {
        AdvanceAccountGkey = 9,
        AccountNbr = "RD-000009",
        AccountType = "SCHEME",
        SettlementMode = "RD Adj",
        BalanceAmount = 12000M
    };

    [Test]
    public void Single_matching_account_is_selected_automatically()
    {
        var model = Settlement(Advance, Rd);
        var line = new InvoiceSettlementLine { PaymentMode = "RD Adj", Amount = 5000M };

        model.Receipts.Add(line);

        Assert.Multiple(() =>
        {
            Assert.That(line.AccountOptions, Is.EqualTo(new[] { Rd }));
            Assert.That(line.AdvanceAccountGkey, Is.EqualTo(Rd.AdvanceAccountGkey));
        });
    }

    [Test]
    public void Several_matching_accounts_must_be_chosen()
    {
        var model = Settlement(Advance, OrderAdvance, Rd);
        model.Receipts.Add(new() { PaymentMode = "Cash", Amount = 5000M });
        var line = new InvoiceSettlementLine { PaymentMode = "Advance Adj", Amount = 5000M };

        model.Receipts.Add(line);

        Assert.Multiple(() =>
        {
            Assert.That(line.AccountOptions, Is.EquivalentTo(new[] { Advance, OrderAdvance }));
            Assert.That(line.AdvanceAccountGkey, Is.Null);
            Assert.That(model.ValidateForFinalise(), Is.False);
            Assert.That(model.ReceiptRequiringCompletion, Is.SameAs(line));
            Assert.That(model.ValidationMessage, Does.Contain("Select the account"));
        });

        line.AdvanceAccountGkey = Advance.AdvanceAccountGkey;

        Assert.That(model.ValidateForFinalise(), Is.True);
    }

    [Test]
    public void Changing_the_mode_clears_an_account_that_no_longer_matches()
    {
        var model = Settlement(Advance, Rd);
        var line = new InvoiceSettlementLine { PaymentMode = "Advance Adj", Amount = 1000M };
        model.Receipts.Add(line);

        line.PaymentMode = "Cash";

        Assert.Multiple(() =>
        {
            Assert.That(line.AdvanceAccountGkey, Is.Null);
            Assert.That(line.AccountOptions, Is.Empty);
        });

        line.PaymentMode = "RD Adj";

        Assert.That(line.AdvanceAccountGkey, Is.EqualTo(Rd.AdvanceAccountGkey));
    }

    [Test]
    public void Using_more_than_the_account_balance_is_blocked()
    {
        var model = Settlement(Advance);
        model.Receipts.Add(new() { PaymentMode = "Advance Adj", Amount = 7000M });
        model.Receipts.Add(new() { PaymentMode = "Cash", Amount = 3000M });

        Assert.Multiple(() =>
        {
            Assert.That(model.ValidateForFinalise(), Is.False);
            Assert.That(model.ValidationMessage, Does.Contain("Only ₹6,000.00 is available in ADV-000007"));
        });
    }

    [Test]
    public void Two_lines_on_the_same_account_are_checked_together()
    {
        var model = Settlement(Advance);
        model.Receipts.Add(new() { PaymentMode = "Advance Adj", Amount = 4000M });
        model.Receipts.Add(new() { PaymentMode = "Advance Adj", Amount = 4000M });
        model.Receipts.Add(new() { PaymentMode = "Cash", Amount = 2000M });

        Assert.That(model.ValidateForFinalise(), Is.False);
    }

    [Test]
    public void Customer_without_an_rd_balance_cannot_use_rd_adj()
    {
        var model = Settlement(Advance);
        model.Receipts.Add(new() { PaymentMode = "RD Adj", Amount = 1000M });
        model.Receipts.Add(new() { PaymentMode = "Cash", Amount = 9000M });

        Assert.Multiple(() =>
        {
            Assert.That(model.ValidateForFinalise(), Is.False);
            Assert.That(model.ValidationMessage, Is.EqualTo("This customer has no RD balance to adjust."));
        });
    }

    [Test]
    public void Accounts_loaded_after_lines_exist_are_applied_to_those_lines()
    {
        var model = Settlement();
        var line = new InvoiceSettlementLine { PaymentMode = "Advance Adj", Amount = 1000M };
        model.Receipts.Add(line);

        model.LoadAdvanceAccounts(new[] { Advance });

        Assert.That(line.AdvanceAccountGkey, Is.EqualTo(Advance.AdvanceAccountGkey));
    }

    [Test]
    public void Valid_adjustment_with_cash_can_be_finalised()
    {
        var model = Settlement(Advance, Rd);
        model.Receipts.Add(new() { PaymentMode = "Advance Adj", Amount = 6000M });
        model.Receipts.Add(new() { PaymentMode = "RD Adj", Amount = 2000M });
        model.Receipts.Add(new() { PaymentMode = "Cash", Amount = 2000M });

        Assert.That(model.ValidateForFinalise(), Is.True, model.ValidationMessage);
    }

    [Test]
    public void Display_text_names_the_account_kind_and_balance()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Advance.DisplayText, Is.EqualTo("ADV-000007 (Advance) · ₹6,000.00"));
            Assert.That(OrderAdvance.DisplayText, Is.EqualTo("ADV-000008 (Order CO-00012) · ₹2,000.00"));
            Assert.That(Rd.DisplayText, Is.EqualTo("RD-000009 (RD) · ₹12,000.00"));
        });
    }

    private static InvoiceSettlementViewModel Settlement(params AdvanceAccountOption[] accounts)
    {
        var model = new InvoiceSettlementViewModel
        {
            InvoiceAmount = 10000M,
            NetSettlementAmount = 10000M
        };

        model.PaymentModes.Add("Cash");
        model.PaymentModes.Add("Advance Adj");
        model.PaymentModes.Add("RD Adj");
        model.LoadAdvanceAccounts(accounts);

        return model;
    }
}
