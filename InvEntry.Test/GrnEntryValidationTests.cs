using System.Reflection;
using DevExpress.Mvvm;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.ViewModels;

namespace InvEntry.Test;

[TestFixture]
public sealed class GrnEntryValidationTests
{
    [Test]
    public void ValidHeaderWithNoLines_SaveCanExecuteIsFalse()
    {
        var fixture = CreateFixture();

        Assert.That(fixture.ViewModel.SubmitCommand.CanExecute(null), Is.False);
    }

    [Test]
    public async Task ProgrammaticSaveWithNoLines_IsRejectedBeforePersistence()
    {
        var fixture = CreateFixture();

        await InvokeSubmit(fixture.ViewModel);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Grn.CreateHeaderCalls, Is.Zero);
            Assert.That(fixture.Messages.LastMessage,
                Is.EqualTo("Please add at least one item before saving the GRN."));
        });
    }

    [TestCase(0)]
    [TestCase(-1)]
    public async Task NonPositiveGrossWeight_IsRejectedBeforePersistence(decimal gross)
    {
        var fixture = CreateFixture();
        fixture.ViewModel.Header.GrnLineSumry!.Add(CreateLine(gross, 0M));

        await InvokeSubmit(fixture.ViewModel);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.ViewModel.SubmitCommand.CanExecute(null), Is.False);
            Assert.That(fixture.Grn.CreateHeaderCalls, Is.Zero);
            Assert.That(fixture.Messages.LastMessage,
                Does.Contain("Gross Weight must be greater than zero."));
        });
    }

    [Test]
    public async Task MissingGrossWeight_IsRejectedBeforePersistence()
    {
        var fixture = CreateFixture();
        var line = CreateLine(5M, 1M);
        line.GrossWeight = null;
        fixture.ViewModel.Header.GrnLineSumry!.Add(line);

        await InvokeSubmit(fixture.ViewModel);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Grn.CreateHeaderCalls, Is.Zero);
            Assert.That(fixture.Messages.LastMessage,
                Does.Contain("Gross Weight must be greater than zero."));
        });
    }

    [Test]
    public void FirstValidLineEnablesSave_AndRemovingLastLineDisablesIt()
    {
        var fixture = CreateFixture();
        int notifications = 0;
        fixture.ViewModel.SubmitCommand.CanExecuteChanged += (_, _) => notifications++;

        var line = CreateLine(5M, 1M);
        fixture.ViewModel.Header.GrnLineSumry!.Add(line);
        bool enabledAfterAdd = fixture.ViewModel.SubmitCommand.CanExecute(null);
        fixture.ViewModel.Header.GrnLineSumry.Remove(line);

        Assert.Multiple(() =>
        {
            Assert.That(enabledAfterAdd, Is.True);
            Assert.That(fixture.ViewModel.SubmitCommand.CanExecute(null), Is.False);
            Assert.That(notifications, Is.GreaterThanOrEqualTo(2));
        });
    }

    [Test]
    public void ValidLineChangedToZeroGross_DisablesSaveImmediately()
    {
        var fixture = CreateFixture();
        var line = CreateLine(5M, 1M);
        fixture.ViewModel.Header.GrnLineSumry!.Add(line);
        Assert.That(fixture.ViewModel.SubmitCommand.CanExecute(null), Is.True);

        line.GrossWeight = 0M;

        Assert.That(fixture.ViewModel.SubmitCommand.CanExecute(null), Is.False);
    }

    [Test]
    public async Task InvalidLineAlreadyInCollection_SaveRejectsBeforePersistence()
    {
        var fixture = CreateFixture();
        fixture.ViewModel.Header.GrnLineSumry!.Add(CreateLine(5M, 6M));

        await InvokeSubmit(fixture.ViewModel);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Grn.CreateHeaderCalls, Is.Zero);
            Assert.That(fixture.Messages.LastMessage,
                Does.Contain("Stone Weight cannot exceed Gross Weight."));
        });
    }

    [Test]
    public async Task ValidMaterialReceipt_UsesExistingSaveWorkflow()
    {
        var fixture = CreateFixture();
        fixture.ViewModel.Header.SupplierId = "SUPPLIER";
        fixture.ViewModel.Header.GrnLineSumry!.Add(CreateLine(5M, 1M));

        await fixture.ViewModel.SubmitCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Grn.CreateHeaderCalls, Is.EqualTo(1));
            Assert.That(fixture.Grn.CreateLineSummaryCalls, Is.EqualTo(1));
            Assert.That(fixture.ProductStockCreateCalls, Is.EqualTo(1));
        });
    }

    private static IEnumerable<TestCaseData> MultiLineCases()
    {
        yield return new TestCaseData(new[] { 1 }).SetName("GRN_OneLine_Qty1");
        yield return new TestCaseData(new[] { 1, 1 }).SetName("GRN_TwoLines_Qty1Each");
        yield return new TestCaseData(new[] { 1, 1, 1 }).SetName("GRN_ThreeLines_Qty1Each");
        yield return new TestCaseData(new[] { 4 }).SetName("GRN_OneLine_QtyGreaterThan1");
        yield return new TestCaseData(new[] { 1, 4, 45 }).SetName("GRN_ThreeLines_Qty1_4_45");
    }

    [TestCaseSource(nameof(MultiLineCases))]
    public async Task CleanFollowUpState_ProcessesEveryLineAndExpandsQuantity(int[] quantities)
    {
        var fixture = CreateFixture();
        for (int index = 0; index < quantities.Length; index++)
        {
            var line = CreateLine(5M, 1M);
            line.ProductGkey = index + 1;
            line.ProductCategory = $"CATEGORY-{index + 1}";
            line.SuppliedQty = quantities[index];
            fixture.ViewModel.Header.GrnLineSumry!.Add(line);
        }

        await fixture.ViewModel.SubmitCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Grn.CreateHeaderCalls, Is.EqualTo(1));
            Assert.That(fixture.Grn.SavedLineCount, Is.EqualTo(quantities.Length));
            Assert.That(fixture.ProductTransactionCreateCalls, Is.EqualTo(quantities.Length));
            Assert.That(fixture.ProductStockCreateCalls, Is.EqualTo(quantities.Sum()));
        });
    }

    [Test]
    public async Task RepeatedCategoryLines_CreateDistinctLineLinkedTransactions()
    {
        var fixture = CreateFixture();
        fixture.ViewModel.Header.GrnLineSumry!.Add(CreateLine(5M, 1M));
        fixture.ViewModel.Header.GrnLineSumry.Add(CreateLine(7M, 2M));

        await fixture.ViewModel.SubmitCommand.ExecuteAsync(null);

        Assert.That(
            fixture.ProductTransactionSourceKeys,
            Is.EqualTo(new[] { 11, 12 }));
    }

    [Test]
    public async Task DuplicateDailySummary_ReproducesFailureWithOneLineQtyOne()
    {
        var fixture = CreateFixture(dailySummaryCount: 2);
        fixture.ViewModel.Header.GrnLineSumry!.Add(CreateLine(5M, 1M));

        await fixture.ViewModel.SubmitCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Grn.CreateHeaderCalls, Is.EqualTo(1));
            Assert.That(fixture.Grn.SavedLineCount, Is.EqualTo(1));
            Assert.That(fixture.ProductTransactionCreateCalls, Is.EqualTo(1));
            Assert.That(fixture.ProductStockCreateCalls, Is.Zero);
            Assert.That(fixture.Messages.LastMessage,
                Does.Contain("follow-up processing could not be completed"));
        });
    }

    private static GrnLineSummary CreateLine(decimal gross, decimal stone) => new()
    {
        ProductGkey = 10,
        ProductCategory = "RING",
        ProductPurity = "22K",
        Uom = "Grams",
        SuppliedQty = 1,
        GrossWeight = gross,
        StoneWeight = stone,
        NetWeight = Math.Round(gross - stone, 3, MidpointRounding.AwayFromZero)
    };

    private static async Task InvokeSubmit(GRNViewModel viewModel)
    {
        var method = typeof(GRNViewModel).GetMethod(
            "Submit",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        await (Task)method!.Invoke(viewModel, null)!;
    }

    private static Fixture CreateFixture(int dailySummaryCount = 0)
    {
        var grnState = new GrnServiceState();
        var stockSummaryReads = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int productStockCreates = 0;
        int productTransactionCreates = 0;
        var productTransactionSourceKeys = new List<int?>();

        var grn = Proxy<IGrnService>((method, args) => method.Name switch
        {
            nameof(IGrnService.CreateHeader) => grnState.CreateHeader((GrnHeader)args![0]!),
            nameof(IGrnService.CreateGrnLineSummary) when args![0] is IEnumerable<GrnLineSummary> lines =>
                grnState.CreateLineSummaries(lines),
            _ => DefaultReturn(method.ReturnType)
        });

        var stockSummary = Proxy<IProductStockSummaryService>((method, args) =>
        {
            if (method.Name == nameof(IProductStockSummaryService.GetProductStockSummaryByCategory))
            {
                string category = (string)args![0]!;
                stockSummaryReads.TryGetValue(category, out int reads);
                stockSummaryReads[category] = ++reads;
                return Task.FromResult(reads == 1
                    ? null!
                    : new ProductStockSummary
                    {
                        GKey = 50 + stockSummaryReads.Count,
                        Category = category
                    });
            }

            return DefaultReturn(method.ReturnType);
        });

        var productTransaction = Proxy<IProductTransactionService>((method, args) => method.Name switch
        {
            nameof(IProductTransactionService.GetByCategory) =>
                Task.FromResult<ProductTransaction?>(null),
            nameof(IProductTransactionService.CreateProductTransaction) =>
                CreateProductTransaction((ProductTransaction)args![0]!),
            _ => DefaultReturn(method.ReturnType)
        });

        var transactionSummary = Proxy<IProductTransactionSummaryService>((method, _) => method.Name switch
        {
            nameof(IProductTransactionSummaryService.GetAll) =>
                Task.FromResult<IEnumerable<ProductTransactionSummary>>(
                    Enumerable.Range(0, dailySummaryCount)
                        .Select(_ => new ProductTransactionSummary())
                        .ToList()),
            nameof(IProductTransactionSummaryService.GetLastProductTranSumryByCategory) =>
                Task.FromResult<ProductTransactionSummary?>(null),
            _ => DefaultReturn(method.ReturnType)
        });

        var productStock = Proxy<IProductStockService>((method, _) =>
        {
            if (method.Name == nameof(IProductStockService.CreateProductStock))
            {
                productStockCreates++;
                return Task.CompletedTask;
            }
            return DefaultReturn(method.ReturnType);
        });

        var categories = Proxy<IProductCategoryService>((method, _) => method.Name switch
        {
            nameof(IProductCategoryService.GetProductCategoryList) =>
                Task.FromResult<IEnumerable<ProductCategory>>([]),
            _ => DefaultReturn(method.ReturnType)
        });

        var references = Proxy<IMtblReferencesService>((method, _) => method.Name switch
        {
            nameof(IMtblReferencesService.GetReferenceList) =>
                Task.FromResult<IEnumerable<MtblReference>>([]),
            _ => DefaultReturn(method.ReturnType)
        });

        var messages = new MessageBoxServiceFake();

        Task<ProductTransaction> CreateProductTransaction(ProductTransaction transaction)
        {
            productTransactionCreates++;
            productTransactionSourceKeys.Add(transaction.RefGkey);
            return Task.FromResult(transaction);
        }

        var viewModel = new GRNViewModel(
            grn,
            Proxy<IProductService>(),
            stockSummary,
            productTransaction,
            transactionSummary,
            null!,
            categories,
            messages,
            productStock,
            references);

        return new Fixture(
            viewModel,
            grnState,
            messages,
            () => productStockCreates,
            () => productTransactionCreates,
            productTransactionSourceKeys);
    }

    private sealed record Fixture(
        GRNViewModel ViewModel,
        GrnServiceState Grn,
        MessageBoxServiceFake Messages,
        Func<int> ProductStockCreateCount,
        Func<int> ProductTransactionCreateCount,
        IReadOnlyList<int?> ProductTransactionSourceKeys)
    {
        public int ProductStockCreateCalls => ProductStockCreateCount();
        public int ProductTransactionCreateCalls => ProductTransactionCreateCount();
    }

    private sealed class GrnServiceState
    {
        public int CreateHeaderCalls { get; private set; }
        public int CreateLineSummaryCalls { get; private set; }
        public int SavedLineCount { get; private set; }

        public Task<GrnHeader> CreateHeader(GrnHeader header)
        {
            CreateHeaderCalls++;
            return Task.FromResult(new GrnHeader
            {
                GKey = 1,
                GrnNbr = "GRN-TEST"
            });
        }

        public Task CreateLineSummaries(IEnumerable<GrnLineSummary> lines)
        {
            CreateLineSummaryCalls++;
            foreach (var line in lines)
            {
                SavedLineCount++;
                line.GKey = 10 + SavedLineCount;
            }
            return Task.CompletedTask;
        }
    }

    private sealed class MessageBoxServiceFake : IMessageBoxService
    {
        public string? LastMessage { get; private set; }

        public MessageResult Show(
            string messageBoxText,
            string caption,
            MessageButton button,
            MessageIcon icon,
            MessageResult defaultResult)
        {
            LastMessage = messageBoxText;
            return MessageResult.OK;
        }
    }

    private static T Proxy<T>(
        Func<MethodInfo, object?[]?, object?>? handler = null)
        where T : class
    {
        T proxy = DispatchProxy.Create<T, ServiceProxy<T>>();
        ((ServiceProxy<T>)(object)proxy).Handler = handler;
        return proxy;
    }

    private class ServiceProxy<T> : DispatchProxy where T : class
    {
        public Func<MethodInfo, object?[]?, object?>? Handler { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            Handler?.Invoke(targetMethod!, args) ?? DefaultReturn(targetMethod!.ReturnType);
    }

    private static object? DefaultReturn(Type returnType)
    {
        if (returnType == typeof(Task))
            return Task.CompletedTask;

        if (returnType.IsGenericType &&
            returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            Type resultType = returnType.GetGenericArguments()[0];
            object? result = resultType.IsValueType
                ? Activator.CreateInstance(resultType)
                : null;
            return typeof(Task)
                .GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(resultType)
                .Invoke(null, [result]);
        }

        return returnType.IsValueType ? Activator.CreateInstance(returnType) : null;
    }
}
