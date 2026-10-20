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
/// Savings scheme (RD) plan setup. Changes apply to new enrollments only:
/// each enrollment keeps the terms it was enrolled on.
/// </summary>
public partial class SchemePlanViewModel : ObservableObject
{
    private readonly ISchemeService _schemeService;
    private readonly IMessageBoxService _messageBoxService;

    public SchemePlanViewModel(
        ISchemeService schemeService,
        IMessageBoxService messageBoxService)
    {
        _schemeService = schemeService;
        _messageBoxService = messageBoxService;

        NewPlan();
        _ = LoadAsync();
    }

    public ObservableCollection<string> SchemeTypes { get; } = new() { SchemeType.Amount, SchemeType.Weight };

    public ObservableCollection<string> Statuses { get; } =
        new() { SchemePlanStatus.Draft, SchemePlanStatus.Active, SchemePlanStatus.Retired };

    public ObservableCollection<string> RefundBases { get; } = new() { "AMOUNT_PAID", "GOLD_VALUE", "LOWER_OF" };

    public ObservableCollection<string> PreclosureBonuses { get; } = new() { "NONE", "PRORATED" };

    public ObservableCollection<string> BenefitTypes { get; } = new()
    {
        "INSTALLMENT_WAIVER", "PERCENT_BONUS", "MAKING_CHARGE_WAIVER", "BONUS_GRAMS"
    };

    public ObservableCollection<string> AppliesOnValues { get; } = new() { "MATURITY", "PRECLOSURE" };

    [ObservableProperty]
    private ObservableCollection<SchemePlanDto> _plans = new();

    [ObservableProperty]
    private SchemePlanDto? _selectedPlan;

    [ObservableProperty]
    private ObservableCollection<SchemeMetalOption> _metalOptions = new();

    [ObservableProperty]
    private SchemeMetalOption? _selectedMetal;

    /// <summary>The plan being edited: a copy, so cancelling leaves the list unchanged.</summary>
    [ObservableProperty]
    private SchemePlanDto _editor = new();

    [ObservableProperty]
    private ObservableCollection<SchemeBenefitRuleDto> _benefitRules = new();

    [ObservableProperty]
    private SchemeBenefitRuleDto? _selectedRule;

    [ObservableProperty]
    private DateTime _validFrom = DateTime.Today;

    [ObservableProperty]
    private DateTime? _validTo;

    public bool IsWeightScheme => Editor.SchemeType == SchemeType.Weight;

    public string EditorTitle => Editor.Gkey == 0
        ? "New plan"
        : $"{Editor.PlanCode}" + (Editor.EnrollmentCount > 0
            ? $" ({Editor.EnrollmentCount} enrollments keep their original terms)"
            : string.Empty);

