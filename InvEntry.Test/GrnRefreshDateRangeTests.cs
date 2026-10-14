using System.Reflection;
using System.Runtime.CompilerServices;
using DevExpress.Mvvm;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.Utils.Options;
using InvEntry.ViewModels;

namespace InvEntry.Test;

public sealed class GrnRefreshDateRangeTests
{
    [Test]
    public async Task Client_uses_invariant_date_query_parameters()
    {
        string? requestedUrl = null;
        var api = Proxy<IMijmsApiService>((method, args) =>
        {
            if (method.Name == nameof(IMijmsApiService.GetEnumerable))
            {
                requestedUrl = (string)args![0]!;
                return Task.FromResult<IEnumerable<GrnHeader>>([]);
            }
            return DefaultReturn(method.ReturnType);
        });

        await new GrnService(api).GetBySupplier(
            "SUP 1",
            new DateTime(2026, 10, 1),
            new DateTime(2026, 10, 14));

        Assert.That(requestedUrl, Is.EqualTo(
            "api/grn/supplierId/SUP%201?fromDate=2026-10-01&toDate=2026-10-14"));
    }

    [Test]
    public async Task Invalid_range_is_rejected_before_service_call()
    {
        int calls = 0;
        var messages = new MessageBoxServiceFake();
        var viewModel = CreateViewModel(messages, (_, _, _) =>
        {
            calls++;
            return Task.FromResult<IEnumerable<GrnHeader>>([]);
        });
        viewModel.SearchOption.From = new DateTime(2026, 10, 14);
        viewModel.SearchOption.To = new DateTime(2026, 10, 1);

        await InvokeRefresh(viewModel);

        Assert.Multiple(() =>
        {
            Assert.That(calls, Is.Zero);
            Assert.That(messages.LastMessage, Is.EqualTo("From Date cannot be later than To Date."));
        });
    }

    [Test]
    public async Task Empty_result_clears_previous_grns()
    {
        var viewModel = CreateViewModel(new MessageBoxServiceFake(),
            (_, _, _) => Task.FromResult<IEnumerable<GrnHeader>>([]));
        viewModel.GrnHdrList.Add(new GrnHeader { GrnNbr = "OLD" });

        await InvokeRefresh(viewModel);

        Assert.That(viewModel.GrnHdrList, Is.Empty);
    }

    private static ProductStockEntryViewModel CreateViewModel(
        IMessageBoxService messages,
        Func<string, DateTime, DateTime, Task<IEnumerable<GrnHeader>>> getBySupplier)
    {
        var service = Proxy<IGrnService>((method, args) =>
        {
            if (method.Name == nameof(IGrnService.GetBySupplier))
                return getBySupplier((string)args![0]!, (DateTime)args[1]!, (DateTime)args[2]!);
            return DefaultReturn(method.ReturnType);
        });
        var viewModel = (ProductStockEntryViewModel)
            RuntimeHelpers.GetUninitializedObject(typeof(ProductStockEntryViewModel));
        SetField(viewModel, "_grnService", service);
        SetField(viewModel, "_messageBoxService", messages);
        viewModel.SupplierID = "SUP-1";
        viewModel.Today = new DateTime(2026, 10, 14);
        viewModel.SearchOption = new DateSearchOption
        {
            From = new DateTime(2026, 10, 1),
            To = new DateTime(2026, 10, 14)
        };
        viewModel.GrnHdrList = [];
        return viewModel;
    }

    private static async Task InvokeRefresh(ProductStockEntryViewModel viewModel)
    {
        var method = typeof(ProductStockEntryViewModel).GetMethod(
            "RefreshGRN", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        await (Task)method!.Invoke(viewModel, null)!;
    }

    private static void SetField(object target, string name, object value) =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(target, value);

    private static T Proxy<T>(Func<MethodInfo, object?[]?, object?> handler) where T : class
    {
        T proxy = DispatchProxy.Create<T, ServiceProxy<T>>();
        ((ServiceProxy<T>)(object)proxy).Handler = handler;
        return proxy;
    }

    private static object? DefaultReturn(Type returnType)
    {
        if (returnType == typeof(Task)) return Task.CompletedTask;
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            Type resultType = returnType.GetGenericArguments()[0];
            object? result = resultType.IsValueType ? Activator.CreateInstance(resultType) : null;
            return typeof(Task).GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(resultType).Invoke(null, [result]);
        }
        return returnType.IsValueType ? Activator.CreateInstance(returnType) : null;
    }

    private class ServiceProxy<T> : DispatchProxy where T : class
    {
        public Func<MethodInfo, object?[]?, object?>? Handler { get; set; }
        protected override object? Invoke(MethodInfo? method, object?[]? args) =>
            Handler!(method!, args);
    }

    private sealed class MessageBoxServiceFake : IMessageBoxService
    {
        public string? LastMessage { get; private set; }
        public MessageResult Show(string messageBoxText, string caption, MessageButton button,
            MessageIcon icon, MessageResult defaultResult)
        {
            LastMessage = messageBoxText;
            return MessageResult.OK;
        }
    }
}
