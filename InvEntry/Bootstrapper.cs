using DevExpress.CodeParser;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.WindowsUI.Navigation;
using InvEntry.Extension;
using InvEntry.Helpers;
using InvEntry.HostedServices;
using InvEntry.IoC;
using InvEntry.Managers;
using InvEntry.Metadata;
using InvEntry.Models;
using InvEntry.Reports;
using InvEntry.Services;
using InvEntry.Tally;
using InvEntry.ViewModels;
using InvEntry.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;

namespace InvEntry;

public sealed class Bootstrapper
{
    private static IHost _host;

    private Bootstrapper()
    {
        DISource.Resolver = Resolve;

        MetadataLocator.Default = MetadataLocator.Create()
                    .AddMetadata<ProductCategory, ProductCategoryMetadata>()
                    .AddMetadata<MtblReference, MtblRefrencesMetadata>()
                    .AddMetadata<Voucher, VoucherMetadata>();
    }

    public static Bootstrapper Default { get; private set; }

    public static async void Run(StartupEventArgs args)
    {
        Default = new Bootstrapper();
        Default.BuildHost(args);

        await _host.RunAsync();
    }

    private object Resolve(Type type)
    {
        try
        {
            if (type is null) return null;

            return _host.Services.GetService(type);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error While resolving {0}", type.Name);
            return null;
        }
    }

    private void BuildHost(StartupEventArgs args)
    {
        var argSet = args.Args.Select(d => d.ToLower()).ToHashSet();
        string[] arguments = args.Args;
        if (argSet.Count == 0 || !argSet.Any(d => d.Contains("environment")))
        {
#if DEBUG
            arguments = ["--environment", "Development"];
#else
            arguments = ["--environment", "Production"];
#endif
        }

        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.File(@"C:\\Madrone\\InvEntry-.log", rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Dispatcher dispatcher = Application.Current.Dispatcher;
        Messenger.Default = new Messenger(true);

        var builder = Host.CreateDefaultBuilder(arguments)
             .ConfigureServices((ctx, services) => services
                 .AddHostedService<DataInitService>()
                 .AddSingleton(dispatcher)
                 .AddSingleton<IMessageBoxService>(_ =>
                 {
                     DXMessageBoxService messageBoxService = null;

                     dispatcher.Invoke(() =>
                     {
                         messageBoxService = new DXMessageBoxService();
                     });
                     return messageBoxService;
                 })
                 .AddTransient<IDialogService, DialogService>(sp =>
                 {
                     if (Application.Current.TryFindResource("ds") is DialogService dialogService)
                     {
                         return dialogService;
                     }

                     return new DialogService();
                 })
                 .AddKeyedSingleton<IDialogService, DialogService>("ReportDialogService", (key, sp) =>
                 {
                     if (Application.Current.TryFindResource("ReportDialogService") is DialogService dialogService)
                     {
                         return dialogService;
                     }

                     return new DialogService();
                 })
                 .AddSingleton<ICustomerOrderDbViewService, CustomerOrderDbViewService>()
                 .AddSingleton<ICustomerOrderService, CustomerOrderService>()
                 .AddSingleton<ICustomerService, CustomerService>()
                 .AddSingleton<IDailyStockSummaryService, DailyStockSummaryService>()
                 .AddSingleton<IEstimateService, EstimateService>()
                 .AddSingleton<IGrnDbViewService, GrnDbViewService>()
                 .AddSingleton<IGrnService, GrnService>()
                 .AddSingleton<IInvoiceArReceiptService, InvoiceArReceiptService>()
                 .AddSingleton<IInvoiceService, InvoiceService>()
                 .AddSingleton<ILedgerService, LedgerService>()
                 .AddSingleton<IMasterDataService, MasterDataService>()
                 .AddSingleton<IMijmsApiService, MijmsApiService>()
                 .AddSingleton<IMtblLedgersService, MtblLedgersService>()
                 .AddSingleton<IMtblReferencesService, MtblReferencesService>()
                 .AddSingleton<IOldMetalTransactionService, OldMetalTransactionService>()
                 .AddSingleton<IOrgThisCompanyViewService, OrgThisCompanyViewService>()
                 .AddSingleton<IProductCategoryService, ProductCategoryService>()
                 .AddSingleton<IProductService, ProductService>()
                 .AddSingleton<IProductStockService, ProductStockService>()
                 .AddSingleton<IProductStockSummaryService, ProductStockSummaryService>()
                 .AddSingleton<IProductTransactionService, ProductTransactionService>()
                 .AddSingleton<IProductTransactionSummaryService, ProductTransactionSummaryService>()
                 .AddSingleton<IProductViewService, ProductViewService>()
                 .AddSingleton<IReportFactoryService, ReportFactoryService>()
                 .AddSingleton<IStockManager, StockManager>()
                 .AddSingleton<IStockVerifyScanService, StockVerifyScanService>()
                 .AddSingleton<IVoucherDbViewService, VoucherDbViewService>()
                 .AddSingleton<IVoucherService, VoucherService>()
                 .AddSingleton<IVoucherTypeService, VoucherTypeService>()
                 .AddTransient<INavigationService, FrameNavigationService>()
                 .AddTransient<ReferenceLoader, ReferenceLoader>()
                 //           .AddSingleton<IMtblVoucherTypeService, MtblVoucherTypeService>()
                 .AddMockService()
                 .ConfigureFormulas()
                 .AddTallyService()
                 .AddSingleton<CashReceiptViewModel>()
                 .AddSingleton<CustomerOrderDBViewListViewModel>()
                 .AddSingleton<CustomerOrderListViewModel>()
                 .AddSingleton<CustomerOrderViewModel>()
                 .AddSingleton<DailyStockSummaryListViewModel>()
                 .AddSingleton<EstimateListViewModel>()
                 .AddSingleton<EstimateViewModel>()
                 .AddSingleton<GRNListViewModel>()
                 .AddSingleton<GRNViewModel>()
                 .AddSingleton<ImportDocViewModel>()
                 .AddSingleton<InvoiceListViewModel>()
                 .AddSingleton<InvoiceProductSelectionViewModel>()
                 .AddSingleton<InvoiceViewModel>()
                 .AddSingleton<InvoiceWithVouchersViewModel>()
                 .AddSingleton<MainWindowViewModel>()
                 .AddSingleton<OldMetalPurchaseViewModel>()
                 .AddSingleton<OldMetalTransactionListViewModel>()
                 .AddSingleton<OldMetalTransferEntryViewModel>()
                 .AddSingleton<ProductStockEntryViewModel>()
                 .AddSingleton<ProductStockTakingViewModel>()
                 .AddSingleton<ProductStockSummaryListViewModel>()
                 .AddSingleton<ReceiptAccountingViewModel>()
                 .AddSingleton<ProductStockViewModel>()
                 .AddSingleton<SettingsPageViewModel>()
                 .AddSingleton<VoucherEntryViewModel>()
                 .AddSingleton<VoucherListViewModel>()
                 .AddTransient<ReportDialogViewModel>()
                 //.AddSingleton<ReviewPopupViewModel>()

                 .AddHttpClient("mijms", httpClient =>
                 {
#if DEBUG
                     httpClient.BaseAddress = new Uri("https://localhost:7001/");
#else
                     httpClient.BaseAddress = new Uri("http://localhost:8500/");
#endif
                 }))
             .ConfigureLogging(logging =>
             {
                 logging.AddSerilog(dispose: true);
             })
             .ConfigureHostConfiguration(config =>
             {
                 config
                 .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
             });

        _host = builder.Build();
    }
}