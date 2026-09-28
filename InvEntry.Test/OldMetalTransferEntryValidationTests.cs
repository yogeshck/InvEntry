using System.Reflection;
using DevExpress.Mvvm;
using InvEntry.Contracts.StockTransfers;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.ViewModels;

namespace InvEntry.Test;

[TestFixture]
public sealed class OldMetalTransferEntryValidationTests
{
    [Test]
    public async Task AddItem_MissingToSite_BlocksBeforeProductLookupAndShowsMessage()
    {
        var fixture = CreateFixture();
        fixture.ViewModel.OldMetalIdUI = "OLD-GOLD";
        fixture.ViewModel.TransferNetWeight = 2M;

        await fixture.ViewModel.FetchProductCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.ViewModel.OmTransUIList, Is.Empty);
            Assert.That(fixture.Products.GetProductCalls, Is.Zero);
            Assert.That(fixture.Stock.GetByProductGkeyCalls, Is.Zero);
            Assert.That(fixture.Messages.LastMessage,
                Is.EqualTo("Please select the To Site before adding transfer items."));
        });
    }

    [Test]
    public async Task AddItem_ValidToSite_UsesExistingLookupAndAddsLine()
    {
        var fixture = CreateFixture();
        fixture.ViewModel.SentTo = "MELTING";
        fixture.ViewModel.OldMetalIdUI = "OLD-GOLD";
        fixture.ViewModel.TransferNetWeight = 2M;

        await fixture.ViewModel.FetchProductCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.ViewModel.OmTransUIList, Has.Count.EqualTo(1));
            Assert.That(fixture.Products.GetProductCalls, Is.GreaterThan(0));
            Assert.That(fixture.Stock.GetByProductGkeyCalls, Is.GreaterThan(0));
            Assert.That(fixture.ViewModel.OmTransUIList[0].TransferWeight, Is.EqualTo(2M));
        });
    }

    [Test]
    public async Task Save_MissingToSite_DoesNotCallPersistence()
    {
        var fixture = CreateFixture();
        fixture.ViewModel.OmTransUIList.Add(CreateLine());

        await InvokeCreateStockTransfer(fixture.ViewModel);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Transfers.CreateCalls, Is.Zero);
            Assert.That(fixture.Messages.LastMessage,
                Is.EqualTo("Please select a valid destination branch."));
        });
    }

    [Test]
    public async Task Save_ValidTransfer_UsesExistingPersistenceWorkflow()
    {
        var fixture = CreateFixture();
        fixture.ViewModel.SentTo = "MELTING";
        fixture.ViewModel.OmTransUIList.Add(CreateLine());

        await fixture.ViewModel.CreateStockTransferCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Transfers.CreateCalls, Is.EqualTo(1));
            Assert.That(fixture.Transfers.LastRequest, Is.Not.Null);
            Assert.That(fixture.Transfers.LastRequest!.ToReferenceGkey, Is.EqualTo(20));
            Assert.That(fixture.ViewModel.TransferGkey, Is.EqualTo(101));
        });
    }

    private static Fixture CreateFixture()
    {
        var transfers = new StockTransferServiceFake();
        var products = new ProductViewServiceFake();
        var stock = new ProductStockSummaryServiceFake();
        var messages = new MessageBoxServiceFake();
        var viewModel = new OldMetalTransferEntryViewModel(
            transfers,
            new CompanyServiceFake(),
            new ReferencesServiceFake(),
            products,
            stock,
            messages,
            null!);

        return new(viewModel, transfers, products, stock, messages);
    }

    private static OldMetalTransferLineItem CreateLine() => new()
    {
        ProductGkey = 10,
        ProductId = "OLD-GOLD",
        TransferWeight = 2M,
        CurrentStock = 10M,
        BalanceAfterTransfer = 8M,
        Uom = "Grams"
    };

    private static async Task InvokeCreateStockTransfer(
        OldMetalTransferEntryViewModel viewModel)
    {
        var method = typeof(OldMetalTransferEntryViewModel).GetMethod(
            "CreateStockTransfer",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null);
        await (Task)method!.Invoke(viewModel, null)!;
    }

    private sealed record Fixture(
        OldMetalTransferEntryViewModel ViewModel,
        StockTransferServiceFake Transfers,
        ProductViewServiceFake Products,
        ProductStockSummaryServiceFake Stock,
        MessageBoxServiceFake Messages);

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

    private sealed class StockTransferServiceFake : IStockTransferService
    {
        public int CreateCalls { get; private set; }
        public CreateStockTransferRequest? LastRequest { get; private set; }

        public Task<StockTransferDetailResponse> CreateAsync(
            CreateStockTransferRequest request)
        {
            CreateCalls++;
            LastRequest = request;
            return Task.FromResult(new StockTransferDetailResponse
            {
                Gkey = 101,
                TransferNbr = "OMT-101"
            });
        }
    }

    private sealed class CompanyServiceFake : IOrgThisCompanyViewService
    {
        public Task<OrgThisCompanyView> GetOrgThisCompany() =>
            Task.FromResult(new OrgThisCompanyView
            {
                CompanyName = "MAIN",
                TenantGkey = 1
            });
    }

    private sealed class ReferencesServiceFake : IMtblReferencesService
    {
        public Task<IEnumerable<MtblReference>> GetReferenceList(string refName) =>
            Task.FromResult<IEnumerable<MtblReference>>(
                refName == "STOCK_TRANSFER"
                    ? [new MtblReference
                    {
                        GKey = 20,
                        RefCode = "MELTING",
                        RefValue = "Melting Site",
                        IsActive = true
                    }]
                    : [new MtblReference
                    {
                        RefCode = "OLD-GOLD",
                        RefValue = "OLD-GOLD",
                        IsActive = true
                    }]);

        public Task<MtblReference> GetReference(string refName, string refCode) =>
            throw new NotSupportedException();
        public Task<MtblReference> GetReferenceByCode(string refName, string refCode) =>
            throw new NotSupportedException();
        public Task<List<string>> GetReferenceByValueList(string refName, string refValue) =>
            throw new NotSupportedException();
        public Task<MtblReference> CreatReference(MtblReference mtblReference) =>
            throw new NotSupportedException();
        public Task UpdateReference(MtblReference mtblReference) =>
            throw new NotSupportedException();
    }

    private sealed class ProductViewServiceFake : IProductViewService
    {
        public int GetProductCalls { get; private set; }

        public Task<ProductView> GetProduct(string productId)
        {
            GetProductCalls++;
            return Task.FromResult(new ProductView
            {
                GKey = 10,
                Id = productId,
                Category = "OLD METAL",
                Metal = "GOLD",
                Purity = "22K"
            });
        }

        public Task<ProductView?> GetOptionalProduct(string productId) =>
            throw new NotSupportedException();
        public Task<ProductView> GetByProductSku(string productSku) =>
            throw new NotSupportedException();
        public Task<ProductView> GetByCategory(string category) =>
            throw new NotSupportedException();
        public Task<IEnumerable<ProductView>> GetAll() =>
            throw new NotSupportedException();
    }

    private sealed class ProductStockSummaryServiceFake
        : IProductStockSummaryService
    {
        public int GetByProductGkeyCalls { get; private set; }

        public Task<ProductStockSummary> GetByProductGkey(int? productGkey)
        {
            GetByProductGkeyCalls++;
            return Task.FromResult(new ProductStockSummary
            {
                ProductGkey = productGkey,
                BalanceWeight = 10M,
                Uom = "Grams"
            });
        }

        public Task<ProductStockSummary> GetByGkey(int stockSummaryGkey) =>
            throw new NotSupportedException();
        public Task<ProductStockSummary> GetProductStockSummary(string productId) =>
            throw new NotSupportedException();
        public Task<ProductStockSummary> GetProductStockSummaryByProductSku(string productSku) =>
            throw new NotSupportedException();
        public Task<ProductStockSummary> GetProductStockSummaryByCategory(string category) =>
            throw new NotSupportedException();
        public Task CreateProductStockSummary(ProductStockSummary productStockSummary) =>
            throw new NotSupportedException();
        public Task UpdateProductStockSummary(ProductStockSummary productStockSummary) =>
            throw new NotSupportedException();
        public Task<IEnumerable<ProductStockSummary>> GetAll() =>
            throw new NotSupportedException();
    }
}
