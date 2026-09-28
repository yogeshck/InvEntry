using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using DataAccess.Controllers;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace InvEntry.Test;

[TestFixture]
public sealed class ProductTransactionContractTests
{
    [Test]
    public async Task CategoryEndpoint_ExistingCategory_ReturnsLatestRecord()
    {
        var repository = new FakeRepository<DataAccess.Models.ProductTransaction>(
            new() { Gkey = 1, ProductCategory = "MALA" },
            new() { Gkey = 2, ProductCategory = "MALA" });
        var controller = CreateController(repository, new FakeUnitOfWork());

        var result = await controller.GetByProductCategory("MALA");

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(((DataAccess.Models.ProductTransaction)ok!.Value!).Gkey, Is.EqualTo(2));
    }

    [TestCase("MALA")]
    [TestCase("SILVER")]
    [TestCase("MISSING")]
    public async Task CategoryEndpoint_MissingCategory_Returns404(string category)
    {
        var controller = CreateController(
            new FakeRepository<DataAccess.Models.ProductTransaction>(),
            new FakeUnitOfWork());

        var result = await controller.GetByProductCategory(category);

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task Post_RepeatedGrnSource_ReturnsExistingWithoutDuplicate()
    {
        var existing = Receipt("GRN-001", "MALA", 10);
        existing.Gkey = 7;
        var repository = new FakeRepository<DataAccess.Models.ProductTransaction>(existing);
        var unitOfWork = new FakeUnitOfWork();
        var controller = CreateController(repository, unitOfWork);

        var result = await controller.Post(Receipt("GRN-001", "MALA", 10));

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.TypeOf<OkObjectResult>());
            Assert.That(repository.Items, Has.Count.EqualTo(1));
            Assert.That(unitOfWork.SaveCount, Is.Zero);
            Assert.That(((DataAccess.Models.ProductTransaction)((OkObjectResult)result).Value!).Gkey, Is.EqualTo(7));
        });
    }

    [Test]
    public async Task Post_NewGrnSource_AddsAndCommits()
    {
        var repository = new FakeRepository<DataAccess.Models.ProductTransaction>();
        var unitOfWork = new FakeUnitOfWork();
        var controller = CreateController(repository, unitOfWork);

        await controller.Post(Receipt("GRN-002", "SILVER", 20));

        Assert.Multiple(() =>
        {
            Assert.That(repository.Items, Has.Count.EqualTo(1));
            Assert.That(unitOfWork.SaveCount, Is.EqualTo(1));
        });
    }

    private static ProductTransactionController CreateController(
        IRepositoryBase<DataAccess.Models.ProductTransaction> repository,
        IUnitOfWork unitOfWork) =>
        new(repository, unitOfWork, NullLogger<ProductTransactionController>.Instance);

    private static DataAccess.Models.ProductTransaction Receipt(
        string documentNumber,
        string category,
        int productGkey) => new()
        {
            DocumentNbr = documentNumber,
            DocumentType = "GRN",
            ProductCategory = category,
            RefGkey = productGkey,
            TransactionType = "Receipt"
        };

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }
        public int ClearCount { get; private set; }
        public Func<Task<int>>? SaveHandler { get; init; }
        public int SaveChanges() => ++SaveCount;
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return SaveHandler?.Invoke() ?? Task.FromResult(1);
        }
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public void ClearChanges() => ClearCount++;
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
