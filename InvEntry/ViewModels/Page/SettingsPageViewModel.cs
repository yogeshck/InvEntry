using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using InvEntry.Extension;
using InvEntry.Models;
using InvEntry.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace InvEntry.ViewModels;

public partial class SettingsPageViewModel : ObservableObject
{
    private readonly IMijmsApiService _mijmsApiService;

    [ObservableProperty]
    private ObservableCollection<DailyRate> dailyMetalRate = new();

    [ObservableProperty]
    private ObservableCollection<DailyRate> todayDailyMetalRate = new();

    [ObservableProperty]
    private ObservableCollection<DailyRate> historyDailyMetalRate = new();


    public SettingsPageViewModel(
        IMijmsApiService mijmsApiService)
    {
        _mijmsApiService = mijmsApiService;
    }


    // =========================================================
    // LOAD
    // =========================================================

    [RelayCommand]
    private async Task OnLoaded()
    {
        var wait =
            WaitIndicatorVM.ShowIndicator(
                "Fetching daily rate details...");

        Messenger.Default.Send(
            MessageType.WaitIndicator,
            wait);

        try
        {
            var dailyRates =
                await _mijmsApiService
                    .GetEnumerable<DailyRate>(
                        "api/dailyrate/latest");

            foreach (var rate in dailyRates ?? [])
            {
                System.Diagnostics.Debug.WriteLine(
                    $"{rate.Metal} | " +
                    $"{rate.EffectiveDate:dd-MMM-yyyy HH:mm:ss} | " +
                    $"{rate.Price}");
            }

            DailyMetalRate =
                dailyRates is null
                    ? new()
                    : new(dailyRates);


            // -------------------------------------------------
            // Current configured rates
            // -------------------------------------------------

            var rateDefinitions =
                GetRateDefinitions();


            BuildTodayRates(rateDefinitions);


            // -------------------------------------------------
            // Recent history
            // -------------------------------------------------

            HistoryDailyMetalRate =
                new ObservableCollection<DailyRate>(
                    DailyMetalRate
                        .Where(x =>
                            x.GKey != 0 &&
                            x.EffectiveDate.Date < DateTime.Today)
                        .OrderByDescending(x => x.EffectiveDate)
                        .Take(10));
        }
        finally
        {
            Messenger.Default.Send(
                MessageType.WaitIndicator,
                WaitIndicatorVM.HideIndicator());
        }
    }


    // =========================================================
    // TEMPORARY CONFIGURATION
    //
    // Later this comes from your Metal / Reference master.
    // =========================================================

    private static List<DailyRate> GetRateDefinitions()
    {
        return
        [
            new DailyRate
            {
                Metal = "GOLD",
                Purity = "916",
                Carat = "22 KT",
                IsDisplay = true
            },

            new DailyRate
            {
                Metal = "GOLD.18KT",
                Purity = "750",
                Carat = "18 KT",
                IsDisplay = true
            },

            new DailyRate
            {
                Metal = "SILVER",
                Purity = "XX",
                Carat = null,
                IsDisplay = true
            },

            new DailyRate
            {
                Metal = "DIAMOND",
                Purity = "XX",
                Carat = null,
                IsDisplay = true
            }
        ];
    }


    // =========================================================
    // BUILD TODAY'S RATE COLLECTION
    // =========================================================

    private void BuildTodayRates(
        IEnumerable<DailyRate> definitions)
    {
        TodayDailyMetalRate.Clear();

        foreach (var definition in definitions)
        {
            var latestRate =
                DailyMetalRate
                    .Where(x => IsSame(x, definition))
                    .OrderByDescending(x => x.EffectiveDate)
                    .ThenByDescending(x => x.GKey)
                    .FirstOrDefault();

            // IMPORTANT:
            // Never put the actual database entity into the editable collection.
            // Create an editable copy with GKey = 0.
            TodayDailyMetalRate.Add(
                new DailyRate
                {
                    GKey = 0,

                    Metal = definition.Metal,
                    Purity = definition.Purity,
                    Carat = definition.Carat,

                    // Show the latest existing price.
                    Price = latestRate?.Price,

                    // This will become the actual save time
                    // when the user changes/saves the rate.
                    EffectiveDate =
                        latestRate?.EffectiveDate
                        ?? DateTime.Now,

                    IsDisplay = definition.IsDisplay
                });
        }
    }


    // =========================================================
    // SAVE ALL
    // =========================================================

