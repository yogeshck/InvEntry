using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Xpf.Editors;
using InvEntry.Contracts.Advances;
using InvEntry.Contracts.Schemes;
using InvEntry.Extension;
using InvEntry.Helpers;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.Services.Customers;
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
    private readonly ICustomerLookupService _customerLookupService;
    private readonly ISchemeService _schemeService;
    private readonly IAdvanceAccountService _advanceAccountService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly IAdvanceReceiptPrinter _receiptPrinter;
    private readonly IRdStatementPrinter _statementPrinter;
    private readonly ICustomerEditorDialog _customerEditorDialog;
    private readonly IOrgThisCompanyViewService _orgThisCompanyViewService;

    // Kept until the server confirms, so a retry after a failure reuses the
    // same ClientRequestId and cannot enroll or record a payment twice.
    private SchemeEnrollmentRequest? _pendingEnrollment;
    private AdvanceReceiptRequest? _pendingPayment;

    public SchemeEnrollmentViewModel(
        ICustomerLookupService customerLookupService,
        ISchemeService schemeService,
        IAdvanceAccountService advanceAccountService,
        IMessageBoxService messageBoxService,
        IAdvanceReceiptPrinter receiptPrinter,
        IRdStatementPrinter statementPrinter,
        ICustomerEditorDialog customerEditorDialog,
        IOrgThisCompanyViewService orgThisCompanyViewService)
    {
        _customerLookupService = customerLookupService;
        _schemeService = schemeService;
        _advanceAccountService = advanceAccountService;
        _messageBoxService = messageBoxService;
        _receiptPrinter = receiptPrinter;
        _statementPrinter = statementPrinter;
        _customerEditorDialog = customerEditorDialog;
        _orgThisCompanyViewService = orgThisCompanyViewService;

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

    public string? CustomerAddress
    {
        get
        {
            var address = Customer?.Address;
            if (address is null)
                return null;

            var parts = new[] { address.AddressLine1, address.AddressLine2, address.Area, address.City }
                .Where(x => !string.IsNullOrWhiteSpace(x));

            return string.Join(", ", parts);
        }
    }

    partial void OnCustomerChanged(Customer? value)
    {
        OnPropertyChanged(nameof(HasCustomer));
        OnPropertyChanged(nameof(CustomerAddress));
    }

    [RelayCommand]
    private async Task FetchCustomer(EditValueChangedEventArgs args)
    {
        if (args.NewValue is not string mobile)
            return;

        await LoadCustomerByMobileAsync(mobile);
    }

    public async Task LoadCustomerByMobileAsync(string mobile)
    {
        mobile = mobile.Trim();

        if (mobile.Length < 10)
            return;

        // Setting the mobile box after a new customer is saved raises the
        // lookup again; that customer is already loaded.
        if (HasCustomer && string.Equals(Customer!.MobileNbr, mobile, StringComparison.Ordinal))
            return;

        var previousCustomer = Customer;
        var previousSchemes = CustomerSchemes;
        var previousScheme = SelectedScheme;

        Customer = null;
        CustomerSchemes = new();
        SelectedScheme = null;

        Customer? found;

        try
        {
            // Same lookup as Invoice and Customer Order: an unknown mobile
            // (HTTP 404) comes back as not existing instead of an error.
            var lookup = await _customerLookupService.ResolveByMobileAsync(mobile);
            found = lookup.IsExisting ? lookup.Customer : null;
        }
        catch (Exception ex)
        {
            ShowError("The customer could not be loaded.", ex);
            return;
        }

        if (found is null)
        {
            found = await CreateCustomerAsync(mobile);

            if (found is null)
            {
                // Cancelled: put back whoever was on the form before.
                Customer = previousCustomer;
                CustomerSchemes = previousSchemes;
                SelectedScheme = previousScheme;

                if (previousCustomer is not null)
                    CustomerPhoneNumber = previousCustomer.MobileNbr;

                return;
            }
        }

        Customer = found;
        CustomerPhoneNumber = found.MobileNbr;

        await LoadCustomerSchemesAsync();
    }

    /// <summary>
    /// Opens the same new-customer popup Invoice and Customer Order use.
    /// Returns the saved customer, or null if the user cancelled.
    /// </summary>
    private async Task<Customer?> CreateCustomerAsync(string mobile)
    {
        var customer = new Customer { MobileNbr = mobile };
        customer.Address ??= new OrgAddress();

        try
        {
            var company = await _orgThisCompanyViewService.GetOrgThisCompany();

            if (company is not null)
            {
                customer.Address.GstStateCode = company.GstCode;
                customer.Address.State = company.State;
                customer.Address.District = company.District;
                customer.GstStateCode = company.GstCode;
            }
        }
        catch
        {
            // Company defaults only prefill the state; the user can still pick it.
        }

        Customer? saved;

        try
        {
            saved = await _customerEditorDialog.EditCustomerAsync(customer, isNewCustomer: true);
        }
        catch (Exception ex)
        {
            ShowError("The customer could not be created.", ex);
            return null;
        }

        if (saved is null || saved.GKey <= 0)
            return null;

        saved.Address ??= new OrgAddress();
        return saved;
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
    private async Task PrintPassbookAsync()
    {
        if (SelectedScheme is null)
        {
            _messageBoxService.ShowMessage("Select the RD scheme to print.", "RD passbook", MessageButton.OK, MessageIcon.Exclamation);
            return;
        }

        await _statementPrinter.PreviewAsync(SelectedScheme.EnrollmentGkey);
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
