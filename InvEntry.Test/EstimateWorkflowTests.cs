using System.Linq.Expressions;
using System.Data.Common;
using DataAccess.Models;
using DataAccess.Controllers;
using DataAccess.Repository;
using DataAccess.Workflows;
using InvEntry.Contracts.Estimates;
using Microsoft.EntityFrameworkCore.Storage;

namespace InvEntry.Test;

[TestFixture]
public sealed class EstimateWorkflowTests
{
    [Test]
    public async Task SaveAsync_SavesHeaderAndLinesWithOneNumberAndDeterministicIdentities()
    {
        var headers = new FakeRepository<EstimateHeader>();
        var lines = new FakeRepository<EstimateLine>();
        var voucher = new VoucherType
        {
            DocumentType = "Estimate",
            DocNbrPrefix = "EST",
            DocNbrLength = 5,
            LastUsedNumber = 41
        };
        var vouchers = new FakeRepository<VoucherType>(voucher);
        var unitOfWork = new FakeUnitOfWork(() => headers.Items.Single().Gkey = 7001);
        var workflow = new EstimateWorkflow(headers, lines, vouchers, unitOfWork);

        var response = await workflow.SaveAsync(new SaveEstimateRequest
        {
            Header = new EstimateHeaderSaveModel { TenantGkey = 9, EstTaxableAmount = 125m },
            Lines =
            [
                new EstimateLineSaveModel { ProductId = "P2", EstlTaxableAmount = 75m },
                new EstimateLineSaveModel { ProductId = "P1", EstlTaxableAmount = 50m }
            ]
        });

        Assert.Multiple(() =>
        {
            Assert.That(response.Gkey, Is.EqualTo(7001));
            Assert.That(response.EstNbr, Is.EqualTo("EST00042"));
            Assert.That(headers.Items, Has.Count.EqualTo(1));
            Assert.That(lines.Items, Has.Count.EqualTo(2));
            Assert.That(lines.Items.Select(x => x.EstimateHdrGkey), Is.All.EqualTo(7001));
            Assert.That(lines.Items.Select(x => x.EstimateId), Is.All.EqualTo("EST00042"));
            Assert.That(lines.Items.Select(x => x.EstLineNbr), Is.EqualTo(new int?[] { 1, 2 }));
            Assert.That(lines.Items.Select(x => x.ProductId), Is.EqualTo(new[] { "P2", "P1" }));
            Assert.That(voucher.LastUsedNumber, Is.EqualTo(42));
            Assert.That(unitOfWork.SaveCount, Is.EqualTo(2));
            Assert.That(unitOfWork.Transaction.Committed, Is.True);
            Assert.That(unitOfWork.Transaction.RolledBack, Is.False);
        });
    }

    [Test]
    public void SaveAsync_WhenLineSaveFails_RollsBackAndDoesNotCommit()
    {
        var headers = new FakeRepository<EstimateHeader>();
        var lines = new FakeRepository<EstimateLine>();
        var vouchers = new FakeRepository<VoucherType>(new VoucherType
        {
            DocumentType = "Estimate", DocNbrPrefix = "E", DocNbrLength = 3, LastUsedNumber = 9
        });
        var unitOfWork = new FakeUnitOfWork(() => headers.Items.Single().Gkey = 15, failOnSave: 2);
        var workflow = new EstimateWorkflow(headers, lines, vouchers, unitOfWork);

        Assert.ThrowsAsync<InvalidOperationException>(() => workflow.SaveAsync(new SaveEstimateRequest
        {
            Header = new EstimateHeaderSaveModel(),
            Lines = [new EstimateLineSaveModel()]
        }));

        Assert.Multiple(() =>
        {
            Assert.That(unitOfWork.Transaction.RolledBack, Is.True);
            Assert.That(unitOfWork.Transaction.Committed, Is.False);
        });
    }

    [Test]
    public void ExistingGet_ReturnsEstimateByNumber()
    {
        var expected = new EstimateHeader { EstNbr = "EST00042" };
        var controller = new EstimateController(
            new FakeRepository<EstimateHeader>(expected),
            new FakeRepository<VoucherType>(),
            new FakeWorkflow());

        Assert.That(controller.Get("EST00042"), Is.SameAs(expected));
    }
    private sealed class FakeUnitOfWork(Action firstSave, int? failOnSave = null) : IUnitOfWork
    {
        public FakeTransaction Transaction { get; } = new();
        public int SaveCount { get; private set; }

        public int SaveChanges() => throw new NotSupportedException();
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            if (SaveCount == 1) firstSave();
            if (SaveCount == failOnSave) throw new InvalidOperationException("Simulated line save failure.");
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

    private sealed class FakeWorkflow : IEstimateWorkflow
    {
        public Task<SaveEstimateResponse> SaveAsync(
            SaveEstimateRequest request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
