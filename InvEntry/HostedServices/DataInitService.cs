using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using InvEntry.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace InvEntry.HostedServices;

public class DataInitService : IHostedService
{
    private Mutex _mutex;
    private readonly ILogger<DataInitService> _logger;
    private readonly IHostApplicationLifetime _appLifeTime;
    private readonly IMasterDataService _masterDataService;

    public DataInitService(ILogger<DataInitService> logger,
        IHostApplicationLifetime appLifeTime, IMasterDataService masterDataService)
    {
        _logger = logger;
        _appLifeTime = appLifeTime;
        _masterDataService = masterDataService;
    }


    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _mutex = new Mutex(true, "InvEntryMainSingleInstanceMutex", out bool isNewInstance);

        if (!isNewInstance)
        {
            DXMessageBox.Show(
                "InvEntry is already running...",
                "Info",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            _appLifeTime.StopApplication();
            return;
        }

        var splashScreenViewModel = new DXSplashScreenViewModel()
        {
            Copyright = "All rights reserved",
            IsIndeterminate = true,
            Title = "InvEntry",
            Status = "Loading..."
        };

        SplashScreenManager.CreateThemed(splashScreenViewModel, topmost: true).ShowOnStartup();

        splashScreenViewModel.Status = "Loading Product Categories .....";

        await _masterDataService.InitAsync();

        SplashScreenManager.CloseAll();
    }


/*    public Task StartAsync(CancellationToken cancellationToken)
    {
        var splashScreenViewModel = new DXSplashScreenViewModel()
        {
            Copyright = "All rights reserved",
            IsIndeterminate = true,
            Title = "InvEntry",
            Status = "Loading..."
        };

        SplashScreenManager.CreateThemed(splashScreenViewModel, topmost: true).ShowOnStartup();

        bool isRunning;
        if (Mutex.TryOpenExisting("InvEntryMainSingleInstanceMutex", out _mutex))
        {
            isRunning = true;
        }
        else
        {
            _mutex = new Mutex(true, "InvEntryMainSingleInstanceMutex", out bool isNew);
            isRunning = !isNew;
        }

        if (isRunning)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                SplashScreenManager.CloseAll();

                DXMessageBox.Show("InvEntry is already running...", "Info", MessageBoxButton.OK,
                    MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);

                _appLifeTime.StopApplication();
            });

            return Task.CompletedTask;
        }

        Task.Run(async () =>
        {
            await _masterDataService.InitAsync();
        });

        splashScreenViewModel.Status = "Loading Product Categories .....";
        Task.Delay(250).Wait();

        return Task.CompletedTask;
    }*/

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Task.Delay(1000);
        SplashScreenManager.CloseAll();
        return Task.CompletedTask;
    }
}