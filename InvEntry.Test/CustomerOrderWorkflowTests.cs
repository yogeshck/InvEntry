using System.Data.Common;
using System.Linq.Expressions;
using DataAccess.Controllers;
using DataAccess.Models;
using DataAccess.Repository;
using DataAccess.Workflows;
using InvEntry.Contracts.CustomerOrders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage;

namespace InvEntry.Test;

[TestFixture]
public sealed class CustomerOrderWorkflowTests
{
    [Test]
    public async Task SaveAsync_NewOrder_SavesAggregateIdentitiesTenantAndNewReceipt()
    {
        var orders = new FakeRepository<CustomerOrder>();
        var lines = new FakeRepository<CustomerOrderLine>();
        var oldMetal = new FakeRepository<OldMetalTransaction>();
        var vouchers = new FakeRepository<Voucher>();
        var voucherTypes = new FakeRepository<VoucherType>(
            new() { DocumentType = "Customer Order", DocNbrPrefix = "CO-", DocNbrLength = 4, LastUsedNumber = 7 },
            new() { DocumentType = "Advance Receipt", DocNbrPrefix = "AR-", DocNbrLength = 4, LastUsedNumber = 3 });
        var unit = new FakeUnitOfWork(() => orders.Items.Single().Gkey = 501);
        var workflow = CreateWorkflow(orders, lines, oldMetal, vouchers, voucherTypes, unit);

        var result = await workflow.SaveAsync(new SaveCustomerOrderRequest
        {
            Header = new() { TenantGkey = 12, CustGkey = 22 },
            Lines =
            [
                new() { ProductId = "P1", TenantGkey = 99 },
                new() { ProductId = "P2" }
            ],
            Receipts =
            [
                new() { VoucherType = "Advance Receipt", Mode = "Cash", TransAmount = 100m }
            ]
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.OrderNbr, Is.EqualTo("CO-0008"));
            Assert.That(result.Gkey, Is.EqualTo(501));
            Assert.That(result.IsNew, Is.True);
            Assert.That(lines.Items.Select(x => x.OrderGkey), Is.All.EqualTo(501));
            Assert.That(lines.Items.Select(x => x.OrderNbr), Is.All.EqualTo("CO-0008"));
            Assert.That(lines.Items.Select(x => x.OrderLineNbr), Is.EqualTo(new int?[] { 1, 2 }));
            Assert.That(lines.Items.Select(x => x.TenantGkey), Is.All.EqualTo(12));
            Assert.That(vouchers.Items, Has.Count.EqualTo(1));
            Assert.That(vouchers.Items[0].RefDocGkey, Is.EqualTo(501));
            Assert.That(unit.Transaction.Committed, Is.True);
        });
    }

    [Test]
    public async Task SaveAsync_ExistingOrder_UpdatesHeaderAndLineAddsLineSyncsOldMetalAndRetainsOmittedReceipt()
    {
        var order = new CustomerOrder
        {
            Gkey = 42, OrderNbr = "CO-0042", Remark = "before", TenantGkey = 4, CustGkey = 10
        };
        var existingLine = new CustomerOrderLine
        {
            Gkey = 101, OrderGkey = 42, OrderNbr = "CO-0042", ProductId = "OLD", TenantGkey = 4
        };
        var existingMetal = new OldMetalTransaction
        {
            Gkey = 201, DocRefGkey = 42, DocRefNbr = "CO-0042", DocRefType = "Customer Order", Remarks = "before"
        };
        var persistedReceipt = new Voucher
        {
            Gkey = 301, RefDocGkey = 42, RefDocNbr = "CO-0042", TransAmount = 250m
        };
        var orders = new FakeRepository<CustomerOrder>(order);
        var lines = new FakeRepository<CustomerOrderLine>(existingLine);
        var oldMetal = new FakeRepository<OldMetalTransaction>(existingMetal);
        var vouchers = new FakeRepository<Voucher>(persistedReceipt);
        var unit = new FakeUnitOfWork();
        var workflow = CreateWorkflow(
            orders, lines, oldMetal, vouchers, new FakeRepository<VoucherType>(), unit);

        var result = await workflow.SaveAsync(new SaveCustomerOrderRequest
        {
            Header = new() { Gkey = 42, OrderNbr = "CO-0042", Remark = "after", TenantGkey = 9, CustGkey = 10 },
            Lines =
            [
                new() { Gkey = 101, ProductId = "UPDATED", TenantGkey = 88 },
                new() { ProductId = "ADDED", TenantGkey = 77 }
            ],
            OldMetalTransactions =
            [
                new() { Gkey = 201, Remarks = "after" }
            ],
            Receipts = []
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.IsNew, Is.False);
            Assert.That(order.Remark, Is.EqualTo("after"));
            Assert.That(existingLine.ProductId, Is.EqualTo("UPDATED"));
            Assert.That(lines.Items, Has.Count.EqualTo(2));
            Assert.That(lines.Items[1].ProductId, Is.EqualTo("ADDED"));
            Assert.That(lines.Items.Select(x => x.OrderGkey), Is.All.EqualTo(42));
            Assert.That(lines.Items.Select(x => x.OrderNbr), Is.All.EqualTo("CO-0042"));
            Assert.That(lines.Items.Select(x => x.TenantGkey), Is.All.EqualTo(9));
            Assert.That(existingMetal.Remarks, Is.EqualTo("after"));
            Assert.That(vouchers.Items, Has.Count.EqualTo(1));
            Assert.That(vouchers.Items[0], Is.SameAs(persistedReceipt));
            Assert.That(unit.Transaction.Committed, Is.True);
        });
    }

    [Test]
    public void SaveAsync_WhenFinalSaveFails_RollsBack()
    {
        var orders = new FakeRepository<CustomerOrder>();
        var unit = new FakeUnitOfWork(() => orders.Items.Single().Gkey = 17, failOnSave: 2);
        var workflow = CreateWorkflow(
            orders,
            new FakeRepository<CustomerOrderLine>(),
            new FakeRepository<OldMetalTransaction>(),
            new FakeRepository<Voucher>(),
            new FakeRepository<VoucherType>(new VoucherType
            {
                DocumentType = "Customer Order", DocNbrPrefix = "CO-", DocNbrLength = 2, LastUsedNumber = 0
            }),
            unit);

        Assert.ThrowsAsync<InvalidOperationException>(() => workflow.SaveAsync(new SaveCustomerOrderRequest
        {
            Header = new(),
            Lines = [new() { ProductId = "P1" }]
        }));

        Assert.Multiple(() =>
        {
            Assert.That(unit.Transaction.RolledBack, Is.True);
            Assert.That(unit.Transaction.Committed, Is.False);
        });
    }

    [Test]
    public async Task UpdateEndpoint_DelegatesMatchingAggregateAndRejectsMismatchedOrderNumber()
    {
        var workflow = new RecordingWorkflow();
        var controller = new CustomerOrderController(new FakeRepository<CustomerOrder>(), workflow);
        var request = new SaveCustomerOrderRequest
        {
            Header = new() { Gkey = 42, OrderNbr = "CO-0042" },
            Lines = [new()]
        };

        var ok = await controller.Update("CO-0042", request, CancellationToken.None);
        var mismatch = await controller.Update("CO-9999", request, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(ok.Result, Is.TypeOf<OkObjectResult>());
            Assert.That(workflow.LastRequest, Is.SameAs(request));
            Assert.That(mismatch.Result, Is.TypeOf<BadRequestObjectResult>());
        });
    }

    private static CustomerOrderWorkflow CreateWorkflow(
        FakeRepository<CustomerOrder> orders,
        FakeRepository<CustomerOrderLine> lines,
        FakeRepository<OldMetalTransaction> oldMetal,
        FakeRepository<Voucher> vouchers,
        FakeRepository<VoucherType> voucherTypes,
        FakeUnitOfWork unit) =>
        new(orders, lines, oldMetal, vouchers, voucherTypes, unit);

    private sealed class RecordingWorkflow : ICustomerOrderWorkflow
    {
        public SaveCustomerOrderRequest? LastRequest { get; private set; }
        public Task<SaveCustomerOrderResponse> SaveAsync(
            SaveCustomerOrderRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(new SaveCustomerOrderResponse
            {
                Gkey = request.Header.Gkey,
                OrderNbr = request.Header.OrderNbr ?? string.Empty,
                IsNew = false
            });
        }
    }

    private sealed class FakeUnitOfWork(Action? firstSave = null, int? failOnSave = null) : IUnitOfWork
    {
        public FakeTransaction Transaction { get; } = new();
        private int SaveCount { get; set; }
        public int SaveChanges() => throw new NotSupportedException();
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            if (SaveCount == 1) firstSave?.Invoke();
            if (SaveCount == failOnSave) throw new InvalidOperationException("Simulated persistence failure.");
            return Task.FromResult(1);
        }
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IDbContextTransaction>(Transaction);
        public void ClearChanges() { }
    }

    private sealed class FakeTransaction : IDbContextTransaction
    {
        public Guid TransactionId { get; } = Guid.NewGuid();
        public bool SupportsSavepoints => false;
        public bool Committed { get; private set; }
        public bool RolledBack { get; private set; }
        public void Commit() => Committed = true;
        public Task CommitAsync(CancellationToken cancellationToken = default) { Committed = true; return Task.CompletedTask; }
        public void Rollback() => RolledBack = true;
        public Task RollbackAsync(CancellationToken cancellationToken = default) { RolledBack = true; return Task.CompletedTask; }
        public void CreateSavepoint(string name) => throw new NotSupportedException();
        public Task CreateSavepointAsync(string name, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void RollbackToSavepoint(string name) => throw new NotSupportedException();
        public Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void ReleaseSavepoint(string name) => throw new NotSupportedException();
        public Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public DbTransaction GetDbTransaction() => throw new NotSupportedException();
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FakeRepository<T>(params T[] initial) : IRepositoryBase<T> where T : class
    {
        public List<T> Items { get; } = [.. initial];
        public void Add(T objModel) => Items.Add(objModel);
        public void AddRange(IEnumerable<T> objModel) => Items.AddRange(objModel);
        public T? Get(Expression<Func<T, bool>> predicate) => Items.AsQueryable().SingleOrDefault(predicate);
        public IEnumerable<T> GetList(Expression<Func<T, bool>> predicate) => Items.AsQueryable().Where(predicate);
        public IEnumerable<T> GetAll() => Items;
        public int Count() => Items.Count;
        public void Update(T objModel) { }
        public void BulkUpdate(IEnumerable<T> objModels) { }
        public void Remove(T objModel) => Items.Remove(objModel);
        public T? GetId(int id) => throw new NotSupportedException();
        public Task<T?> GetIdAsync(int id) => throw new NotSupportedException();
        public Task<T?> GetAsync(Expression<Func<T, bool>> predicate) => Task.FromResult(Get(predicate));
        public Task<IEnumerable<T>> GetListAsync(Expression<Func<T, bool>> predicate) => Task.FromResult(GetList(predicate));
        public Task<IEnumerable<T>> GetAllAsync() => Task.FromResult(GetAll());
        public Task<int> CountAsync() => Task.FromResult(Count());
        public void Dispose() { }
    }
}
