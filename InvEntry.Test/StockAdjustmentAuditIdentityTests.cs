using DataAccess.Inventory.ProductStock;
using DataAccess.Models;
using DataAccess.Repository;
using DataAccess.Services;
using DataAccess.Workflows;
using InvEntry.Contracts.StockAdjustments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace InvEntry.Test;

[TestFixture]
public sealed class StockAdjustmentAuditIdentityTests
{
    [Test]
    public async Task CreateAsync_UsesInjectedIdentityForCreatedByAndModifiedBy()
    {
        await using var context = new TestMijmsContext(Guid.NewGuid().ToString());
        context.Products.Add(new Product
        {
            Gkey = 101,
            Category = "RING",
            Metal = "GOLD",
            Purity = "22K",
            Uom = "GM"
        });
        await context.SaveChangesAsync();

        var workflow = new StockAdjustmentWorkflow(
            context,
            new UnitOfWork(context),
            new StockMovementServiceFake(),
            new VoucherNumberServiceFake(),
            new AuditIdentityProviderFake("  TEST_OPERATOR  "));

        await workflow.CreateAsync(new CreateStockAdjustmentRequest
        {
            AdjustmentDate = DateTime.Today,
            AdjustmentType = StockAdjustmentTypes.Increase,
            ReasonCode = StockAdjustmentReasonCodes.PhysicalExcess,
            Lines =
            [
                new CreateStockAdjustmentLineRequest
                {
                    LineNbr = 1,
                    Direction = StockAdjustmentDirections.In,
                    StockLevel = StockAdjustmentStockLevels.Consolidated,
                    MovementKind = StockAdjustmentMovementKinds.Weight,
                    ProductGkey = 101,
                    ProductCategory = "RING",
                    GrossWeight = 1.250M,
                    StoneWeight = 0.100M,
                    NetWeight = 1.150M
                }
            ]
        });

        var header = await context.StockAdjustmentHeaders.SingleAsync();

        Assert.Multiple(() =>
        {
            Assert.That(header.CreatedBy, Is.EqualTo("TEST_OPERATOR"));
            Assert.That(header.ModifiedBy, Is.EqualTo("TEST_OPERATOR"));
            Assert.That(header.Status, Is.EqualTo(StockAdjustmentStatuses.Posted));
            Assert.That(header.ModifiedOn, Is.EqualTo(header.FinalisedOn));
        });
    }

    private sealed class TestMijmsContext(string databaseName) : MijmsContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseInMemoryDatabase(databaseName)
                .ConfigureWarnings(warnings =>
                    warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning));
        }
    }

    private sealed class StockMovementServiceFake : IStockMovementService
    {
        public void PostMovement(StockMovementRequest request)
        {
        }

        public void PostMovements(IEnumerable<StockMovementRequest> requests)
        {
        }
    }

    private sealed class VoucherNumberServiceFake : IVoucherNumberService
    {
        public Task<string> GetNextNumberAsync(
            string documentType,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult("ADJ-TEST");
        }
    }

    private sealed class AuditIdentityProviderFake(string identity)
        : IAuditIdentityProvider
    {
        public string GetCurrentIdentity()
        {
            return identity;
        }
    }
}
