using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Printing;
using InvEntry.Extension;
using InvEntry.Models;
using InvEntry.Reports;
using InvEntry.Services;
using InvEntry.Tally;
using InvEntry.Tally.Model;
using InvEntry.Utils.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using IDialogService = DevExpress.Mvvm.IDialogService;


namespace InvEntry.ViewModels;

public partial class VoucherListViewModel: ObservableObject
{
    private readonly IVoucherDbViewService _voucherDbViewService;
    private readonly IDialogService _reportDialogService;
    private readonly ITallyXMLService _xmlService;
    private readonly IMtblLedgersService _mtblLedgersService;

    private readonly IReportFactoryService _reportFactoryService;

    [ObservableProperty]
    private ObservableCollection<VoucherDbView> _vouchersView;

    [ObservableProperty]
    private DateSearchOption _searchOption;

    [ObservableProperty]
    private ObservableCollection<string> _statementTypeOptionList;

    [ObservableProperty]
    private VoucherDbView _selectedVoucher;

    [ObservableProperty]
    private DateTime _Today = DateTime.Today;

    [ObservableProperty]
    private ObservableCollection<MtblLedger> _masterLedgerList;

    public  VoucherListViewModel(IVoucherDbViewService voucherDbViewService,
            ITallyXMLService xmlService,
            IMtblLedgersService mtblLedgersService,
            IReportFactoryService reportFactoryService,
            [FromKeyedServices("ReportDialogService")] IDialogService reportDialogService)
    {
        _voucherDbViewService = voucherDbViewService;
        _reportDialogService = reportDialogService;
        _mtblLedgersService = mtblLedgersService;
        _xmlService = xmlService;
        _reportFactoryService = reportFactoryService;

        _searchOption = new();
        SearchOption.To = Today;
        SearchOption.From = Today.AddDays(-2);
        SearchOption.Filter1 ??= "Cash";

        PopulateStatmentTypeOpionList();
        //PopulateMasterLedgerList();

        Task.Run(init).Wait();
    }

    private void PopulateStatmentTypeOpionList()
    {

        StatementTypeOptionList = new();

        StatementTypeOptionList.Add("Cash");
        StatementTypeOptionList.Add("Petty Cash");

    }

    private async void init()
    {

        await PopulateMasterLedgerList();
        await RefreshVoucherAsync();
    }

    private async Task PopulateMasterLedgerList()
    {
        var masterLedgerList = await _mtblLedgersService.GetAll();
            //GetLedgerList("Indirect Expenses");  //hard-coded need to be dynamic

        if (masterLedgerList is not null)
        {
            MasterLedgerList = new(masterLedgerList);
            //AccountGroupList = new(MasterLedgerList.Select(x => x.LedgerName));
        }

    }

    private void SetOpeningBalance(string type)
    {

    }

    [RelayCommand]
    private async Task RefreshVoucherAsync()
    {
        VouchersView = new();


       // SearchOption.BookType = null;

        var vouchersResult = await _voucherDbViewService.GetAll(SearchOption);
        if (vouchersResult is not null)
        {

            foreach (var voucher in vouchersResult)
            {
            //    voucher.FromLedgerName
             //   = MasterLedgerList?.FirstOrDefault(x => x.GKey == voucher.FromLedgerGkey).LedgerName;

                VouchersView.Add(voucher);

            }
            //RecdAmount = (Vouchers.Select(x => x.TransType == "Receipt")).TransAmount;

        }    
    }

    [RelayCommand] //CanExecute = nameof(CanPrintStatement))]
    private void StatementPrint()
    {
        //var printed = PrintHelper.Print(_reportFactoryService.CreateFinStatementReport(SearchOption.From, SearchOption.To));

        var report = _reportFactoryService.CreateFinStatementReport((DateTime)SearchOption.From,
                                                (DateTime)SearchOption.To, 
                                                SearchOption.Filter1);

 //       PrintHelper.ShowPrintPreviewDialog(Application.Current.MainWindow,report);

        //if (printed.HasValue && printed.Value)
        //    _messageBoxService.ShowMessage("Estimate printed Successfully", "Estimate print", MessageButton.OK, MessageIcon.None);
    
    }

    [RelayCommand(CanExecute = nameof(CanPrintVoucher))]
    private void PrintVoucher()
    {
        var waitVM = WaitIndicatorVM.ShowIndicator("Please wait.... preparing print document.... .");

        SplashScreenManager.CreateWaitIndicator(waitVM).Show();

        _reportDialogService.PrintPreviewVoucher(SelectedVoucher.GKey);

        SplashScreenManager.ActiveSplashScreens.FirstOrDefault(x => x.ViewModel == waitVM).Close();

        PrintPreviewVoucherCommand.NotifyCanExecuteChanged();
        PrintVoucherCommand.NotifyCanExecuteChanged();
        Messenger.Default.Send(MessageType.WaitIndicator, WaitIndicatorVM.HideIndicator());

    }

    private bool CanPrintVoucher()
    {
        return SelectedVoucher is not null;
    }

