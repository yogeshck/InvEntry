using System.Data.Common;
using System.Linq.Expressions;
using DataAccess.Inventory.ProductStock;
using DataAccess.Models;
using DataAccess.Repository;
using DataAccess.Workflows;
using InvEntry.Contracts.Invoices;
using InvEntry.Models.Extensions;
using InvEntry.ViewModels.Invoices;
using Microsoft.EntityFrameworkCore.Storage;

namespace InvEntry.Test;

[TestFixture]
public sealed class InvoiceLineValidationTests
{
    [TestCase(null)]
    [TestCase(0)]
    [TestCase(-1)]
    public void WeightBasedLine_RejectsMissingOrNonPositiveGross(decimal? gross)
    {
        var result = Validate(gross, 0M, gross);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Message, Does.Contain("Gross Weight"));
    }

    [Test]
    public void WeightBasedLine_AcceptsConsistentWeights()
    {
        Assert.That(Validate(10M, 2M, 8M).IsValid, Is.True);
    }

    [Test]
    public void GrossCell_AcceptsConvertibleCurrentEditorValue()
    {
        Assert.That(
            InvoiceLineWeightValidator.ValidateGrossCell("5", 1M).IsValid,
            Is.True);
    }

    [TestCase(null)]
    [TestCase(0)]
    [TestCase(-1)]
    public void GrossCell_RejectsMissingOrNonPositiveValue(object? value)
    {
        Assert.That(
            InvoiceLineWeightValidator.ValidateGrossCell(value, 0M).IsValid,
            Is.False);
    }

    [Test]
    public void GrossCell_RejectsValueBelowCurrentStoneWeight()
    {
        var result = InvoiceLineWeightValidator.ValidateGrossCell(5M, 6M);

        Assert.That(result.Message, Does.Contain("Stone Weight cannot exceed"));
    }

    [Test]
    public void StoneCell_RejectsNegativeAndGreaterThanGrossValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                InvoiceLineWeightValidator.ValidateStoneCell(-1M, 5M).IsValid,
                Is.False);
            Assert.That(
                InvoiceLineWeightValidator.ValidateStoneCell(6M, 5M).IsValid,
                Is.False);
            Assert.That(
                InvoiceLineWeightValidator.ValidateStoneCell(1M, 5M).IsValid,
                Is.True);
        });
    }

    [Test]
    public void WeightBasedLine_RejectsStoneGreaterThanGross()
    {
        var result = Validate(10M, 11M, -1M);

        Assert.That(result.Message, Does.Contain("Stone Weight cannot exceed"));
    }

    [Test]
    public void NonWeightProduct_DoesNotRequireJewelleryWeights()
    {
        Assert.That(Validate(null, null, null, metal: null).IsValid, Is.True);
    }

    [Test]
    public void Validation_IdentifiesInvalidSecondLine()
    {
        var results = new[]
        {
            InvoiceLineWeightValidator.Validate(1, "Ring", "R1", "GOLD", 10M, 1M, 9M),
            InvoiceLineWeightValidator.Validate(2, "Chain", "C1", "GOLD", 0M, 0M, 0M)
        };

        var invalid = results.Single(x => !x.IsValid);
        Assert.That(invalid.Message, Does.StartWith("Row 2 (Chain)"));
    }

    [Test]
    public void DraftSave_RejectsInvalidLineBeforeStartingTransaction()
    {
        var fixture = WorkflowFixture.Create(grossWeight: 0M);
        var request = new SaveInvoiceRequest
        {
            Header = ValidHeader(),
            Lines = [LineSaveModel(0M)]
        };

        Assert.ThrowsAsync<InvoiceLineBusinessValidationException>(async () =>
            await fixture.Workflow.SaveDraftAsync(request));
        Assert.That(fixture.UnitOfWork.BeginCount, Is.Zero);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void CustomerValidation_RejectsMissingMobile(string? mobile)
    {
        var result = InvoiceCustomerAssociationValidator.Validate(
            mobile, 5, "9876543210", "Customer");

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void CustomerValidation_RejectsMobileWithoutCustomerReference()
    {
        var result = InvoiceCustomerAssociationValidator.Validate(
            "9876543210", null, "9876543210", "Customer");

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.FailureCode, Is.EqualTo("CUSTOMER_ID_MISSING"));
    }

    [Test]
    public void CustomerValidation_ReportsAllSpecificFailureCodes()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                InvoiceCustomerAssociationValidator.Validate("123", 5, "123", "Customer").FailureCode,
                Is.EqualTo("MOBILE_INCOMPLETE"));
            Assert.That(
                InvoiceCustomerAssociationValidator.Validate("9876543210", 5, "9876543210", "Customer", false).FailureCode,
                Is.EqualTo("CUSTOMER_NOT_AVAILABLE"));
            Assert.That(
                InvoiceCustomerAssociationValidator.Validate("9876543210", 5, "9876543210", null).FailureCode,
                Is.EqualTo("CUSTOMER_NAME_MISSING"));
        });
    }

    [Test]
    public void CustomerValidation_ReportsMobileMismatchSpecifically()
    {
        var result = InvoiceCustomerAssociationValidator.Validate(
            "9841012345", 5, "9841099999", "Customer");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.FailureCode, Is.EqualTo("MOBILE_MISMATCH"));
            Assert.That(result.Message, Does.Contain("mobile number has changed"));
        });
    }

    [Test]
    public void CustomerValidation_AcceptsResolvedExistingCustomer()
    {
        var result = InvoiceCustomerAssociationValidator.Validate(
            " 9876543210 ", 5, "9876543210", "Customer");

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void DraftSave_RejectsDeletedCustomerBeforeTransaction()
    {
        var fixture = WorkflowFixture.Create(10M, customerDeleted: true);

        Assert.ThrowsAsync<InvoiceCustomerBusinessValidationException>(async () =>
            await fixture.Workflow.SaveDraftAsync(new SaveInvoiceRequest
            {
                Header = ValidHeader(),
                Lines = [LineSaveModel(10M)]
            }));

        Assert.That(fixture.UnitOfWork.BeginCount, Is.Zero);
    }

    [Test]
    public async Task Finalisation_RejectsUnavailableCustomerWithoutPosting()
    {
        var fixture = WorkflowFixture.Create(10M, includeCustomer: false);

        Assert.ThrowsAsync<InvoiceCustomerBusinessValidationException>(async () =>
            await fixture.Workflow.FinaliseAsync(new FinaliseInvoiceRequest
            {
                InvoiceGkey = 10
            }));

        Assert.Multiple(() =>
        {
            Assert.That(fixture.UnitOfWork.SaveCount, Is.Zero);
            Assert.That(fixture.UnitOfWork.Transaction.RollbackCount, Is.EqualTo(1));
            Assert.That(fixture.StockMovement.PostCount, Is.Zero);
            Assert.That(fixture.Vouchers.Items, Is.Empty);
            Assert.That(fixture.Receipts.Items, Is.Empty);
            Assert.That(fixture.Invoice.InvNbr, Is.Null);
            Assert.That(fixture.Invoice.Status, Is.EqualTo(InvoiceStatus.Draft));
        });
    }

    [Test]
    public async Task Finalisation_RejectsPersistedInvalidLineWithoutPosting()
    {
        var fixture = WorkflowFixture.Create(grossWeight: 0M);

        Assert.ThrowsAsync<InvoiceLineBusinessValidationException>(async () =>
            await fixture.Workflow.FinaliseAsync(new FinaliseInvoiceRequest
            {
                InvoiceGkey = 10
            }));

        Assert.Multiple(() =>
        {
            Assert.That(fixture.UnitOfWork.SaveCount, Is.Zero);
            Assert.That(fixture.UnitOfWork.Transaction.RollbackCount, Is.EqualTo(1));
            Assert.That(fixture.StockMovement.PostCount, Is.Zero);
            Assert.That(fixture.Vouchers.Items, Is.Empty);
            Assert.That(fixture.Receipts.Items, Is.Empty);
            Assert.That(fixture.Invoice.Status, Is.EqualTo(InvoiceStatus.Draft));
        });
    }

    [Test]
    public void NewInvoiceLine_DefaultsVaValuesToZero()
    {
        var line = new InvEntry.Models.InvoiceLine();

        Assert.Multiple(() =>
        {
            Assert.That(line.VaPercent, Is.Zero);
            Assert.That(line.VaAmount, Is.Zero);
        });
    }

    [Test]
    public void ProductAndStockVaPrecedence_IsStable()
    {
        Assert.Multiple(() =>
        {
            Assert.That(InvoiceLineVaDefaults.ResolvePercent(null, 12M), Is.EqualTo(12M));
            Assert.That(InvoiceLineVaDefaults.ResolvePercent(18M, 12M), Is.EqualTo(18M));
            Assert.That(InvoiceLineVaDefaults.ResolvePercent(null, null), Is.Zero);
        });
    }

    [Test]
    public void ExistingPersistedVaValues_AreRetained()
    {
        Assert.Multiple(() =>
        {
            Assert.That(InvoiceLineVaDefaults.PreserveOrZero(7.5M), Is.EqualTo(7.5M));
            Assert.That(InvoiceLineVaDefaults.PreserveOrZero(1250M), Is.EqualTo(1250M));
            Assert.That(InvoiceLineVaDefaults.PreserveOrZero(null), Is.Zero);
        });
    }

    [Test]
    public void ProductSwitch_DoesNotRetainPreviousVa()
    {
        var first = InvoiceLineVaDefaults.ResolvePercent(15M, 10M);
        var second = InvoiceLineVaDefaults.ResolvePercent(null, null);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(15M));
            Assert.That(second, Is.Zero);
        });
    }

    private static InvoiceLineWeightValidationResult Validate(
        decimal? gross,
        decimal? stone,
        decimal? net,
        string? metal = "GOLD") =>
        InvoiceLineWeightValidator.Validate(
            1, "Test Product", "P1", metal, gross, stone, net);

    private static InvoiceLineSaveModel LineSaveModel(decimal? grossWeight) => new()
    {
        ProductName = "Test Ring",
        ProductId = "R1",
        Metal = "GOLD",
        ProdGrossWeight = grossWeight,
        ProdStoneWeight = 0M,
        ProdNetWeight = grossWeight
    };

    private static InvoiceHeaderSaveModel ValidHeader() => new()
    {
        CustGkey = 5,
        CustMobile = "9876543210"
    };

    private sealed class WorkflowFixture
    {
        public required InvoiceWorkflow Workflow { get; init; }
        public required InvoiceHeader Invoice { get; init; }
        public required FakeRepository<InvoiceArReceipt> Receipts { get; init; }
        public required FakeRepository<Voucher> Vouchers { get; init; }
        public required FakeUnitOfWork UnitOfWork { get; init; }
        public required FakeStockMovementService StockMovement { get; init; }

        public static WorkflowFixture Create(
            decimal? grossWeight,
            bool includeCustomer = true,
            bool customerDeleted = false)
        {
            var invoice = new InvoiceHeader
            {
                Gkey = 10,
                Status = InvoiceStatus.Draft,
                AmountPayable = 100M,
                CustGkey = 5,
                CustMobile = "9876543210"
            };
            var line = new DataAccess.Models.InvoiceLine
            {
                Gkey = 20,
                InvoiceHdrGkey = 10,
                InvLineNbr = 1,
                ProductName = "Test Ring",
                ProductId = "R1",
                Metal = "GOLD",
                ProdGrossWeight = grossWeight,
                ProdStoneWeight = 0M,
                ProdNetWeight = grossWeight
            };
            var headers = new FakeRepository<InvoiceHeader>(invoice);
            var lines = new FakeRepository<DataAccess.Models.InvoiceLine>(line);
            var receipts = new FakeRepository<InvoiceArReceipt>();
            var oldMetal = new FakeRepository<OldMetalTransaction>();
            var voucherTypes = new FakeRepository<VoucherType>();
            var vouchers = new FakeRepository<Voucher>();
            var customers = includeCustomer
                ? new FakeRepository<OrgCustomer>(new OrgCustomer
                {
                    Gkey = 5,
                    MobileNbr = "9876543210",
                    CustomerName = "Customer",
                    DeleteFlag = customerDeleted
                })
                : new FakeRepository<OrgCustomer>();
            var stock = new FakeStockMovementService();
            var unitOfWork = new FakeUnitOfWork();
            var workflow = new InvoiceWorkflow(
                headers, lines, receipts, oldMetal, voucherTypes, vouchers, customers,
                stock, null!, unitOfWork);

            return new()
            {
                Workflow = workflow,
                Invoice = invoice,
                Receipts = receipts,
                Vouchers = vouchers,
                UnitOfWork = unitOfWork,
                StockMovement = stock
            };
        }
    }

    private sealed class FakeStockMovementService : IStockMovementService
    {
        public int PostCount { get; private set; }
        public void PostMovement(StockMovementRequest request) => PostCount++;
        public void PostMovements(IEnumerable<StockMovementRequest> requests) =>
            PostCount += requests.Count();
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int BeginCount { get; private set; }
        public int SaveCount { get; private set; }
        public FakeTransaction Transaction { get; } = new();
        public int SaveChanges() { SaveCount++; return 1; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        { SaveCount++; return Task.FromResult(1); }
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        { BeginCount++; return Task.FromResult<IDbContextTransaction>(Transaction); }
        public void ClearChanges() { }
    }

    private sealed class FakeTransaction : IDbContextTransaction
    {
        public Guid TransactionId { get; } = Guid.NewGuid();
        public int RollbackCount { get; private set; }
        public bool SupportsSavepoints => false;
        public void Commit() { }
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Rollback() => RollbackCount++;
        public Task RollbackAsync(CancellationToken cancellationToken = default)
        { RollbackCount++; return Task.CompletedTask; }
        public DbTransaction GetDbTransaction() => throw new NotSupportedException();
        public void CreateSavepoint(string name) => throw new NotSupportedException();
        public Task CreateSavepointAsync(string name, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void RollbackToSavepoint(string name) => throw new NotSupportedException();
        public Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void ReleaseSavepoint(string name) => throw new NotSupportedException();
        public Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FakeRepository<T>(params T[] initial) : IRepositoryBase<T> where T : class
    {
        public List<T> Items { get; } = [.. initial];
        public void Add(T value) => Items.Add(value);
        public void AddRange(IEnumerable<T> values) => Items.AddRange(values);
        public void Update(T value) { }
        public void BulkUpdate(IEnumerable<T> values) { }
        public void Remove(T value) => Items.Remove(value);
        public T? Get(Expression<Func<T, bool>> predicate) => Items.AsQueryable().FirstOrDefault(predicate);
        public IEnumerable<T> GetList(Expression<Func<T, bool>> predicate) => Items.AsQueryable().Where(predicate).ToList();
        public IEnumerable<T> GetAll() => Items;
        public int Count() => Items.Count;
        public T? GetId(int id) => throw new NotSupportedException();
        public Task<T?> GetIdAsync(int id) => throw new NotSupportedException();
        public Task<T?> GetAsync(Expression<Func<T, bool>> predicate) => Task.FromResult(Get(predicate));
        public Task<IEnumerable<T>> GetListAsync(Expression<Func<T, bool>> predicate) => Task.FromResult(GetList(predicate));
        public Task<IEnumerable<T>> GetAllAsync() => Task.FromResult(GetAll());
        public Task<int> CountAsync() => Task.FromResult(Count());
        public void Dispose() { }
    }
}
