using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Xpf.Editors;
using InvEntry.Contracts.Advances;
using InvEntry.Contracts.Schemes;
using InvEntry.Helpers;
using InvEntry.Models;
using InvEntry.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace InvEntry.ViewModels;

/// <summary>
/// Enrolls a customer in a savings scheme (RD) and collects installments.
/// A customer may hold several schemes; each has its own RD account and schedule.
/// </summary>
public partial class SchemeEnrollmentViewModel : ObservableObject
{
    private readonly ICustomerService _customerService;
    private readonly ISchemeService _schemeService;
    private readonly IAdvanceAccountService _advanceAccountService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly IAdvanceReceiptPrinter _receiptPrinter;

    // Kept until the server confirms, so a retry after a failure reuses the
    // same ClientRequestId and cannot enroll or record a payment twice.
    private SchemeEnrollmentRequest? _pendingEnrollment;
    private AdvanceReceiptRequest? _pendingPayment;

    public SchemeEnrollmentViewModel(
        ICustomerService customerService,
        ISchemeService schemeService,
        IAdvanceAccountService advanceAccountService,
        IMessageBoxService messageBoxService,
        IAdvanceReceiptPrinter receiptPrinter)
    {
        _customerService = customerService;
        _schemeService = schemeService;
        _advanceAccountService = advanceAccountService;
        _messageBoxService = messageBoxService;
        _receiptPrinter = receiptPrinter;

        _ = LoadPlansAsync();
    }

    // =========================================================
    // CUSTOMER
    // =========================================================

    [ObservableProperty]
    private string? _customerPhoneNumber;

    [ObservableProperty]
    private Customer? _customer;

    public bool HasCustomer => Customer is not null && Customer.GKey > 0;

    partial void OnCustomerChanged(Customer? value)
    {
        OnPropertyChanged(nameof(HasCustomer));
    }

    [RelayCommand]
    private async Task FetchCustomer(EditValueChangedEventArgs args)
    {
        if (args.NewValue is not string mobile)
            return;

        mobile = mobile.Trim();

        if (mobile.Length < 10)
            return;

        Customer = null;
        CustomerSchemes = new();
        SelectedScheme = null;

        try
        {
            Customer = await _customerService.GetCustomer(mobile);
        }
        catch (Exception ex)
        {
            ShowError("The customer could not be loaded.", ex);
            return;
        }

        if (Customer is null)
        {
            _messageBoxService.ShowMessage(
                "No customer found for this mobile number. Create the customer first (Cash Receipt or billing).",
                "Customer not found", MessageButton.OK, MessageIcon.Exclamation);
            return;
        }

        await LoadCustomerSchemesAsync();
    }

    // =========================================================
    // PLAN AND ENROLLMENT DETAILS
    // =========================================================

    [ObservableProperty]
    private ObservableCollection<SchemePlanDto> _plans = new();

    [ObservableProperty]
    private SchemePlanDto? _selectedPlan;

    [ObservableProperty]
    private decimal _installmentAmount;

    [ObservableProperty]
    private DateTime _startDate = DateTime.Today;

    [ObservableProperty]
    private string? _nomineeName;

    [ObservableProperty]
    private string? _nomineeRelation;

    [ObservableProperty]
    private string? _nomineeMobile;

    [ObservableProperty]
    private bool _collectFirstInstallment = true;

    [ObservableProperty]
    private string _paymentMode = "Cash";

    public ObservableCollection<string> PaymentModes { get; } = new()
    {
        "Cash", "GPAY", "Bank Scanner", "Credit Card", "Debit Card", "Bank"
    };

    public string PlanSummary => SelectedPlan is null ? string.Empty : Describe(SelectedPlan);

    /// <summary>Only ACTIVE plans within their valid dates take enrollments.</summary>
    public string PlanPlaceholder => Plans.Count == 0
        ? "No active plan. Set a plan's status to ACTIVE in Scheme Plans."
        : "Select an active plan";

    partial void OnPlansChanged(ObservableCollection<SchemePlanDto> value)
    {
        OnPropertyChanged(nameof(PlanPlaceholder));
    }

    partial void OnSelectedPlanChanged(SchemePlanDto? value)
    {
        if (value is not null &&
            (InstallmentAmount < value.MinInstallmentAmount ||
             (value.MaxInstallmentAmount is decimal max && InstallmentAmount > max)))
        {
            InstallmentAmount = value.MinInstallmentAmount;
        }

        OnPropertyChanged(nameof(PlanSummary));
    }

