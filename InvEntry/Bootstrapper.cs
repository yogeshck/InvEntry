using DevExpress.CodeParser;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.WindowsUI.Navigation;
using InvEntry.Extension;
using InvEntry.HostedServices;
using InvEntry.IoC;
using InvEntry.Metadata;
using InvEntry.Models;
using InvEntry.Reports;
using InvEntry.Services;
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
                    .AddMetadata<MtblReference, MtblRefrencesMetadata>();
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
        if (type is null) return null;

        return _host.Services.GetService(type);
    }

    private void BuildHost(StartupEventArgs args)
    {
        var argSet = args.Args.Select(d => d.ToLower()).ToHashSet();
        string[] arguments = args.Args;
        if(argSet.Count == 0 || !argSet.Any(d => d.Contains("environment")))
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
                 .AddTransient<VoucherEntryViewModel>()
                 .AddTransient<InvoiceListViewModel>()
                 .AddTransient<InvoiceViewModel>()
                 .AddTransient<ProductStockViewModel>()
                 .AddSingleton<MainWindowViewModel>()
                 .AddTransient<ReportDialogViewModel>()
                 .AddSingleton<SettingsPageViewModel>()
                 .AddTransient<INavigationService, FrameNavigationService>()
                 .AddSingleton<ICustomerService, CustomerService>()
                 .AddSingleton<IProductService, ProductService>()
                 .AddSingleton<IProductCategoryService, ProductCategoryService>()
                 .AddSingleton<IInvoiceService, InvoiceService>()
                 .AddSingleton<IMijmsApiService, MijmsApiService>()
                 .AddSingleton<IVoucherService, VoucherService>()
                 .AddSingleton<IInvoiceArReceiptService, InvoiceArReceiptService>()
                 .AddSingleton<IMtblReferencesService, MtblReferencesService>()
                 .AddSingleton<IMasterDataService, MasterDataService>()
                 .AddSingleton<IReportFactoryService, ReportFactoryService>()
                 .ConfigureFormulas()
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