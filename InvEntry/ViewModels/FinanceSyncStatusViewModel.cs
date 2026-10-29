#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using InvEntry.Contracts.FinanceSync;
using InvEntry.Helpers;
using InvEntry.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace InvEntry.ViewModels;

/// <summary>
/// Whether invoices and receipts have reached FinanceTracker, why not, and what to
/// do, with Send to try again. Shared by the Finance Sync Status page and the
/// main window's status bar, which refreshes it every few minutes.
/// </summary>
public partial class FinanceSyncStatusViewModel : ObservableObject
{
    private static readonly TimeSpan BackgroundRefresh = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan AfterSendDelay = TimeSpan.FromSeconds(4);

    private readonly IFinanceSyncStatusClient _client;
    private readonly IMessageBoxService _messageBoxService;
    private DispatcherTimer? _timer;

    public FinanceSyncStatusViewModel(
        IFinanceSyncStatusClient client,
        IMessageBoxService messageBoxService)
    {
        _client = client;
        _messageBoxService = messageBoxService;
    }

    public ObservableCollection<FinanceSyncAttentionItemDto> Items { get; } = new();

    public ObservableCollection<FinanceSyncIssueDto> Issues { get; } = new();

    [ObservableProperty]
    private bool _isBusy;

    /// <summary>OK, ATTENTION, PROBLEM, or UNKNOWN before the first answer.</summary>
    [ObservableProperty]
    private string _overallState = "UNKNOWN";

    [ObservableProperty]
    private string _headline = "Checking finance sync…";

    [ObservableProperty]
    private string _automaticSyncText = string.Empty;

    [ObservableProperty]
    private int _sendableCount;

    [ObservableProperty]
    private string _checkedOnText = string.Empty;

    /// <summary>Short text for the main window's status bar.</summary>
    [ObservableProperty]
    private string _statusBarText = "Finance sync: checking…";

    public bool HasItems => Items.Count > 0;

    public bool HasIssues => Issues.Count > 0;

    public bool NeedsAttention => OverallState is FinanceSyncOverallStates.Attention or FinanceSyncOverallStates.Problem;

    public string SendAllText => $"Send all ({SendableCount})";

    public bool CanSendAll => SendableCount > 0 && !IsBusy;

    partial void OnOverallStateChanged(string value) => OnPropertyChanged(nameof(NeedsAttention));

    partial void OnSendableCountChanged(int value)
    {
        OnPropertyChanged(nameof(SendAllText));
        OnPropertyChanged(nameof(CanSendAll));
    }

    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(CanSendAll));

    /// <summary>Starts the background refresh for the status bar (once).</summary>
    public void StartBackgroundRefresh()
    {
        if (_timer != null)
            return;

        _timer = new DispatcherTimer { Interval = BackgroundRefresh };
        _timer.Tick += async (_, _) => await RefreshQuietly();
        _timer.Start();
        _ = RefreshQuietly();
    }

    [RelayCommand]
    private async Task Load()
    {
        try
        {
            await Refresh();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Finance sync status could not be loaded");
            ShowUnavailable();
            _messageBoxService.ShowMessage(
                "The finance sync status could not be loaded." + Environment.NewLine + ApiErrorMessage.From(ex),
                "Finance Sync Status",
                MessageButton.OK,
                MessageIcon.Warning);
        }
    }

    [RelayCommand]
    private Task Send(FinanceSyncAttentionItemDto? item) =>
        item is { CanSend: true } ? SendEntries(new[] { item.EventId }) : Task.CompletedTask;

    [RelayCommand]
    private Task SendAll() => SendEntries(null);

    private async Task SendEntries(long[]? eventIds)
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            var result = await _client.Send(eventIds);
            _messageBoxService.ShowMessage(result.Message, "Finance Sync", MessageButton.OK, MessageIcon.Information);

            if (result.Requested > 0)
            {
                await Task.Delay(AfterSendDelay);
                await Refresh();
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Finance sync send failed");
            _messageBoxService.ShowMessage(
                "The entries could not be sent." + Environment.NewLine + ApiErrorMessage.From(ex),
                "Finance Sync",
                MessageButton.OK,
                MessageIcon.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshQuietly()
    {
        if (IsBusy)
            return;

        try
        {
            await Refresh();
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Finance sync status could not be refreshed");
            ShowUnavailable();
        }
    }

    private async Task Refresh()
    {
        var wasBusy = IsBusy;
        IsBusy = true;
        try
        {
            Apply(await _client.GetStatus());
        }
        finally
        {
            IsBusy = wasBusy;
        }
    }

    private void Apply(FinanceSyncStatusDto status)
    {
        OverallState = status.OverallState;
        Headline = status.Headline;
        SendableCount = status.SendableCount;
        CheckedOnText = $"Checked {status.CheckedOn:dd-MM-yyyy HH:mm}";
        AutomaticSyncText = status.AutomaticSyncRunning
            ? $"Automatic sync is running{(status.LastAutomaticSyncOn is { } last ? $" (last run {last:HH:mm})" : string.Empty)}."
            : "Automatic sync is NOT running.";

        Items.Clear();
        foreach (var item in status.Items)
            Items.Add(item);

        Issues.Clear();
        foreach (var issue in status.Issues)
            Issues.Add(issue);

        var waiting = status.Items.Count(x => x.State != FinanceSyncAttentionStates.InProgress);
        StatusBarText = status.OverallState switch
        {
            FinanceSyncOverallStates.Ok => "Finance sync: OK",
            FinanceSyncOverallStates.Problem when waiting == 0 => "Finance sync: PROBLEM – click to view",
            _ => $"Finance sync: {waiting} need attention – click to view"
        };

        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(HasIssues));
    }

    private void ShowUnavailable()
    {
        OverallState = "UNKNOWN";
        Headline = "The finance sync status could not be checked.";
        StatusBarText = "Finance sync: status unavailable";
    }
}
