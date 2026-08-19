using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvEntry.Helpers;
using InvEntry.Models;
using InvEntry.Services.Customers;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace InvEntry.ViewModels.Common;

public partial class CustomerEditViewModel : ObservableObject
{
    private readonly ICustomerLookupService _customerLookupService;
    private readonly ReferenceLoader _referenceLoader;

    private CustomerEditSnapshot? _originalSnapshot;

    public bool HasChanges =>
        CurrentCustomer is not null &&
        _originalSnapshot is not null &&
        !_originalSnapshot.Equals(
            CustomerEditSnapshot.From(CurrentCustomer));

    private bool _initializing;

    public CustomerEditViewModel(
        ICustomerLookupService customerLookupService,
        ReferenceLoader referenceLoader)
    {
        _customerLookupService = customerLookupService;
        _referenceLoader = referenceLoader;
    }

    // ---------------------------------------------------------
    // Customer
    // ---------------------------------------------------------

    [ObservableProperty]
    private Customer? currentCustomer;

    [ObservableProperty]
    private bool isNewCustomer;

    // ---------------------------------------------------------
    // State reference
    // ---------------------------------------------------------

    [ObservableProperty]
    private ObservableCollection<string> stateReferencesList = new();

    [ObservableProperty]
    private string? customerState;

    // ---------------------------------------------------------
    // UI State
    // ---------------------------------------------------------

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? errorMessage;

    // ---------------------------------------------------------
    // Result
    // ---------------------------------------------------------

    public bool WasSaved { get; private set; }

    public event EventHandler<CustomerSavedEventArgs>? Saved;

    public event EventHandler? Cancelled;

    // ---------------------------------------------------------
    // Initialize
    // ---------------------------------------------------------

    public async Task InitializeAsync(
        Customer customer,
        bool isNewCustomer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        try
        {
            _initializing = true;

            customer.Address ??= new OrgAddress();

            CurrentCustomer = customer;
            IsNewCustomer = isNewCustomer;

            ErrorMessage = null;
            IsBusy = false;
            WasSaved = false;

            StateReferencesList =
                await _referenceLoader.LoadValuesAsync(
                    "CUST_STATE");

            var gstStateCode =
                customer.Address.GstStateCode
                ?? customer.GstStateCode;

            if (!string.IsNullOrWhiteSpace(gstStateCode))
            {
                CustomerState =
                    await _referenceLoader.GetValueAsync(
                        "CUST_STATE",
                        gstStateCode);

                customer.Address.GstStateCode =
                    gstStateCode;

                customer.GstStateCode =
                    gstStateCode;

                if (!string.IsNullOrWhiteSpace(CustomerState))
                {
                    customer.Address.State =
                        CustomerState;
                }
            }
            else if (!string.IsNullOrWhiteSpace(
                         customer.Address.State))
            {
                CustomerState =
                    customer.Address.State;

                var code =
                    await _referenceLoader.GetCodeAsync(
                        "CUST_STATE",
                        CustomerState);

                if (!string.IsNullOrWhiteSpace(code))
                {
                    customer.Address.GstStateCode = code;
                    customer.GstStateCode = code;
                }
            }

            _originalSnapshot =
    CustomerEditSnapshot.From(customer);

        }
        finally
        {
            _initializing = false;
        }
    }

    // ---------------------------------------------------------
    // State changed
    // ---------------------------------------------------------

    partial void OnCustomerStateChanged(
        string? value)
    {
        if (_initializing)
            return;

        if (string.IsNullOrWhiteSpace(value))
            return;

        if (CurrentCustomer is null)
            return;

        _ = ApplyCustomerStateAsync(value);
    }

