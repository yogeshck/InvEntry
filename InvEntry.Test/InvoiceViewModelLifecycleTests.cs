using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using InvEntry.Contracts.Invoices;
using InvEntry.Models;
using InvEntry.ViewModels;

namespace InvEntry.Test;

[TestFixture]
public sealed class InvoiceViewModelLifecycleTests
{
    [Test]
    public void NewInvoiceReset_ClearsTransientStateAndRestoresDefaults()
    {
        var viewModel = CreateViewModel(new InvoiceHeader
        {
            GKey = 42,
            Status = InvoiceStatus.Draft,
            OldGoldAmount = 60000M
        });
        viewModel.Header.Lines.Add(new InvoiceLine());
        viewModel.Buyer = new Customer
        {
            GKey = 5,
            MobileNbr = "9876543210",
            CustomerName = "Customer"
        };
        viewModel.CustomerPhoneNumber = "9876543210";
        viewModel.ProductIdUI = "RING";
        viewModel.HasUnsavedChanges = true;

        viewModel.ResetInvoiceCommand.Execute(null);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.Header.GKey, Is.Zero);
            Assert.That(viewModel.Header.Status, Is.EqualTo(InvoiceStatus.Draft));
            Assert.That(viewModel.Header.Lines, Is.Empty);
            Assert.That(viewModel.Header.OldGoldAmount.GetValueOrDefault(), Is.Zero);
            Assert.That(viewModel.Header.TenantGkey, Is.EqualTo(11));
            Assert.That(viewModel.Header.GstLocSeller, Is.EqualTo("33"));
            Assert.That(viewModel.Buyer, Is.Null);
            Assert.That(viewModel.CustomerPhoneNumber, Is.Null);
            Assert.That(viewModel.ProductIdUI, Is.Null);
            Assert.That(viewModel.HasUnsavedChanges, Is.False);
            Assert.That(viewModel.FinaliseInvoiceCommand.CanExecute(null), Is.False);
        });
    }

    [Test]
    public void RetrievedEligibleDraft_EnablesFinalise()
    {
        var viewModel = CreateViewModel(new InvoiceHeader
        {
            GKey = 42,
            Status = InvoiceStatus.Draft
        });

        Assert.That(viewModel.FinaliseInvoiceCommand.CanExecute(null), Is.True);
    }

    [Test]
    public void ProgrammaticDraftLoad_WithMultipleLines_RemainsCleanAndFinalisable()
    {
        var viewModel = CreateViewModel(new InvoiceHeader());
        SetField(viewModel, "_isLoadingDraft", true);

        viewModel.Header = new InvoiceHeader
        {
            GKey = 42,
            Status = InvoiceStatus.Draft
        };
        viewModel.Header.Lines.Add(new InvoiceLine());
        viewModel.Header.Lines.Add(new InvoiceLine());

        SetField(viewModel, "_isLoadingDraft", false);
        viewModel.HasUnsavedChanges = false;

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.HasUnsavedChanges, Is.False);
            Assert.That(viewModel.Header.Lines, Has.Count.EqualTo(2));
            Assert.That(viewModel.FinaliseInvoiceCommand.CanExecute(null), Is.True);
        });
    }

    [TestCase(nameof(InvoiceLine.ProdGrossWeight))]
    [TestCase(nameof(InvoiceLine.VaPercent))]
    public void PersistedLineEdit_DisablesFinaliseImmediately(string property)
    {
        var line = new InvoiceLine
        {
            ProdGrossWeight = 10M,
            VaPercent = 1M
        };
        var header = new InvoiceHeader
        {
            GKey = 42,
            Status = InvoiceStatus.Draft
        };
        header.Lines.Add(line);
        var viewModel = CreateViewModel(header);

        if (property == nameof(InvoiceLine.ProdGrossWeight))
            line.ProdGrossWeight = 11M;
        else
            line.VaPercent = 2M;

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.HasUnsavedChanges, Is.True);
            Assert.That(viewModel.FinaliseInvoiceCommand.CanExecute(null), Is.False);
        });
    }

    [Test]
    public void CustomerOrDiscountEdit_DisablesFinaliseImmediately()
    {
        var viewModel = CreateViewModel(new InvoiceHeader
        {
            GKey = 42,
            Status = InvoiceStatus.Draft,
            CustGkey = 5,
            DiscountAmount = 0M
        });

        viewModel.Header.CustGkey = 6;
        Assert.That(viewModel.FinaliseInvoiceCommand.CanExecute(null), Is.False);

        viewModel.HasUnsavedChanges = false;
        viewModel.Header.DiscountAmount = 100M;
        Assert.That(viewModel.FinaliseInvoiceCommand.CanExecute(null), Is.False);
    }

    [Test]
    public void CustomerMobileEdit_DisablesFinaliseBeforeLookupCompletes()
    {
        var viewModel = CreateViewModel(new InvoiceHeader
        {
            GKey = 42,
            Status = InvoiceStatus.Draft,
            CustGkey = 5,
            CustMobile = "9876543210"
        });
        SetField(viewModel, "_customerPhoneNumber", "9876543210");

        viewModel.CustomerPhoneNumber = "9876543211";

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.HasUnsavedChanges, Is.True);
            Assert.That(viewModel.FinaliseInvoiceCommand.CanExecute(null), Is.False);
        });
    }

    [Test]
    public void FullyResolvedCustomer_WithOptionalAddressValues_AllowsValidation()
    {
        var header = new InvoiceHeader
        {
            CustGkey = 5,
            CustMobile = "9841012345"
        };
        var viewModel = CreateViewModel(header);
        var customer = new Customer
        {
            GKey = 5,
            MobileNbr = "9841012345",
            CustomerName = "Customer",
            Address = new OrgAddress()
        };

        SetField(viewModel, "_buyer", customer);
        SetField(viewModel, "_customerPhoneNumber", "9841012345");

        var isValid = (bool)typeof(InvoiceViewModel)
            .GetMethod(
                "TryValidateResolvedCustomer",
                BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(viewModel, null)!;

        Assert.That(isValid, Is.True);
    }

    [Test]
    public void ProgrammaticCustomerSynchronization_LeavesIdentityConsistent()
    {
        var header = new InvoiceHeader();
        var viewModel = CreateViewModel(header);
        var customer = new Customer
        {
            GKey = 5,
            MobileNbr = "stale",
            CustomerName = "Customer"
        };
        SetField(viewModel, "_buyer", customer);

        typeof(InvoiceViewModel)
            .GetMethod(
                "SynchronizeResolvedCustomer",
                BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(viewModel, ["9841012345"]);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.CustomerPhoneNumber, Is.EqualTo("9841012345"));
            Assert.That(viewModel.Buyer.MobileNbr, Is.EqualTo("9841012345"));
            Assert.That(viewModel.Header.CustMobile, Is.EqualTo("9841012345"));
            Assert.That(viewModel.Header.CustGkey, Is.EqualTo(5));
            Assert.That(viewModel.Buyer.GKey, Is.EqualTo(5));
        });
    }

    [Test]
    public void AddOrRemoveLine_DisablesFinaliseImmediately()
    {
        var header = new InvoiceHeader
        {
            GKey = 42,
            Status = InvoiceStatus.Draft
        };
        var viewModel = CreateViewModel(header);
        var line = new InvoiceLine();

        header.Lines.Add(line);
        Assert.That(viewModel.FinaliseInvoiceCommand.CanExecute(null), Is.False);

        viewModel.HasUnsavedChanges = false;
        header.Lines.Remove(line);
        Assert.That(viewModel.FinaliseInvoiceCommand.CanExecute(null), Is.False);
    }

    [Test]
    public void FinaliseCommand_ReactsToDirtyAndStatusChanges()
    {
        var viewModel = CreateViewModel(new InvoiceHeader
        {
            GKey = 42,
            Status = InvoiceStatus.Draft
        });
        var notifications = 0;
        viewModel.FinaliseInvoiceCommand.CanExecuteChanged += (_, _) => notifications++;

        viewModel.HasUnsavedChanges = true;
        var dirtyCanFinalise = viewModel.FinaliseInvoiceCommand.CanExecute(null);

        viewModel.HasUnsavedChanges = false;
        viewModel.Header = new InvoiceHeader
        {
            GKey = 42,
            Status = InvoiceStatus.Final,
            InvNbr = "B-0042"
        };

        Assert.Multiple(() =>
        {
            Assert.That(dirtyCanFinalise, Is.False);
            Assert.That(viewModel.FinaliseInvoiceCommand.CanExecute(null), Is.False);
            Assert.That(notifications, Is.GreaterThanOrEqualTo(3));
        });
    }

    private static InvoiceViewModel CreateViewModel(InvoiceHeader header)
    {
        var viewModel =
            (InvoiceViewModel)RuntimeHelpers.GetUninitializedObject(
                typeof(InvoiceViewModel));

        SetField(viewModel, "_header", new InvoiceHeader());
        SetField(viewModel, "_company", new OrgThisCompanyView
        {
            TenantGkey = 11,
            GstCode = "33",
            State = "TAMIL NADU"
        });
        SetField(
            viewModel,
            "selectedRows",
            new ObservableCollection<InvoiceLine>());

        viewModel.Header = header;

        return viewModel;
    }

    private static void SetField(
        InvoiceViewModel viewModel,
        string name,
        object? value)
    {
        typeof(InvoiceViewModel)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(viewModel, value);
    }
}