    [RelayCommand(CanExecute = nameof(CanPrintVoucher))]
    private void PrintPreviewVoucher()
    {
        _reportDialogService.PrintPreviewVoucher(SelectedVoucher.GKey);
        //ResetVoucher();
    }

    [RelayCommand]
    private void SelectionChanged()
    {
        PrintVoucherCommand.NotifyCanExecuteChanged();
    }

    //[RelayCommand]
    //private async Task SendToTally()
    //{
    //    if (_SelectedVoucher is null)
    //        return;

    //    TallyMessageBuilder tallyMessageBuilder = new TallyMessageBuilder(TallyXMLMessageType.SendVoucherToTally, "MATHA THANGA MALIGAI");

    //   // TallyXmlMesage tallyXmlMsg = new TallyXmlMesage();
    //   // tallyXmlMsg.HEADER = new TallyHeader();
    //   // tallyXmlMsg.HEADER.TallyRequest = TallyRequestEnum.Import;

    //    TallyVoucher tallyVoucer = new TallyVoucher();

    //    /*
    //     * SET more values as needed to send to tally
    //     */
    //    tallyVoucer.VOUCHERNUMBER = _SelectedVoucher?.VoucherNbr;
    //    tallyVoucer.DATE = _SelectedVoucher?.VoucherDate?.ToString("yyyyMMdd");
    //    //tallyVoucer.VCHTYPE = //asdlkfjlksadjf;

    //  //  tallyXmlMsg.BODY = new TallyBody();

    //     tallyMessageBuilder.AddVoucher(tallyVoucer);

    //    await _xmlService.SendToTally(tallyMessageBuilder.Build());
    //    //await _xmlService.SendToTally(tallyXmlMsg);
    //}


public class VoucherEntry
{
    public int Gkey { get; set; }
    public int SeqNbr { get; set; }
    public string CustomerGkey { get; set; }
    public string TransType { get; set; }
    public string VoucherType { get; set; }
    public string Mode { get; set; }
    public decimal TransAmount { get; set; }
    public string VoucherNbr { get; set; }
    public DateTime VoucherDate { get; set; }
    public string RefDocNbr { get; set; }
    public DateTime? RefDocDate { get; set; }
    public string TransDesc { get; set; }
    public DateTime TransDate { get; set; }
    public decimal Recd_Amount { get; set; }
    public decimal Paid_Amount { get; set; }
    public string FromLedgerName { get; set; }
    public string ToLedgerName { get; set; }
    public decimal ObAmount { get; set; }
    public decimal CbAmount { get; set; }
    public string FundTransferMode { get; set; }
    public int? FundTransferRefGkey { get; set; }
    public DateTime? FundTransferDate { get; set; }
}

public class VoucherViewModel : ViewModelBase
{
    public ObservableCollection<VoucherEntry> VoucherEntries { get; }
        = new ObservableCollection<VoucherEntry>();

    public async Task LoadDataAsync(string connectionString)
    {
        VoucherEntries.Clear();

        using (var conn = new SqlConnection(connectionString))
        using (var cmd = new SqlCommand("SELECT * FROM dbo.VIEW_VOUCHER_RECEIPT_PAYMENT", conn))
        {
            await conn.OpenAsync();
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    VoucherEntries.Add(new VoucherEntry
                    {
                        Gkey = reader.GetInt32(reader.GetOrdinal("gkey")),
                        SeqNbr = reader.GetInt32(reader.GetOrdinal("seq_nbr")),
                        CustomerGkey = reader["customer_gkey"].ToString(),
                        TransType = reader["trans_type"].ToString(),
                        VoucherType = reader["voucher_type"].ToString(),
                        Mode = reader["mode"].ToString(),
                        TransAmount = reader.GetDecimal(reader.GetOrdinal("trans_amount")),
                        VoucherNbr = reader["voucher_nbr"].ToString(),
                        VoucherDate = reader.GetDateTime(reader.GetOrdinal("voucher_date")),
                        RefDocNbr = reader["ref_doc_nbr"].ToString(),
                        RefDocDate = reader["ref_doc_date"] as DateTime?,
                        TransDesc = reader["trans_desc"].ToString(),
                        TransDate = reader.GetDateTime(reader.GetOrdinal("trans_date")),
                        Recd_Amount = reader.GetDecimal(reader.GetOrdinal("Recd_Amount")),
                        Paid_Amount = reader.GetDecimal(reader.GetOrdinal("Paid_Amount")),
                        FromLedgerName = reader["FROM_LEDGER_NAME"].ToString(),
                        ToLedgerName = reader["TO_LEDGER_NAME"].ToString(),
                        ObAmount = reader.GetDecimal(reader.GetOrdinal("ob_amount")),
                        CbAmount = reader.GetDecimal(reader.GetOrdinal("cb_amount")),
                        FundTransferMode = reader["fund_transfer_mode"].ToString(),
                        FundTransferRefGkey = reader["fund_transfer_ref_gkey"] as int?,
                        FundTransferDate = reader["fund_transfer_date"] as DateTime?
                    });
                }
            }
        }
    }
}
}