    [RelayCommand]
    private async Task LoadPlansAsync()
    {
        try
        {
            var plans = await _schemeService.GetPlans(activeOnly: true);
            Plans = new(plans);

            if (SelectedPlan is not null)
                SelectedPlan = Plans.FirstOrDefault(x => x.Gkey == SelectedPlan.Gkey);
        }
        catch (Exception ex)
        {
            ShowError("Scheme plans could not be loaded.", ex);
        }
    }

    [RelayCommand]
    private async Task EnrollAsync()
    {
        if (!HasCustomer)
        {
            _messageBoxService.ShowMessage("Enter the customer's mobile number first.", "Enrollment", MessageButton.OK, MessageIcon.Exclamation);
            return;
        }

        if (SelectedPlan is null)
        {
            _messageBoxService.ShowMessage("Select the scheme plan.", "Enrollment", MessageButton.OK, MessageIcon.Exclamation);
            return;
        }

        var startDate = DateOnly.FromDateTime(StartDate);

        if (_pendingEnrollment is null ||
            _pendingEnrollment.CustomerGkey != Customer!.GKey ||
            _pendingEnrollment.SchemePlanGkey != SelectedPlan.Gkey ||
            _pendingEnrollment.InstallmentAmount != InstallmentAmount ||
            _pendingEnrollment.StartDate != startDate)
        {
            _pendingEnrollment = new SchemeEnrollmentRequest
            {
                ClientRequestId = Guid.NewGuid(),
                CustomerGkey = (int)Customer!.GKey,
                SchemePlanGkey = SelectedPlan.Gkey,
                InstallmentAmount = InstallmentAmount,
                StartDate = startDate
            };
        }

        _pendingEnrollment.NomineeName = NomineeName;
        _pendingEnrollment.NomineeRelation = NomineeRelation;
        _pendingEnrollment.NomineeMobile = NomineeMobile;

        SchemeEnrollmentDto enrollment;

        try
        {
            enrollment = await _schemeService.Enroll(_pendingEnrollment);
            _pendingEnrollment = null;
        }
        catch (Exception ex)
        {
            ShowError("The enrollment was not saved. Saving again will not enroll twice.", ex);
            return;
        }

        var message =
            $"Enrolled in {enrollment.PlanName}. Account {enrollment.AccountNbr}, " +
            $"{enrollment.TenureMonths} installments of Rs. {enrollment.InstallmentAmount:N2}, " +
            $"maturity {enrollment.MaturityDate:dd-MM-yyyy}.";

        await LoadCustomerSchemesAsync();
        SelectedScheme = CustomerSchemes.FirstOrDefault(x => x.EnrollmentGkey == enrollment.EnrollmentGkey);

        if (CollectFirstInstallment && SelectedScheme is not null)
        {
            _messageBoxService.ShowMessage(message, "Enrollment", MessageButton.OK, MessageIcon.Information);
            PaymentAmount = enrollment.InstallmentAmount;
            await CollectPaymentAsync();
            return;
        }

        _messageBoxService.ShowMessage(message, "Enrollment", MessageButton.OK, MessageIcon.Information);
        ClearEnrollmentForm();
    }

    // =========================================================
    // CUSTOMER SCHEMES AND PAYMENTS
    // =========================================================

    [ObservableProperty]
    private ObservableCollection<SchemeEnrollmentDto> _customerSchemes = new();

    [ObservableProperty]
    private SchemeEnrollmentDto? _selectedScheme;

    [ObservableProperty]
    private decimal _paymentAmount;

    partial void OnSelectedSchemeChanged(SchemeEnrollmentDto? value)
    {
        PaymentAmount = value?.NextInstallment?.AmountOutstanding ?? 0M;
    }

    private async Task LoadCustomerSchemesAsync()
    {
        if (!HasCustomer)
            return;

        var selected = SelectedScheme?.EnrollmentGkey;

        try
        {
            CustomerSchemes = new(await _schemeService.GetCustomerEnrollments((int)Customer!.GKey));
            SelectedScheme = CustomerSchemes.FirstOrDefault(x => x.EnrollmentGkey == selected)
                             ?? (CustomerSchemes.Count == 1 ? CustomerSchemes[0] : null);
        }
        catch (Exception ex)
        {
            ShowError("The customer's schemes could not be loaded.", ex);
        }
    }

