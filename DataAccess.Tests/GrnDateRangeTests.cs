using System.Linq.Expressions;
using DataAccess.Controllers;
using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;

namespace DataAccess.Tests;

public sealed class GrnDateRangeTests
{
    [Test]
    public void Supplier_query_filters_open_grns_by_inclusive_document_date()
    {
        var repository = new StubRepository<GrnHeader>(
        [
            Header("SUP-1", "Open", new DateTime(2026, 10, 1)),
            Header("SUP-1", "Open", new DateTime(2026, 10, 13, 23, 59, 59)),
            Header("SUP-1", "Open", new DateTime(2026, 9, 30, 23, 59, 59)),
            Header("SUP-1", "Open", new DateTime(2026, 10, 14)),
            Header("SUP-2", "Open", new DateTime(2026, 10, 5)),
            Header("SUP-1", "Closed", new DateTime(2026, 10, 5))
        ]);
        var controller = new GrnController(repository, null!, null!);

        var result = controller.GetBySupplierId(
            "SUP-1",
            new DateTime(2026, 10, 1),
            new DateTime(2026, 10, 13));

        var rows = ((OkObjectResult)result).Value as IEnumerable<GrnHeader>;
        Assert.That(rows, Is.Not.Null);
        Assert.That(rows!.Select(x => x.GrnDate), Is.EquivalentTo(new DateTime?[]
        {
            new(2026, 10, 1),
            new(2026, 10, 13, 23, 59, 59)
        }));
    }

    [Test]
    public void Supplier_query_rejects_reversed_range()
    {
        var controller = new GrnController(
            new StubRepository<GrnHeader>([]), null!, null!);

        var result = controller.GetBySupplierId(
            "SUP-1",
            new DateTime(2026, 10, 13),
            new DateTime(2026, 10, 1));

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
    }

    private static GrnHeader Header(string supplier, string status, DateTime date) =>
        new() { SupplierId = supplier, Status = status, GrnDate = date };

    private sealed class StubRepository<T>(IEnumerable<T> rows) : IRepositoryBase<T>
        where T : class
    {
        private readonly List<T> _rows = rows.ToList();
        public IEnumerable<T> GetList(Expression<Func<T, bool>> predicate) =>
            _rows.Where(predicate.Compile());
        public IEnumerable<T> GetAll() => _rows;
        public Task<IEnumerable<T>> GetListAsync(Expression<Func<T, bool>> predicate) =>
            Task.FromResult(GetList(predicate));
        public Task<IEnumerable<T>> GetAllAsync() => Task.FromResult(GetAll());
        public T? Get(Expression<Func<T, bool>> predicate) => _rows.FirstOrDefault(predicate.Compile());
        public Task<T?> GetAsync(Expression<Func<T, bool>> predicate) => Task.FromResult(Get(predicate));
        public T? GetId(int id) => null;
        public Task<T?> GetIdAsync(int id) => Task.FromResult<T?>(null);
        public int Count() => _rows.Count;
        public Task<int> CountAsync() => Task.FromResult(Count());
        public void Add(T objModel) => throw new NotSupportedException();
        public void AddRange(IEnumerable<T> objModel) => throw new NotSupportedException();
        public void Update(T objModel) => throw new NotSupportedException();
        public void BulkUpdate(IEnumerable<T> objModels) => throw new NotSupportedException();
        public void Remove(T objModel) => throw new NotSupportedException();
        public void Dispose() { }
    }
}