    private async Task ApplyCustomerStateAsync(
        string state)
    {
        if (CurrentCustomer is null)
            return;

        try
        {
            CurrentCustomer.Address ??=
                new OrgAddress();

            var gstCode =
                await _referenceLoader.GetCodeAsync(
                    "CUST_STATE",
                    state);

            if (string.IsNullOrWhiteSpace(gstCode))
            {
                ErrorMessage =
                    $"GST state code is not configured for '{state}'.";

                return;
            }

            CurrentCustomer.Address.State =
                state;

            CurrentCustomer.Address.GstStateCode =
                gstCode;

            // Temporary compatibility field.
            CurrentCustomer.GstStateCode =
                gstCode;

            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    // ---------------------------------------------------------
    // Save
    // ---------------------------------------------------------

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (CurrentCustomer is null)
        {
            ErrorMessage =
                "Customer information is not available.";

            return;
        }

        ErrorMessage = null;

        if (!await ValidateCustomerAsync())
            return;

        try
        {
            IsBusy = true;

            Customer savedCustomer;

            if (IsNewCustomer ||
                CurrentCustomer.GKey <= 0)
            {
                savedCustomer =
                    await _customerLookupService.CreateAsync(
                        CurrentCustomer);
            }
            else
            {
                savedCustomer =
                    await _customerLookupService.UpdateAsync(
                        CurrentCustomer);
            }

            if (savedCustomer is null ||
                savedCustomer.GKey <= 0)
            {
                ErrorMessage =
                    "Customer could not be saved.";

                return;
            }

            //
            // Customer.Address is JsonIgnore.
            // Preserve the address currently edited in the modal.
            //
            savedCustomer.Address ??=
                CurrentCustomer.Address;

            CurrentCustomer =
                savedCustomer;

            _originalSnapshot =
                    CustomerEditSnapshot.From(savedCustomer);

            WasSaved = true;
            IsNewCustomer = false;

            Saved?.Invoke(
                this,
                new CustomerSavedEventArgs(savedCustomer));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ---------------------------------------------------------
    // Cancel
    // ---------------------------------------------------------

    [RelayCommand]
    private void Cancel()
    {
        ErrorMessage = null;
        //WasSaved = false;

        Cancelled?.Invoke(
            this,
            EventArgs.Empty);
    }


    // ---------------------------------------------------------
    // Validation
    // ---------------------------------------------------------

    private async Task<bool> ValidateCustomerAsync()
    {
        if (CurrentCustomer is null)
            return false;

        if (string.IsNullOrWhiteSpace(
                CurrentCustomer.MobileNbr))
        {
            ErrorMessage =
                "Mobile number is required.";

            return false;
        }

        CurrentCustomer.MobileNbr =
            CurrentCustomer.MobileNbr.Trim();

        if (CurrentCustomer.MobileNbr.Length < 10)
        {
            ErrorMessage =
                "Enter a valid mobile number.";

            return false;
        }

        if (string.IsNullOrWhiteSpace(
                CurrentCustomer.CustomerName))
        {
            ErrorMessage =
                "Customer name is required.";

            return false;
        }

        CurrentCustomer.CustomerName =
            CurrentCustomer.CustomerName.Trim();

        CurrentCustomer.Address ??=
            new OrgAddress();

        if (string.IsNullOrWhiteSpace(CustomerState))
        {
            ErrorMessage =
                "Customer state is required.";

            return false;
        }

        //
        // Resolve again at save time.
        // Avoids race with async OnCustomerStateChanged().
        //
        var gstCode =
            await _referenceLoader.GetCodeAsync(
                "CUST_STATE",
                CustomerState);

        if (string.IsNullOrWhiteSpace(gstCode))
        {
            ErrorMessage =
                $"GST state code is not configured for '{CustomerState}'.";

            return false;
        }

        CurrentCustomer.Address.State =
            CustomerState;

        CurrentCustomer.Address.GstStateCode =
            gstCode;

        CurrentCustomer.GstStateCode =
            gstCode;

        return true;
    }

    internal sealed record CustomerEditSnapshot(
    string? MobileNbr,
    string? CustomerName,
    string? CustomerType,
    string? GstinNbr,
    string? PanNbr,
    string? Salutations,
    string? AddressLine1,
    string? AddressLine2,
    string? AddressLine3,
    string? Area,
    string? City,
    string? District,
    string? State,
    string? Country,
    string? GstStateCode)
    {
        public static CustomerEditSnapshot From(
            Customer customer)
        {
            var address = customer.Address;

            return new CustomerEditSnapshot(
                customer.MobileNbr,
                customer.CustomerName,
                customer.CustomerType,
                customer.GstinNbr,
                customer.PanNbr,
                customer.Salutations,

                address?.AddressLine1,
                address?.AddressLine2,
                address?.AddressLine3,
                address?.Area,
                address?.City,
                address?.District,
                address?.State,
                address?.Country,
                address?.GstStateCode);
        }
    }

}

public sealed class CustomerSavedEventArgs : EventArgs
{
    public CustomerSavedEventArgs(
        Customer customer)
    {
        Customer =
            customer ??
            throw new ArgumentNullException(
                nameof(customer));
    }

    public Customer Customer { get; }
}