using System.Linq.Expressions;
using DataAccess.Controllers;
using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace InvEntry.Test;

[TestFixture]
public sealed class ProductStockSummaryPersistenceTests
{
    [Test]
    public async Task Post_AddsAndCommitsBeforeReturningSuccess()
    {
        var repository = new FakeRepository<ProductStockSummary>();
        var unitOfWork = new FakeUnitOfWork();
        var controller = CreateController(repository, unitOfWork);
        var value = new ProductStockSummary { ProductGkey = 6, Category = "MALA" };

        var result = await controller.Post(value);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.TypeOf<OkObjectResult>());
            Assert.That(repository.Items, Is.EqualTo(new[] { value }));
            Assert.That(unitOfWork.SaveCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Put_UpdatesAndCommitsBeforeReturningSuccess()
    {
        var repository = new FakeRepository<ProductStockSummary>();
        var unitOfWork = new FakeUnitOfWork();
        var controller = CreateController(repository, unitOfWork);
        var value = new ProductStockSummary { Gkey = 7, ProductGkey = 11, Category = "SILVER" };

        var result = await controller.Put(11, value);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.TypeOf<OkObjectResult>());
            Assert.That(repository.Updated, Is.SameAs(value));
            Assert.That(unitOfWork.SaveCount, Is.EqualTo(1));
        });
    }

    [TestCase("POST")]
    [TestCase("PUT")]
    public void CommitFailure_DoesNotReturnHttp200(string operation)
    {
        var repository = new FakeRepository<ProductStockSummary>();
        var controller = CreateController(repository, new FakeUnitOfWork(fail: true));
        var value = new ProductStockSummary { Gkey = 7, ProductGkey = 11, Category = "SILVER" };

        Assert.That(async () =>
        {
            if (operation == "POST")
                await controller.Post(value);
            else
                await controller.Put(11, value);
        }, Throws.TypeOf<InvalidOperationException>());
    }

    private static ProductStockSummaryController CreateController(
        IRepositoryBase<ProductStockSummary> repository,
        IUnitOfWork unitOfWork) =>
        new(repository, unitOfWork, NullLogger<ProductStockSummaryController>.Instance);

    private sealed class FakeUnitOfWork(bool fail = false) : IUnitOfWork
    {
        public int SaveCount { get; private set; }
        public int SaveChanges() => throw new NotSupportedException();
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return fail
                ? Task.FromException<int>(new InvalidOperationException("Simulated database failure."))
                : Task.FromResult(1);
        }
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public void ClearChanges() { }
    }

    private sealed class FakeRepository<T> : IRepositoryBase<T> where T : class
    {
        public List<T> Items { get; } = [];
        public T? Updated { get; private set; }
        public void Add(T value) => Items.Add(value);
        public void AddRange(IEnumerable<T> values) => Items.AddRange(values);
        public void Update(T value) => Updated = value;
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
