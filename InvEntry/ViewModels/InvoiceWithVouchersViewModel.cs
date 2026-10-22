using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Core;
using InvEntry.Contracts.Invoices;
using InvEntry.Extension;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.Utils.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

using IDialogService = DevExpress.Mvvm.IDialogService;
using System.ComponentModel;

namespace InvEntry.ViewModels;

public partial class InvoiceWithVouchersViewModel : ObservableObject
{
    [ObservableProperty]
    public ObservableCollection<Customer> _osCustomers;

    [ObservableProperty]
    public ObservableCollection<InvoiceLine> _invoiceItems;

    [ObservableProperty]
    public ObservableCollection<InvoiceArReceipt> _invReceipts;

    [ObservableProperty]
    private ObservableCollection<InvoiceHeader> _osInvoices;

    //[ObservableProperty]
    //private ObservableCollection<MtblReference> _mtblReferencesList;

    [ObservableProperty]
    private ObservableCollection<string> paymentModeList;

    [ObservableProperty]
    private Customer _selectedCustomer;

    [ObservableProperty]
    private InvoiceHeader _selectedInvoice;

    [ObservableProperty]
    private DateTime invoiceDate = DateTime.Now;

    [ObservableProperty]
    private DateSearchOption _searchOption;

    [ObservableProperty]
    private decimal customerOutstanding;

    [ObservableProperty]
    private DateTime _paidOn = DateTime.Today;

    [ObservableProperty]
    private DateTime _Today = DateTime.Today;

    private readonly ICustomerService _customerService;
    private readonly IInvoiceService _invoiceService;
    private readonly IVoucherService _voucherService;
    private readonly IInvoiceArReceiptService _invoiceArReceiptService;
    private readonly IMtblReferencesService _mtblReferencesService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly IDialogService _reportDialogService;

    public decimal TotalInvoiceAmount => (decimal)OsInvoices.Sum(i => i.GrossRcbAmount);
    public decimal TotalVoucherAmount => (decimal)OsInvoices.Sum(v => v.RecdAmount);
    public decimal BalanceAmount => (decimal)OsInvoices.Sum(v => v.InvBalance);

    //public List<InvoiceLine> Items => InvoiceItems.ToList();
    public List<string> mobileNbrs = [];

    public ICommand SaveCommand { get; }

    public InvoiceWithVouchersViewModel(IInvoiceService invoiceService,
                                        ICustomerService customerService,
                                        IVoucherService voucherService,
                                        IMtblReferencesService mtblReferencesService,
                                        IInvoiceArReceiptService invoiceArReceiptService,
                                        IMessageBoxService messageBoxService,
                                        [FromKeyedServices("ReportDialogService")] IDialogService reportDialogService)
    {
        //to do  Customers = CustomerService.LoadCustomers();

        _customerService = customerService;
        _invoiceService = invoiceService;
        _voucherService = voucherService;
        _invoiceArReceiptService = invoiceArReceiptService;
        _mtblReferencesService = mtblReferencesService;
        _messageBoxService = messageBoxService;
        _reportDialogService = reportDialogService;

        _searchOption = new();
        SearchOption.To = Today;
        SearchOption.From = Today.AddDays(-1);
        SearchOption.Filter1 = null;

        OsInvoices = new();
        mobileNbrs = new();
        OsCustomers = new();
        NewReceipts.CollectionChanged += OnNewReceiptsChanged;

        PopulateMtblRefNameList();

        Task.Run(RefreshInvoicesAsync).Wait();

        //   SaveCommand = new AsyncRelayCommand(SaveAsync);

        //    PropertyChanged += (_, e) =>
        //    {
        //       SearchOption.To = Today;
        //       SearchOption.From = Today.AddDays(-1);
        //        SearchOption.Filter1 = null; // SelectedCustomer.MobileNbr;

        //        Task.Run(RefreshInvoicesAsync).Wait();

        //if (e.PropertyName == nameof(SelectedCustomer))
        //   LoadOutstanding();

        //       OnPropertyChanged(nameof(TotalInvoiceAmount));
        //        OnPropertyChanged(nameof(TotalVoucherAmount));
        //        OnPropertyChanged(nameof(BalanceAmount));
        //    };
    }

    private async void PopulateMtblRefNameList()
    {
        var mtblRefList = await _mtblReferencesService.GetReferenceList("PAYMENT_MODE");

        // Credit and advance / RD adjustments are settled elsewhere, not collected here.
        PaymentModeList = new(mtblRefList
                                .Select(x => x.RefValue)
                                .Where(InvoiceCollectionRules.IsCollectable)
                                .OrderBy(x => x)
                             );

    }


