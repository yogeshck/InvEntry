using DevExpress.Mvvm;
using InvEntry.Extension;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.Services.Customers;
using InvEntry.ViewModels;
using System.Net;
using System.Reflection;

namespace InvEntry.Test;

[TestFixture]
public sealed class SchemeEnrollmentCustomerTests
{
    private const string ExistingMobile = "9876543210";
    private const string UnknownMobile = "9123456789";

    [Test]
    public async Task UnknownMobileOpensNewCustomerPopupAndLoadsSavedCustomer()
    {
        var customers = new FakeCustomerService();
        var dialog = new FakeCustomerEditorDialog(customer =>
        {
            customer.GKey = 42;
            customer.CustomerName = "NEW CUSTOMER";
            customer.Address.AddressLine1 = "12 Main Road";
            customer.Address.City = "Thrissur";
            return customer;
        });
        var viewModel = CreateViewModel(customers, dialog);

        await viewModel.LoadCustomerByMobileAsync(UnknownMobile);

        Assert.That(dialog.Calls, Is.EqualTo(1));
        Assert.That(dialog.LastIsNewCustomer, Is.True);
        Assert.That(dialog.LastCustomer!.MobileNbr, Is.EqualTo(UnknownMobile));
        Assert.That(dialog.LastCustomer.Address.GstStateCode, Is.EqualTo("32"));
        Assert.That(viewModel.HasCustomer, Is.True);
        Assert.That(viewModel.Customer!.GKey, Is.EqualTo(42));
        Assert.That(viewModel.Customer.CustomerName, Is.EqualTo("NEW CUSTOMER"));
        Assert.That(viewModel.CustomerPhoneNumber, Is.EqualTo(UnknownMobile));
        Assert.That(viewModel.CustomerAddress, Is.EqualTo("12 Main Road, Thrissur"));
    }

    [Test]
    public async Task CancellingNewCustomerPopupKeepsPreviousCustomer()
    {
        var customers = new FakeCustomerService();
        var dialog = new FakeCustomerEditorDialog(_ => null);
        var viewModel = CreateViewModel(customers, dialog);

        await viewModel.LoadCustomerByMobileAsync(ExistingMobile);
        var previous = viewModel.Customer;

        await viewModel.LoadCustomerByMobileAsync(UnknownMobile);

        Assert.That(dialog.Calls, Is.EqualTo(1));
        Assert.That(viewModel.Customer, Is.SameAs(previous));
        Assert.That(viewModel.CustomerPhoneNumber, Is.EqualTo(ExistingMobile));
    }

    [Test]
    public async Task CancellingWithNoPreviousCustomerLeavesFormEmpty()
    {
        var dialog = new FakeCustomerEditorDialog(_ => null);
        var viewModel = CreateViewModel(new FakeCustomerService(), dialog);

        await viewModel.LoadCustomerByMobileAsync(UnknownMobile);

        Assert.That(viewModel.Customer, Is.Null);
        Assert.That(viewModel.HasCustomer, Is.False);
    }

    [Test]
    public async Task ExistingMobileLoadsCustomerWithoutPopup()
    {
        var dialog = new FakeCustomerEditorDialog(_ => throw new AssertionException("Popup should not open."));
        var viewModel = CreateViewModel(new FakeCustomerService(), dialog);

        await viewModel.LoadCustomerByMobileAsync(ExistingMobile);

        Assert.That(dialog.Calls, Is.Zero);
        Assert.That(viewModel.Customer!.GKey, Is.EqualTo(7));
        Assert.That(viewModel.HasCustomer, Is.True);
    }

    private static SchemeEnrollmentViewModel CreateViewModel(
        ICustomerService customers,
        ICustomerEditorDialog dialog)
    {
        var company = Stub.Create<IOrgThisCompanyViewService>(
            (method, _) => method.Name == nameof(IOrgThisCompanyViewService.GetOrgThisCompany)
                ? Task.FromResult(new OrgThisCompanyView { GstCode = "32", State = "Kerala", District = "Thrissur" })
                : null);

        return new SchemeEnrollmentViewModel(
            new CustomerLookupService(customers),
            Stub.Create<ISchemeService>(),
            Stub.Create<IAdvanceAccountService>(),
            Stub.Create<IMessageBoxService>(),
            Stub.Create<IAdvanceReceiptPrinter>(),
            Stub.Create<IRdStatementPrinter>(),
            dialog,
            company);
    }

    private sealed class FakeCustomerService : ICustomerService
    {
        // The API answers an unknown mobile with HTTP 404.
        public Task<Customer> GetCustomer(string mobileNbr)
            => mobileNbr == ExistingMobile
                ? Task.FromResult(new Customer { GKey = 7, MobileNbr = ExistingMobile, CustomerName = "EXISTING" })
                : Task.FromException<Customer>(new HttpRequestException(
                    $"GET '/api/customer/{mobileNbr}' failed with HTTP 404 (NotFound).",
                    null,
                    HttpStatusCode.NotFound));

        public Task<List<Customer>> GetCustomers(List<string> mobileNumbers) => Task.FromResult(new List<Customer>());
        public Task<Customer?> GetCustomerByGkey(int customerGkey) => Task.FromResult<Customer?>(null);
        public Task<Customer> CreateCustomer(Customer customer) => Task.FromResult(customer);
        public Task UpdateCustomer(Customer customer) => Task.CompletedTask;
    }

    private sealed class FakeCustomerEditorDialog(Func<Customer, Customer?> onShow) : ICustomerEditorDialog
    {
        public int Calls { get; private set; }
        public Customer? LastCustomer { get; private set; }
        public bool LastIsNewCustomer { get; private set; }

        public Task<Customer?> EditCustomerAsync(Customer customer, bool isNewCustomer)
        {
            Calls++;
            LastCustomer = customer;
            LastIsNewCustomer = isNewCustomer;
            return Task.FromResult(onShow(customer));
        }
    }

    /// <summary>
    /// Interface stub whose methods return completed tasks with empty values,
    /// for services these tests do not exercise.
    /// </summary>
    private class Stub : DispatchProxy
    {
        private Func<MethodInfo, object?[]?, object?>? _handler;

        public static T Create<T>(Func<MethodInfo, object?[]?, object?>? handler = null) where T : class
        {
            var proxy = Create<T, Stub>();
            ((Stub)(object)proxy)._handler = handler;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod is null)
                return null;

            if (_handler?.Invoke(targetMethod, args) is { } result)
                return result;

            var returnType = targetMethod.ReturnType;

            if (returnType == typeof(Task))
                return Task.CompletedTask;

            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var valueType = returnType.GetGenericArguments()[0];
                var value = EmptyValue(valueType);
                return typeof(Task).GetMethod(nameof(Task.FromResult))!
                    .MakeGenericMethod(valueType)
                    .Invoke(null, [value]);
            }

            return EmptyValue(returnType);
        }

        private static object? EmptyValue(Type type)
        {
            if (type == typeof(void) || type == typeof(string))
                return null;

            if (type.IsValueType)
                return Activator.CreateInstance(type);

            if (!type.IsAbstract && !type.IsInterface && type.GetConstructor(Type.EmptyTypes) is not null)
                return Activator.CreateInstance(type);

            return null;
        }
    }
}