    [RelayCommand]
    private async Task SaveAllDailyRate()
    {
        if (TodayDailyMetalRate.Count == 0)
            return;

        var missingRates =
            TodayDailyMetalRate
                .Where(x => !x.Price.HasValue)
                .ToList();

        if (missingRates.Count > 0)
        {
            var metals =
                string.Join(
                    ", ",
                    missingRates.Select(x => x.Metal));

            DXMessageBox.Show(
                $"Please enter the rate for: {metals}",
                "Rate Required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }


        // ---------------------------------------------------------
        // Find only rates whose PRICE actually changed
        // ---------------------------------------------------------

        var changedRates =
            TodayDailyMetalRate
                .Where(current =>
                {
                    var latest =
                        DailyMetalRate
                            .Where(history =>
                                IsSame(history, current))
                            .OrderByDescending(
                                history =>
                                    history.EffectiveDate)
                            .ThenByDescending(
                                history =>
                                    history.GKey)
                            .FirstOrDefault();

                    return latest is null ||
                           latest.Price != current.Price;
                })
                .ToList();


        if (changedRates.Count == 0)
        {
            DXMessageBox.Show(
                "There are no rate changes to save.",
                "Daily Rate",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }


        Messenger.Default.Send(
            MessageType.WaitIndicator,
            WaitIndicatorVM.ShowIndicator(
                "Saving rate changes..."));


        try
        {
            // -----------------------------------------------------
            // IMPORTANT:
            //
            // Every changed rate becomes a NEW record.
            // No PUT / UPDATE.
            // -----------------------------------------------------

            var saveTime = DateTime.Now;

            var newRates =
                changedRates
                    .Select(x =>
                        new DailyRate
                        {
                            GKey = 0,

                            Metal = x.Metal,
                            Purity = x.Purity,
                            Carat = x.Carat,

                            Price = x.Price,

                            EffectiveDate = saveTime,

                            IsDisplay = x.IsDisplay
                        })
                    .ToList();


            var savedRates =
                await _mijmsApiService
                    .PostList<DailyRate>(
                        "api/dailyrate/save",
                        newRates);


            if (savedRates is null)
                return;


            // Add newly saved rows to our local history.
            foreach (var savedRate in savedRates)
            {
                DailyMetalRate.Add(savedRate);
            }


            RefreshRateCollections();


            DXMessageBox.Show(
                "Rate changes saved successfully.",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        finally
        {
            Messenger.Default.Send(
                MessageType.WaitIndicator,
                WaitIndicatorVM.HideIndicator());
        }
    }

    private void RefreshRateCollections()
    {
        HistoryDailyMetalRate =
            new ObservableCollection<DailyRate>(
                DailyMetalRate
                    .Where(x => x.GKey != 0)
                    .OrderByDescending(x => x.EffectiveDate)
                    .ThenByDescending(x => x.GKey)
                    .Take(10));
    }

    // =========================================================
    // VALIDATION
    // =========================================================

    public bool IsAllPriceUpdated()
    {
        if (TodayDailyMetalRate.Count == 0)
            return false;


        return TodayDailyMetalRate.All(x =>
            x.EffectiveDate.Date ==
                DateTime.Today &&
            x.Price.HasValue);
    }


    // =========================================================
    // RATE LOOKUP
    // =========================================================

    public decimal? GetPrice(string metalType)
    {
        if (string.IsNullOrWhiteSpace(metalType))
            return 0M;


        return TodayDailyMetalRate
            .FirstOrDefault(x =>
                string.Equals(
                    x.Metal,
                    metalType,
                    StringComparison.OrdinalIgnoreCase))
            ?.Price ?? 0M;
    }


    // Keep this for compatibility with existing code.
    public decimal? GetPrice(MetalType metalType)
    {
        return GetPrice(
            metalType switch
            {
                MetalType.Gold =>
                    "GOLD",

                MetalType.Gold18KT =>
                    "GOLD.18KT",

                MetalType.Silver =>
                    "SILVER",

                MetalType.Diamond =>
                    "DIAMOND",

                _ =>
                    string.Empty
            });
    }


    // =========================================================
    // HELPERS
    // =========================================================

    private static bool IsSame(
        DailyRate? x,
        DailyRate? y)
    {
        if (x is null || y is null)
            return false;


        return
            string.Equals(
                x.Metal,
                y.Metal,
                StringComparison.OrdinalIgnoreCase)

            &&

            string.Equals(
                x.Purity,
                y.Purity,
                StringComparison.OrdinalIgnoreCase)

            &&

            string.Equals(
                x.Carat,
                y.Carat,
                StringComparison.OrdinalIgnoreCase);
    }
}