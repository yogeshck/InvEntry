using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using InvEntry.Contracts.Schemes;
using InvEntry.Helpers;
using InvEntry.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace InvEntry.ViewModels;

/// <summary>
/// One-time import of the shop's RD register: paste the scheme master and the
/// payment ledger from Excel, preview what will be imported, then import.
/// </summary>
public partial class SchemeImportViewModel : ObservableObject
{
    private readonly ISchemeService _schemeService;
    private readonly IMessageBoxService _messageBoxService;

    public SchemeImportViewModel(
        ISchemeService schemeService,
        IMessageBoxService messageBoxService)
    {
        _schemeService = schemeService;
        _messageBoxService = messageBoxService;
    }

    [ObservableProperty]
    private ObservableCollection<SchemePlanDto> _plans = new();

    [ObservableProperty]
    private SchemePlanDto? _selectedPlan;

    [ObservableProperty]
    private string? _masterText;

    [ObservableProperty]
    private string? _paymentsText;

    [ObservableProperty]
    private ObservableCollection<SchemeImportSchemeResult> _results = new();

    [ObservableProperty]
    private string? _summary;

    [ObservableProperty]
    private string? _problems;

    [ObservableProperty]
    private bool _canImport;

    partial void OnMasterTextChanged(string? value) => CanImport = false;

    partial void OnPaymentsTextChanged(string? value) => CanImport = false;

    partial void OnSelectedPlanChanged(SchemePlanDto? value) => CanImport = false;

    [RelayCommand]
    private async Task LoadPlansAsync()
    {
        try
        {
            var plans = await _schemeService.GetPlans(activeOnly: false);
            Plans = new(plans.Where(x => x.SchemeType == SchemeType.Amount && x.AmountFixed));

            SelectedPlan = SelectedPlan is null
                ? Plans.Count == 1 ? Plans[0] : null
                : Plans.FirstOrDefault(x => x.Gkey == SelectedPlan.Gkey);
        }
        catch (Exception ex)
        {
            ShowError("Scheme plans could not be loaded.", ex);
        }
    }

    [RelayCommand]
    private async Task PreviewAsync()
    {
        await RunAsync(apply: false);
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        var ready = Results.Count(x => x.Outcome == SchemeImportOutcome.Ready);
        var amount = Results.Where(x => x.Outcome == SchemeImportOutcome.Ready).Sum(x => x.TotalPaid);

        if (ready == 0)
        {
            _messageBoxService.ShowMessage("Nothing is ready to import. Preview first and fix the errors.",
                "Import RD register", MessageButton.OK, MessageIcon.Exclamation);
            return;
        }

        if (_messageBoxService.ShowMessage(
                $"Import {ready} RD scheme(s) with Rs. {amount:N2} already paid into plan {SelectedPlan?.PlanCode}?" +
                $"{Environment.NewLine}Schemes with errors are skipped and can be imported later.",
                "Import RD register", MessageButton.YesNo, MessageIcon.Question) != MessageResult.Yes)
        {
            return;
        }

        await RunAsync(apply: true);
    }

    [RelayCommand]
    private void Clear()
    {
        MasterText = null;
        PaymentsText = null;
        Results = new();
        Summary = null;
        Problems = null;
        CanImport = false;
    }

    private async Task RunAsync(bool apply)
    {
        if (SelectedPlan is null)
        {
            _messageBoxService.ShowMessage("Select the plan the RDs are imported into.",
                "Import RD register", MessageButton.OK, MessageIcon.Exclamation);
            return;
        }

        var schemes = SchemeImportSheetParser.ParseSchemes(MasterText);
        var payments = SchemeImportSheetParser.ParsePayments(PaymentsText);

        var parseProblems = schemes.Problems.Select(x => $"RD master: {x}")
            .Concat(payments.Problems.Select(x => $"Payments: {x}"))
            .ToList();

        if (schemes.Rows.Count == 0)
        {
            Problems = string.Join(Environment.NewLine, parseProblems.DefaultIfEmpty("No RD rows found."));
            CanImport = false;
            return;
        }

        try
        {
            var result = await _schemeService.Import(new SchemeImportRequest
            {
                Apply = apply,
                SchemePlanGkey = SelectedPlan.Gkey,
                Schemes = schemes.Rows,
                Payments = payments.Rows
            });

            Results = new(result.Schemes.OrderBy(x => x.Outcome).ThenBy(x => x.RowNumber));

            Summary = apply
                ? $"Imported {result.Imported} scheme(s), Rs. {result.AmountImported:N2}. " +
                  $"Already imported: {result.AlreadyImported}. Skipped: {result.Skipped}."
                : $"Ready to import: {result.ReadyToImport}. Already imported: {result.AlreadyImported}. " +
                  $"Skipped (errors): {result.Skipped}. Payments read: {payments.Rows.Count}.";

            Problems = string.Join(Environment.NewLine,
                parseProblems.Concat(result.GeneralIssues.Select(x => $"{x.Severity}: {x.Message}")));

            CanImport = !apply && result.ReadyToImport > 0;

            if (apply)
            {
                _messageBoxService.ShowMessage(Summary, "Import RD register", MessageButton.OK, MessageIcon.Information);
            }
        }
        catch (Exception ex)
        {
            ShowError(apply ? "The import failed." : "The preview failed.", ex);
            CanImport = false;
        }
    }

    private void ShowError(string message, Exception ex)
    {
        _messageBoxService.ShowMessage(
            message + Environment.NewLine + ApiErrorMessage.From(ex),
            "Import RD register", MessageButton.OK, MessageIcon.Error);
    }
}
