using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using InvEntry.Extension;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.Tally;
using InvEntry.Tally.Model;
using InvEntry.Utils.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DevExpress.Xpf.Printing;
using IDialogService = DevExpress.Mvvm.IDialogService;
using InvEntry.Reports;
using System.Windows;

namespace InvEntry.ViewModels;

public partial class VoucherListViewModel: ObservableObject
{
    private readonly IVoucherService _voucherService;
    private readonly IDialogService _reportDialogService;
    private readonly ITallyXMLService _xmlService;

    private readonly IReportFactoryService _reportFactoryService;

    [ObservableProperty]
    private ObservableCollection<Voucher> _vouchers;

    [ObservableProperty]
    private VoucherSearchOption _searchOption;

    [ObservableProperty]
    private Voucher _SelectedVoucher;

    [ObservableProperty]
    private DateTime _Today = DateTime.Today;

    public  VoucherListViewModel(IVoucherService voucherService,
            ITallyXMLService xmlService,
            IReportFactoryService reportFactoryService,
            [FromKeyedServices("ReportDialogService")] IDialogService reportDialogService)
    {
        _voucherService = voucherService;
        _reportDialogService = reportDialogService;
        _xmlService = xmlService;
        _reportFactoryService = reportFactoryService;
        _searchOption = new();
        SearchOption.To = Today;
        SearchOption.From = Today.AddDays(-2);
        Task.Run(RefreshVoucherAsync).Wait();
    }

    [RelayCommand]
    private async Task RefreshVoucherAsync()
    {
        var vouchersResult = await _voucherService.GetAll(SearchOption);
        if (vouchersResult is not null)
            Vouchers = new(vouchersResult);
    }

    [RelayCommand] //CanExecute = nameof(CanPrintStatement))]
    private void StatementPrint()
    {
        //var printed = PrintHelper.Print(_reportFactoryService.CreateFinStatementReport(SearchOption.From, SearchOption.To));

        var report = _reportFactoryService.CreateFinStatementReport(SearchOption.From, SearchOption.To);

        PrintHelper.ShowPrintPreviewDialog(Application.Current.MainWindow,report);

        //if (printed.HasValue && printed.Value)
        //    _messageBoxService.ShowMessage("Estimate printed Successfully", "Estimate print", MessageButton.OK, MessageIcon.None);
    
    }

    /*    [RelayCommand(CanExecute = nameof(CanPrintInvoice))]
    private void PrintInvoice()
    {
        _reportDialogService.PrintPreview(SelectedVoucher.??);
    }

    private bool CanPrintInvoice()
    {
        return SelectedVoucher is not null;
    }*/


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
}