    partial void OnSelectedPlanChanged(SchemePlanDto? value)
    {
        if (value is not null)
            Edit(value);
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            MetalOptions = new(await _schemeService.GetMetalOptions());
            var plans = await _schemeService.GetPlans(activeOnly: false);
            Plans = new(plans);
        }
        catch (Exception ex)
        {
            ShowError("Scheme plans could not be loaded.", ex);
        }
    }

    [RelayCommand]
    private void NewPlan()
    {
        SelectedPlan = null;

        Edit(new SchemePlanDto
        {
            SchemeType = SchemeType.Amount,
            TenureMonths = 11,
            AmountFixed = true,
            MinInstallmentAmount = 1000M,
            ValidFrom = DateOnly.FromDateTime(DateTime.Today),
            Status = SchemePlanStatus.Draft,
            RefundBasis = "AMOUNT_PAID",
            PreclosureBonus = "NONE"
        });
    }

    /// <summary>Called by the view when the scheme type changes, to show or hide the metal.</summary>
    [RelayCommand]
    private void SchemeTypeChanged()
    {
        OnPropertyChanged(nameof(IsWeightScheme));
    }

    [RelayCommand]
    private void AddRule()
    {
        var rule = new SchemeBenefitRuleDto
        {
            BenefitType = "INSTALLMENT_WAIVER",
            BenefitValue = 1M,
            AppliesOn = "MATURITY",
            MinInstallmentsPaid = Editor.TenureMonths,
            IsActive = true
        };

        BenefitRules.Add(rule);
        SelectedRule = rule;
    }

    [RelayCommand]
    private void RemoveRule(SchemeBenefitRuleDto? rule)
    {
        if ((rule ?? SelectedRule) is { } toRemove)
            BenefitRules.Remove(toRemove);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        Editor.ValidFrom = DateOnly.FromDateTime(ValidFrom);
        Editor.ValidTo = ValidTo is DateTime to ? DateOnly.FromDateTime(to) : null;
        Editor.BenefitRules = BenefitRules.ToList();

        if (Editor.SchemeType == SchemeType.Weight)
        {
            Editor.Metal = SelectedMetal?.Metal;
            Editor.Purity = SelectedMetal?.Purity;
        }
        else
        {
            Editor.Metal = null;
            Editor.Purity = null;
        }

        try
        {
            var saved = await _schemeService.SavePlan(Editor);

            var today = DateOnly.FromDateTime(DateTime.Today);
            var availability = saved.Status != SchemePlanStatus.Active
                ? $"It is {saved.Status}: set Status to ACTIVE to make it available in Scheme Enrollment."
                : saved.ValidFrom > today
                    ? $"It is available for enrollment from {saved.ValidFrom:dd-MM-yyyy}."
                    : saved.ValidTo is DateOnly validTo && validTo < today
                        ? $"Its valid-to date {validTo:dd-MM-yyyy} has passed, so it is not offered for enrollment."
                        : "It is available in Scheme Enrollment.";

            _messageBoxService.ShowMessage(
                $"Plan {saved.PlanCode} saved.{Environment.NewLine}{availability}",
                "Scheme plan", MessageButton.OK, MessageIcon.Information);

            await LoadAsync();
            SelectedPlan = Plans.FirstOrDefault(x => x.Gkey == saved.Gkey);
        }
        catch (Exception ex)
        {
            ShowError("The plan was not saved.", ex);
        }
    }

    private void Edit(SchemePlanDto plan)
    {
        // Work on a copy so an unsaved edit never changes the list.
        Editor = new SchemePlanDto
        {
            Gkey = plan.Gkey,
            PlanCode = plan.PlanCode,
            PlanName = plan.PlanName,
            SchemeType = plan.SchemeType,
            Metal = plan.Metal,
            Purity = plan.Purity,
            TenureMonths = plan.TenureMonths,
            AmountFixed = plan.AmountFixed,
            MinInstallmentAmount = plan.MinInstallmentAmount,
            MaxInstallmentAmount = plan.MaxInstallmentAmount,
            GraceDays = plan.GraceDays,
            MaxMissedForBonus = plan.MaxMissedForBonus,
            MaxMissedBeforeLapse = plan.MaxMissedBeforeLapse,
            MinMonthsForRefund = plan.MinMonthsForRefund,
            RefundDeductionPct = plan.RefundDeductionPct,
            RefundBasis = plan.RefundBasis,
            PreclosureBonus = plan.PreclosureBonus,
            ValidFrom = plan.ValidFrom,
            ValidTo = plan.ValidTo,
            Status = plan.Status,
            TermsText = plan.TermsText,
            EnrollmentCount = plan.EnrollmentCount
        };

        BenefitRules = new(plan.BenefitRules.Select(x => new SchemeBenefitRuleDto
        {
            BenefitType = x.BenefitType,
            BenefitValue = x.BenefitValue,
            AppliesOn = x.AppliesOn,
            MinInstallmentsPaid = x.MinInstallmentsPaid,
            EligibleCategories = x.EligibleCategories,
            IsActive = x.IsActive
        }));

        ValidFrom = plan.ValidFrom == default ? DateTime.Today : plan.ValidFrom.ToDateTime(TimeOnly.MinValue);
        ValidTo = plan.ValidTo?.ToDateTime(TimeOnly.MinValue);
        SelectedMetal = MetalOptions.FirstOrDefault(x => x.Metal == plan.Metal && x.Purity == plan.Purity);

        OnPropertyChanged(nameof(IsWeightScheme));
        OnPropertyChanged(nameof(EditorTitle));
    }

    private void ShowError(string message, Exception ex)
    {
        _messageBoxService.ShowMessage(
            message + Environment.NewLine + ApiErrorMessage.From(ex),
            "Scheme plan", MessageButton.OK, MessageIcon.Error);
    }
}
