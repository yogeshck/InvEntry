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
using InvEntry.Models.UI;

namespace InvEntry.ViewModels;

public partial class SettingsPageViewModel : ObservableObject
{
    private readonly IMijmsApiService _mijmsApiService;
    private readonly IDailyRateDefinitionService _rateDefinitionService;

    public bool NavigateToInvoiceWhenPricesComplete { get; set; }

    [ObservableProperty]
    private ObservableCollection<DailyRate> dailyMetalRate = new();

    [ObservableProperty]
    private ObservableCollection<DailyRate> todayDailyMetalRate = new();

    [ObservableProperty]
    private ObservableCollection<DailyRate> historyDailyMetalRate = new();

    [ObservableProperty]
    private ObservableCollection<DailyRate> headerRates = new();

    [ObservableProperty]
    private ObservableCollection<DailyRateDefinition>  rateDefinitions = new();

    public SettingsPageViewModel(
       IMijmsApiService mijmsApiService,
       IDailyRateDefinitionService rateDefinitionService)
    {
        _mijmsApiService = mijmsApiService;
        _rateDefinitionService = rateDefinitionService;
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
                    .GetResponse<List<DailyRate>>(
                        "api/dailyrate/latest");

/*            foreach (var rate in dailyRates ?? [])
            {
                System.Diagnostics.Debug.WriteLine(
                    $"{rate.Metal} | " +
                    $"{rate.EffectiveDate:dd-MMM-yyyy HH:mm:ss} | " +
                    $"{rate.Price}");
            }*/

            DailyMetalRate =
                dailyRates is null
                    ? new()
                    : new(dailyRates);


            // -------------------------------------------------
            // Current configured rates
            // -------------------------------------------------

            var definitions =
                await _rateDefinitionService
                    .GetDefinitionsAsync();

            RateDefinitions =
                new ObservableCollection<DailyRateDefinition>(
                    definitions);

            BuildTodayRates();

            RefreshRateCollections();


            // -------------------------------------------------
            // Recent history
            // -------------------------------------------------

            HistoryDailyMetalRate =
                new ObservableCollection<DailyRate>(
                    DailyMetalRate
                        .Where(x => x.GKey != 0)
                        .OrderByDescending(x => x.EffectiveDate)
                        .ThenByDescending(x => x.GKey)
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

    /*    private static List<DailyRate> GetRateDefinitions()
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
        }*/


    // =========================================================
    // BUILD TODAY'S RATE COLLECTION
    // =========================================================

    private void BuildTodayRates()
    {
        TodayDailyMetalRate.Clear();


        var activeDefinitions =
            RateDefinitions
                .Where(x => x.TrackDailyRate)
                .OrderBy(x => x.DisplayOrder);


        foreach (var definition in activeDefinitions)
        {
            var latestRate =
                DailyMetalRate
                    .Where(x =>
                        IsSame(
                            x,
                            definition))
                    .OrderByDescending(
                        x => x.EffectiveDate)
                    .ThenByDescending(
                        x => x.GKey)
                    .FirstOrDefault();


            TodayDailyMetalRate.Add(
                new DailyRate
                {
                    // Always editable/new.
                    // Historical DB record is never modified.
                    GKey = 0,

                    Metal = definition.Metal,

                    Purity = definition.Purity,

                    Carat = definition.Carat,

                    Price = latestRate?.Price,

                    EffectiveDate =
                        latestRate?.EffectiveDate
                        ?? DateTime.Now,

                    IsDisplay = true
                });
        }

        RefreshHeaderRates();

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
        // Save a row when today's required record is missing, even if
        // its value is unchanged from yesterday. Daily rates are an
        // append-only history and startup completeness is date-based.
        // ---------------------------------------------------------

        var ratesToSave =
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
                           latest.EffectiveDate.Date != DateTime.Today ||
                           latest.Price != current.Price;
                })
                .ToList();


        if (ratesToSave.Count == 0)
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
                ratesToSave
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


            // Re-read persisted history before treating startup rates as
            // complete. A successful HTTP response alone is not enough.
            await LoadedCommand.ExecuteAsync(null);

            if (!IsAllPriceUpdated())
            {
                DXMessageBox.Show(
                    $"The following required rates are still missing for today: " +
                    string.Join(", ", GetMissingRequiredPriceNames()),
                    "Daily Rate Incomplete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }


            DXMessageBox.Show(
                "Rate changes saved successfully.",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            if (NavigateToInvoiceWhenPricesComplete)
            {
                NavigateToInvoiceWhenPricesComplete = false;
                Messenger.Default.Send(
                    "InvoiceEntryPage",
                    "NavigateToPage");
            }
        }
        finally
        {
            Messenger.Default.Send(
                MessageType.WaitIndicator,
                WaitIndicatorVM.HideIndicator());
        }
    }

    private void RefreshHeaderRates()
    {
        HeaderRates.Clear();


        var definitions =
            RateDefinitions
                .Where(x =>
                    x.TrackDailyRate &&
                    x.ShowInHeader)
                .OrderBy(x =>
                    x.DisplayOrder);


        foreach (var definition in definitions)
        {
            var rate =
                TodayDailyMetalRate
                    .FirstOrDefault(x =>
                        IsSame(
                            x,
                            definition));

            if (rate is not null)
                HeaderRates.Add(rate);
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

/*    public IEnumerable<DailyRate> HeaderRates
    {
        get
        {
            var headerDefinitions =
                RateDefinitions
                    .Where(x =>
                        x.TrackDailyRate &&
                        x.ShowInHeader)
                    .OrderBy(x =>
                        x.DisplayOrder);


            foreach (var definition
                     in headerDefinitions)
            {
                var rate =
                    TodayDailyMetalRate
                        .FirstOrDefault(x =>
                            IsSame(
                                x,
                                definition));

                if (rate is not null)
                    yield return rate;
            }
        }
    }*/

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

    public IReadOnlyList<string> GetMissingRequiredPriceNames()
    {
        return TodayDailyMetalRate
            .Where(x =>
                x.EffectiveDate.Date != DateTime.Today ||
                !x.Price.HasValue)
            .Select(x =>
                string.IsNullOrWhiteSpace(x.Carat)
                    ? x.Metal ?? string.Empty
                    : $"{x.Metal} ({x.Carat})")
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
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


    private static bool IsSame(
        DailyRate? rate,
        DailyRateDefinition? definition)
    {
        if (rate is null || definition is null)
            return false;

        return
            string.Equals(
                rate.Metal,
                definition.Metal,
                StringComparison.OrdinalIgnoreCase)
            &&
            string.Equals(
                rate.Purity,
                definition.Purity,
                StringComparison.OrdinalIgnoreCase)
            &&
            string.Equals(
                rate.Carat,
                definition.Carat,
                StringComparison.OrdinalIgnoreCase);
    }

}