    [RelayCommand]
    private async Task CollectPaymentAsync()
    {
        if (SelectedScheme is null)
        {
            _messageBoxService.ShowMessage("Select the scheme to pay.", "Installment", MessageButton.OK, MessageIcon.Exclamation);
            return;
        }

        if (PaymentAmount <= 0M)
        {
            _messageBoxService.ShowMessage("Enter the amount received.", "Installment", MessageButton.OK, MessageIcon.Exclamation);
            return;
        }

        if (_pendingPayment is null ||
            _pendingPayment.AdvanceAccountGkey != SelectedScheme.AdvanceAccountGkey ||
            _pendingPayment.Amount != PaymentAmount ||
            _pendingPayment.Mode != PaymentMode)
        {
            _pendingPayment = new AdvanceReceiptRequest
            {
                ClientRequestId = Guid.NewGuid(),
                CustomerGkey = SelectedScheme.CustomerGkey,
                ReceiptType = AdvanceReceiptType.RecurringDeposit,
                AdvanceAccountGkey = SelectedScheme.AdvanceAccountGkey,
                Amount = PaymentAmount,
                Mode = PaymentMode,
                TransDesc = $"{SelectedScheme.PlanCode} installment"
            };
        }

        try
        {
            var result = await _advanceAccountService.CreateReceipt(_pendingPayment);
            _pendingPayment = null;

            var message = $"Receipt {result.VoucherNbr}: Rs. {result.Amount:N2} into {result.AccountNbr}.";

            if (result.Weight > 0M)
                message += $"{Environment.NewLine}{result.Weight:N3} g at Rs. {result.MetalRate:N2}/g; total {result.BalanceWeight:N3} g.";

            if (result.InstallmentsPaid is int paid)
                message += $"{Environment.NewLine}Installments paid: {paid} of {SelectedScheme.TenureMonths}.";

            message += $"{Environment.NewLine}{Environment.NewLine}Print the receipt now?";

            if (_messageBoxService.ShowMessage(message, "Installment received", MessageButton.YesNo, MessageIcon.Question)
                == MessageResult.Yes)
            {
                await _receiptPrinter.PreviewAsync(result.VoucherGkey, isReprint: result.IsReplay);
            }
        }
        catch (Exception ex)
        {
            ShowError("The payment was not recorded. Saving again will not record it twice.", ex);
            return;
        }

        await LoadCustomerSchemesAsync();
        ClearEnrollmentForm();
    }

    [RelayCommand]
    private void Reset()
    {
        CustomerPhoneNumber = string.Empty;
        Customer = null;
        CustomerSchemes = new();
        SelectedScheme = null;
        PaymentAmount = 0M;
        _pendingPayment = null;
        ClearEnrollmentForm();
    }

    private void ClearEnrollmentForm()
    {
        _pendingEnrollment = null;
        NomineeName = null;
        NomineeRelation = null;
        NomineeMobile = null;
        StartDate = DateTime.Today;
    }

    private void ShowError(string message, Exception ex)
    {
        _messageBoxService.ShowMessage(
            message + Environment.NewLine + ApiErrorMessage.From(ex),
            "Scheme", MessageButton.OK, MessageIcon.Error);
    }

    public static string Describe(SchemePlanDto plan)
    {
        var kind = plan.SchemeType == SchemeType.Weight
            ? $"Weight scheme ({plan.Metal} {plan.Purity}): each payment is converted to grams at the day's rate."
            : "Amount scheme.";

        var amount = plan.AmountFixed
            ? $"Fixed monthly amount, at least Rs. {plan.MinInstallmentAmount:N2}"
            : $"Any amount each month, at least Rs. {plan.MinInstallmentAmount:N2}";

        if (plan.MaxInstallmentAmount is decimal max)
            amount += $", at most Rs. {max:N2}";

        var benefits = plan.BenefitRules.Count == 0
            ? "No benefits."
            : "Benefits: " + string.Join("; ", plan.BenefitRules.Select(DescribeBenefit)) + ".";

        return $"{kind} {plan.TenureMonths} months. {amount}. {benefits}";
    }

    private static string DescribeBenefit(SchemeBenefitRuleDto rule)
    {
        var what = rule.BenefitType switch
        {
            "INSTALLMENT_WAIVER" => $"{rule.BenefitValue:0.##} installment(s) paid by the shop",
            "PERCENT_BONUS" => $"{rule.BenefitValue:0.##}% bonus",
            "MAKING_CHARGE_WAIVER" => $"{rule.BenefitValue:0.##}% off making charges",
            "BONUS_GRAMS" => $"{rule.BenefitValue:0.###} g bonus",
            "FLAT_BONUS" => $"Rs. {rule.BenefitValue:N2} bonus",
            _ => rule.BenefitType
        };

        var when = rule.AppliesOn == "PRECLOSURE" ? "on pre-closure" : "on maturity";
        var condition = rule.MinInstallmentsPaid > 0 ? $" after {rule.MinInstallmentsPaid} installments" : string.Empty;

        return $"{what} {when}{condition}";
    }
}
