#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using InvEntry.Contracts.Schemes;
using InvEntry.Helpers;
using InvEntry.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace InvEntry.ViewModels;

/// <summary>Which installments the RD dues list shows.</summary>
public enum RdDueFilter
{
    /// <summary>Everything due or collected in the month.</summary>
    All,

    /// <summary>Installments with an unpaid balance.</summary>
    Due,

    /// <summary>Installments that received money in the month.</summary>
    Collected
}

/// <summary>
/// RD dues and collections for a month, for follow-up calls, printing and Excel export.
/// </summary>
public partial class RdDuesViewModel : ObservableObject
{
    private readonly ISchemeService _schemeService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly IRdDuesPrinter _printer;

    private RdDueReportDto? _report;
    private bool _reloadRequested;

    public RdDuesViewModel(
        ISchemeService schemeService,
        IMessageBoxService messageBoxService,
        IRdDuesPrinter printer)
    {
        _schemeService = schemeService;
        _messageBoxService = messageBoxService;
        _printer = printer;
    }

    public IReadOnlyList<RdDueFilter> Filters { get; } = Enum.GetValues<RdDueFilter>();

    /// <summary>Any day of the month to list; only month and year are used.</summary>
    [ObservableProperty]
    private DateTime _selectedMonth = DateTime.Today;

    [ObservableProperty]
    private RdDueFilter _filter = RdDueFilter.All;

    /// <summary>Also list unpaid installments due before the month.</summary>
    [ObservableProperty]
    private bool _includeArrears = true;

    [ObservableProperty]
    private bool _isBusy;

    public ObservableCollection<RdDueRowDto> Rows { get; } = new();

    public string MonthTitle => SelectedMonth.ToString("MMMM yyyy");

    /// <summary>"RD-Dues-2026-10" (used for the Excel file name).</summary>
    public string FileBaseName => $"RD-{Filter}-{SelectedMonth:yyyy-MM}";

    // Summary of everything loaded for the month, whatever the filter.
    public int DueCount => All.Count(x => x.IsDue);
    public decimal DueAmount => All.Where(x => x.IsDue).Sum(x => x.Balance);
    public int ArrearCount => All.Count(x => x.IsArrear);
    public decimal ArrearAmount => All.Where(x => x.IsArrear).Sum(x => x.Balance);
    public int CollectedCount => All.Count(x => x.IsCollected);
    public decimal CollectedAmount => All.Sum(x => x.CollectedInMonth);
    public int CustomerCount => Rows.Select(x => x.CustomerGkey).Distinct().Count();

    public bool HasRows => Rows.Count > 0;

    public string StatusMessage =>
        _report is null
            ? "Choose a month and press Load."
            : Rows.Count == 0
                ? $"Nothing {(Filter == RdDueFilter.All ? "due or collected" : Filter.ToString().ToLowerInvariant())} for {MonthTitle}."
                : $"{Rows.Count} installment(s) of {CustomerCount} customer(s) — {Filter} for {MonthTitle}" +
                  (_report.IncludesArrears ? ", with earlier arrears." : ".");

    private IEnumerable<RdDueRowDto> All => _report?.Rows ?? Enumerable.Empty<RdDueRowDto>();

    partial void OnSelectedMonthChanged(DateTime value) => _ = LoadAsync();

    partial void OnIncludeArrearsChanged(bool value) => _ = LoadAsync();

    partial void OnFilterChanged(RdDueFilter value) => ApplyFilter();

    [RelayCommand]
    private Task Load() => LoadAsync();

    private async Task LoadAsync()
    {
        if (IsBusy)
        {
            // Month or arrears changed while loading: load again with the latest choice.
            _reloadRequested = true;
            return;
        }

        IsBusy = true;
        try
        {
            do
            {
                _reloadRequested = false;
                _report = await _schemeService.GetMonthlyDues(SelectedMonth.Year, SelectedMonth.Month, IncludeArrears);
            }
            while (_reloadRequested);

            ApplyFilter();
        }
        catch (Exception ex)
        {
            _messageBoxService.ShowMessage(
                "The RD dues could not be loaded." + Environment.NewLine + ApiErrorMessage.From(ex),
                "RD dues", MessageButton.OK, MessageIcon.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyFilter()
    {
        Rows.Clear();

        foreach (var row in All.Where(Matches))
            Rows.Add(row);

        OnPropertyChanged(nameof(MonthTitle));
        OnPropertyChanged(nameof(FileBaseName));
        OnPropertyChanged(nameof(DueCount));
        OnPropertyChanged(nameof(DueAmount));
        OnPropertyChanged(nameof(ArrearCount));
        OnPropertyChanged(nameof(ArrearAmount));
        OnPropertyChanged(nameof(CollectedCount));
        OnPropertyChanged(nameof(CollectedAmount));
        OnPropertyChanged(nameof(CustomerCount));
        OnPropertyChanged(nameof(HasRows));
        OnPropertyChanged(nameof(StatusMessage));
        PrintCommand.NotifyCanExecuteChanged();
    }

    private bool Matches(RdDueRowDto row) => Filter switch
    {
        RdDueFilter.Due => row.IsDue,
        RdDueFilter.Collected => row.IsCollected,
        _ => true
    };

    [RelayCommand(CanExecute = nameof(HasRows))]
    private async Task Print()
    {
        if (_report is null)
            return;

        await _printer.PreviewAsync(_report, Rows.ToList(), Filter);
    }

    [RelayCommand]
    private void PreviousMonth() => SelectedMonth = SelectedMonth.AddMonths(-1);

    [RelayCommand]
    private void NextMonth() => SelectedMonth = SelectedMonth.AddMonths(1);

    [RelayCommand]
    private void ThisMonth() => SelectedMonth = DateTime.Today;
}