    [RelayCommand]
    private async Task RefreshInvoicesAsync()
    {
        OsInvoices.Clear();
        mobileNbrs.Clear();

        var waitVM = WaitIndicatorVM.ShowIndicator("Please wait.... processing your request.... .");

        SplashScreenManager.CreateWaitIndicator(waitVM).Show();

        try
        {
            var OsInvlist = await _invoiceService.GetOutStanding(SearchOption);

            if (OsInvlist is not null)
            {
                foreach (var invoice in OsInvlist)
                {
                    OsInvoices.Add(invoice);
                    mobileNbrs.Add(invoice.CustMobile);
                }
            }

            if (mobileNbrs.Count > 0)
            {
                OsCustomers.Clear();
                var cust = await _customerService.GetCustomers(mobileNbrs);

                foreach (var c in cust)
                {
                    OsCustomers.Add(c);
                }
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Loading outstanding invoices failed");

            // The constructor's first load runs on a pool thread while the UI thread waits on it,
            // so only prompt when already on the UI thread (the Refresh button).
            if (Application.Current?.Dispatcher.CheckAccess() == true)
                _messageBoxService.ShowMessage("Could not load outstanding invoices. Please try Refresh again.\n\n" + ex.Message,
                                               "Outstanding Invoices", MessageButton.OK, MessageIcon.Error);
        }
        finally
        {
            SplashScreenManager.ActiveSplashScreens.FirstOrDefault(x => x.ViewModel == waitVM)?.Close();
        }
    }

    [RelayCommand]
    private async Task SelectionInvoiceChanged()
    {
        if (SelectedInvoice is null) return;

        var arReceiptsList = await _invoiceArReceiptService.GetByInvHdrGKey(SelectedInvoice.GKey, SelectedInvoice.InvNbr);

        SelectedInvoice.ReceiptLines = new((arReceiptsList ?? []).OrderBy(c => c.SeqNbr));

        SelectedCustomer = await _customerService.GetCustomer(SelectedInvoice.CustMobile);

        foreach (var cust in OsCustomers)
        {
            if (cust.MobileNbr == SelectedInvoice.CustMobile)
            {
                SelectedCustomer = cust;
            }
        }
    }

    // ---- Receipt entry grid: several receipts (cash, UPI, card, discount ...) entered
    // ---- together and saved in one go, never past the invoice balance.

    public ObservableCollection<CollectionEntryLine> NewReceipts { get; } = [];

    [ObservableProperty]
    private decimal _entryTotal;

    [ObservableProperty]
    private decimal _balanceAfterEntry;

    [ObservableProperty]
    private bool _isOverBalance;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveReceiptsCommand))]
    private bool _isSaving;

    partial void OnSelectedInvoiceChanged(InvoiceHeader value)
    {
        NewReceipts.Clear();
        RecalculateEntry();
    }

    private void OnNewReceiptsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
            foreach (CollectionEntryLine line in e.NewItems)
                line.PropertyChanged += OnEntryLineChanged;
        if (e.OldItems is not null)
            foreach (CollectionEntryLine line in e.OldItems)
                line.PropertyChanged -= OnEntryLineChanged;

        RecalculateEntry();
    }

    private void OnEntryLineChanged(object? sender, PropertyChangedEventArgs e) => RecalculateEntry();

    private void RecalculateEntry()
    {
        EntryTotal = NewReceipts.Sum(x => x.Amount);
        BalanceAfterEntry = (SelectedInvoice?.InvBalance).GetValueOrDefault() - EntryTotal;
        IsOverBalance = BalanceAfterEntry < 0;
        SaveReceiptsCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void AddReceiptLine()
    {
        if (SelectedInvoice is null) return;

        // Each new line offers whatever is still left to collect.
        NewReceipts.Add(new CollectionEntryLine
        {
            PaymentType = NewReceipts.Count == 0 ? "Cash" : null,
            Amount = Math.Max(BalanceAfterEntry, 0M)
        });
    }

    [RelayCommand]
    private void RemoveReceiptLine(CollectionEntryLine? line)
    {
        if (line is not null)
            NewReceipts.Remove(line);
    }

    private bool CanSaveReceipts() => !IsSaving && SelectedInvoice is not null && NewReceipts.Count > 0;

    [RelayCommand(CanExecute = nameof(CanSaveReceipts))]
    private async Task SaveReceipts()
    {
        var invoice = SelectedInvoice;
        if (invoice is null) return;

        var lines = NewReceipts.Select(x => x.ToModel()).ToList();
        var error = InvoiceCollectionRules.Validate(invoice.InvBalance.GetValueOrDefault(), lines);
        if (error is not null)
        {
            _messageBoxService.ShowMessage(error, "Outstanding Adjustment", MessageButton.OK, MessageIcon.Warning);
            return;
        }

        IsSaving = true;
        try
        {
            var response = await _invoiceService.SaveCollectionAsync(new SaveInvoiceCollectionRequest
            {
                InvoiceGkey = invoice.GKey,
                InvNbr = invoice.InvNbr,
                ReceivedOn = PaidOn,
                Lines = lines
            });

            invoice.InvBalance = response.InvBalance;
            invoice.RecdAmount = response.RecdAmount;
            NewReceipts.Clear();
            await SelectionInvoiceChanged();

            _messageBoxService.ShowMessage(
                $"Saved {response.VoucherNumbers.Count} receipt(s): {string.Join(", ", response.VoucherNumbers)}.\n" +
                $"Balance on {response.InvNbr} is now {response.InvBalance:N2}.",
                "Outstanding Adjustment", MessageButton.OK, MessageIcon.None);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Outstanding receipts failed for {InvNbr}", invoice.InvNbr);
            _messageBoxService.ShowMessage("The receipts were not saved; nothing was changed. Please check and try again.\n\n" + ex.Message,
                                           "Outstanding Adjustment", MessageButton.OK, MessageIcon.Error);
        }
        finally
        {
            IsSaving = false;
            RecalculateEntry();
        }
    }

    /*    private ProductView? ProductStockSelection()
    {
     
        var vm = DISource.Resolve<InvoiceProductSelectionViewModel>();
        vm.Category = ProductIdUI;

        var result = _dialogService.ShowDialog(MessageButton.OKCancel, "Product", 
                                                        "InvoiceProductSelectionView", vm);

        if (result == MessageResult.OK)
        {
            return vm.SelectedProduct; 
        } 
        return null;
    }*/

    [RelayCommand] //(CanExecute = nameof(CanPrintInvoice))]
    private void PrintInvoice()
    {
        _reportDialogService.PrintPreview(SelectedInvoice.InvNbr);

    }

    [RelayCommand]
    private void SelectionChanged()
    {
        PrintInvoiceCommand.NotifyCanExecuteChanged();
    }

    // private bool ProcessInvBalance()
    // {

    // ProcessSettlements();

    //Note if inv balance is greater than zero - we need to show message to get confirmation from user
    // and warn to check there is unpaid balance........ 

    //        if (Header.InvBalance > 0)
    /*        {
                var result = _messageBoxService.ShowMessage("Received Amount is lesser than the Invoice Amount, " +
                    "Do you want to make Credit for the balance Invoice Amount of Rs. " + Header.InvBalance + " ?", "Invoice", MessageButton.YesNo, MessageIcon.Question, MessageResult.No);

                if (result == MessageResult.Yes)
                {
                    Header.PaymentDueDate = Header.InvDate.Value.AddDays(7);
                    Header.InvRefund = 0M;
                    BalanceVisible();
                    SetReceipts("Credit");
                }
                else
                {
                    return false;
                }
            }
            else if (Header.InvBalance == 0)
            {
                Header.PaymentDueDate = null;
                BalanceVisible();

            }
            else if (Header.InvBalance < 0)
            {
                var result = _messageBoxService.ShowMessage("Received Amount is more than Invoice Amount, " +
                    "Do you want to Refund excess Amount of Rs. " + Header.InvBalance + " ?", "Invoice", MessageButton.YesNo, MessageIcon.Question, MessageResult.No);

                if (result == MessageResult.Yes)
                {*/
    /*                Header.PaymentDueDate = null;
                    Header.InvRefund = Header.InvBalance * -1;
                    Header.InvBalance = 0M;
                    RefundVisible();
                    SetReceipts("Refund");*/
    /*            }
                else
                {
                    return false;
                }

            }

            return true;
        }*/
    ////////////////////

    private async Task SaveAsync()
    {
        if (CustomerOutstanding > 0)
        {
            var confirm = MessageBox.Show(
                $"Customer has ₹{CustomerOutstanding} due. Continue?",
                "Outstanding Alert", MessageBoxButton.YesNo);

            if (confirm != MessageBoxResult.Yes)
                return;
        }

        var invoice = new InvoiceHeader
        {
            CustMobile = SelectedCustomer.CustomerName,
            InvDate = InvoiceDate,
            //    GrossRcbAmount = TotalInvoiceAmount,
            //    Lines = InvoiceItems
        };

        /*        var vouchers = VoucherEntries.Select(v => new Voucher
                {
                    VoucherType = v.VoucherType,
                    TransAmount = v.TransAmount,
                    TransDesc = v.TransDesc,
                    VoucherDate = DateTime.Now,
                    CustomerGkey = SelectedCustomer.GKey
                }).ToList();*/

        // await InvoiceService.SaveInvoiceWithVouchersAsync(invoice, vouchers);

        MessageBox.Show("Invoice and vouchers saved.");
        InvoiceItems.Clear();
        //VoucherEntries.Clear();
    }


}
