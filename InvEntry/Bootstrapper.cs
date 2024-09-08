using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Xpf.Core;
using InvEntry.Extension;
using InvEntry.Metadata;
using InvEntry.ViewModels;
using InvEntry.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using Tern.MI.InvEntry.Models;

namespace InvEntry;
public sealed class Bootstrapper
{

    private static IHost _host;

    private Bootstrapper()
    {
        DISource.Resolver = Resolve;

        //MetadataLocator.Default = MetadataLocator.Create()
        //            .AddMetadata<InvoiceHeader, InvoiceHeaderMetadata>();
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
            arguments = ["--environment", "Development"];
        }

        Dispatcher dispatcher = Application.Current.Dispatcher;
        Messenger.Default = new Messenger(true);

        _host = Host.CreateDefaultBuilder(arguments)
            .ConfigureServices((ctx, services) => services
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
                .AddTransient<InvoiceListViewModel>()
                .AddTransient<InvoiceViewModel>()
                ).Build();
    }
}