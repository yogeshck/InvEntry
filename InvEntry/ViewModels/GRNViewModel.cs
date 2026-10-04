using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Printing;
using InvEntry.Extension;
using InvEntry.Models;
using InvEntry.Models.Extensions;
using InvEntry.Reports;
using InvEntry.Services;
using InvEntry.Store;
using InvEntry.Utils;
using InvEntry.Utils.Options;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using IDialogService = DevExpress.Mvvm.IDialogService;

namespace InvEntry.ViewModels
{
    public partial class GRNViewModel : ObservableObject
    {
        [ObservableProperty]
        private GrnHeader _header;

        [ObservableProperty]
        private string _categoryUI;

/*        [ObservableProperty]
        private string _supplierId;*/

        [ObservableProperty]
        private MtblReference _supplierId;

        [ObservableProperty]
        private ObservableCollection<string> _productCategoryList;

        [ObservableProperty]
        private ObservableCollection<GrnLineSummary> selectedRows;

        [ObservableProperty]
        private ObservableCollection<string> _supplierReferencesList;

        [ObservableProperty]
        private DateSearchOption _searchOption;

        /*        [ObservableProperty]
                private ObservableCollection<GrnLine> selectedRows;*/

        private readonly IGrnService _grnService;
        private readonly IProductCategoryService _productCategoryService;
        private readonly IProductService _productService;
        private readonly IProductStockSummaryService _productStockSummaryService;
        private readonly IProductStockService _productStockService;
        private readonly IMessageBoxService _messageBoxService;
        private readonly IDialogService _dialogService;
        private readonly IMtblReferencesService _mtblReferencesService;
        private readonly IProductStockMovementService _productStockMovementService;

        private Dictionary<string, Action<GrnLineSummary, decimal?>> copyGRNLineSumryExpression;
        private bool _isSubmitting;
        private bool _isPersisted;

        //private readonly IProductStockService _productStockService;
        //private readonly IDialogService _reportDialogService;
        //private readonly ICustomerService _customerService;

        public GRNViewModel(IGrnService                         grnService,
                            IProductService                     productService ,
                            IProductStockSummaryService         productStockSummaryService,
                            IDialogService                      dialogService,
                            IProductCategoryService             productCategoryService,
                            IMessageBoxService                  messageBoxService ,
                            IProductStockService                productStockService,
                            IProductStockMovementService        productStockMovementService,
                            IMtblReferencesService              mtblReferencesService)
        {
            _grnService = grnService;
            _productService = productService;
            _productStockSummaryService = productStockSummaryService;
            _dialogService = dialogService;
            _productCategoryService = productCategoryService;
            _productStockService = productStockService;
            _messageBoxService = messageBoxService;
            _mtblReferencesService = mtblReferencesService;
            _productStockMovementService = productStockMovementService;

            selectedRows = new();

            PopulateMtblSupplierListAsync();
            PopulateProductCategoryList();
            PopulateUnboundLineDataMap();

            SetHeader();

        }

        private void SetHeader()
        {
            DetachLineValidationNotifications();

            Header = new()
            {
                GrnDate = DateTime.Now,
                DocumentDate = DateTime.Now,
                ItemReceivedDate = DateTime.Now,
                Status = "Open"
            };

            AttachLineValidationNotifications();
        }

        private async void PopulateProductCategoryList()
        {
            var list = await _productCategoryService.GetProductCategoryList();
            ProductCategoryList = new(list.Select(x => x.Name));
        }

        private async void PopulateMtblSupplierListAsync()
        {
            var suppRefServiceList = await _mtblReferencesService.GetReferenceList("SUPPLIERS");
            SupplierReferencesList = new(suppRefServiceList.Select(x => x.RefValue));
        }

        partial void OnSupplierIdChanged(MtblReference? value)
        {
            if (Header is null)
                return;

            Header.SupplierId = value?.RefValue;
        }

        [RelayCommand]
        private async Task FetchProduct(EditValueChangedEventArgs args)
        {

            if (string.IsNullOrEmpty(CategoryUI)) return;

            var product = await _productService.GetByCategory(CategoryUI);

            if (product is null)
            {
                _messageBoxService.ShowMessage("Category " + CategoryUI + " not found., Contact Admin",
                                 "Product not found",
                                 MessageButton.OK);

                return;
            }

            GrnLineSummary grnLineSumry = new GrnLineSummary()
            {
                ProductGkey = product.GKey,
                ProductCategory = CategoryUI,
                SuppliedQty = 1,
                StoneWeight = 0,
                NetWeight = 0
            };

            SetLineSummary(grnLineSumry,product);

            //var NetWeight = grnLineSumry.GrossWeight.GetValueOrDefault() - grnLineSumry.StoneWeight.GetValueOrDefault();
            //grnLineSumry.NetWeight = Math.Round(NetWeight, 3, MidpointRounding.AwayFromZero);

            Header.GrnLineSumry.Add(grnLineSumry);

        }

        public void SetLineSummary(GrnLineSummary line, Product product)
        {
            line.ProductCategory = product.Category;
            line.Uom = product.Uom;
            line.ProductPurity = product.Purity;

           // line.GrossWeight = product.GrossWeight;
           // line.StoneWeight = product.StoneWeight;
           // line.NetWeight = product.GrossWeight - product.StoneWeight;
        }

/*        [RelayCommand]
        private void CellUpdate(CellValueChangedEventArgs args)
        {
            if (args.Row is GrnLineSummary line)
            {
               EvaluateFormula(line);
            }
        }*/


        [RelayCommand]
        private void CellUpdate(CellValueChangedEventArgs args)
        {
            if (args?.Row is not GrnLineSummary line)
                return;

            decimal gross = line.GrossWeight.GetValueOrDefault();
            decimal stone = line.StoneWeight.GetValueOrDefault();

            line.NetWeight = Math.Round(
                gross - stone,
                3,
                MidpointRounding.AwayFromZero);
        }

        [RelayCommand]
        private void ValidateGrnCell(GridCellValidationEventArgs args)
        {
            if (args?.Row is not GrnLineSummary line || args.Column is null)
                return;

            decimal? proposedValue = TryConvertDecimal(args.Value);

            if (args.Column.FieldName == nameof(GrnLineSummary.GrossWeight) &&
                (!proposedValue.HasValue || proposedValue.Value <= 0M))
            {
                args.SetError("Gross Weight must be greater than zero.");
            }
            else if (args.Column.FieldName == nameof(GrnLineSummary.StoneWeight))
            {
                if (!proposedValue.HasValue || proposedValue.Value < 0M)
                {
                    args.SetError("Stone Weight cannot be negative.");
                }
                else if (line.GrossWeight.HasValue &&
                         proposedValue.Value > line.GrossWeight.Value)
                {
                    args.SetError("Stone Weight cannot exceed Gross Weight.");
                }
            }
        }

        private static decimal? TryConvertDecimal(object? value)
        {
            if (value is null || string.IsNullOrWhiteSpace(value.ToString()))
                return null;

            try
            {
                return Convert.ToDecimal(value);
            }
            catch (Exception) when (value is string || value is IConvertible)
            {
                return null;
            }
        }

        private bool CanSubmit() =>
            !_isSubmitting &&
            !_isPersisted &&
            Header is not null &&
            Header.GKey <= 0 &&
            Header.GrnLineSumry is { Count: > 0 } &&
            Header.GrnLineSumry.All(IsValidLine);

        [RelayCommand(CanExecute = nameof(CanSubmit))]
        private async Task Submit()
        {
            if (_isSubmitting ||
                _isPersisted ||
                Header is null ||
                Header.GKey > 0)
            {
                _messageBoxService.ShowMessage(
                    "This material receipt has already been saved or is currently being saved.",
                    "Material Receipt",
                    MessageButton.OK,
                    MessageIcon.Information);
                return;
            }

            if (Header.GrnLineSumry is not { Count: > 0 })
            {
                _messageBoxService.ShowMessage(
                    "Please add at least one item before saving the GRN.",
                    "Material Receipt",
                    MessageButton.OK,
                    MessageIcon.Warning);
                return;
            }

            if (!TryValidateLines(Header.GrnLineSumry, out string validationMessage))
            {
                _messageBoxService.ShowMessage(
                    validationMessage,
                    "Material Receipt",
                    MessageButton.OK,
                    MessageIcon.Warning);
                return;
            }

            _isSubmitting = true;
            SubmitCommand.NotifyCanExecuteChanged();

            string? savedGrnNumber = null;

            try
            {
                var lines = Header.GrnLineSumry.ToList();
                var header = await _grnService.CreateHeader(Header);

                if (header is null || header.GKey <= 0)
                {
                    throw new InvalidOperationException(
                        "The GRN header response did not contain a valid record key.");
                }

                Header.GKey = header.GKey;
                Header.GrnNbr = header.GrnNbr;
                savedGrnNumber = header.GrnNbr;
                _isPersisted = true;
                SubmitCommand.NotifyCanExecuteChanged();

                var savedDocumentDate = Header.GrnDate;
                var savedSupplierId = Header.SupplierId;

                lines.ForEach(x =>
                {
                    x.GrnHdrGkey    = header.GKey;
                    x.LineNbr       = lines.IndexOf(x) + 1;
                });

                await _grnService.CreateGrnLineSummary(lines);

                var summaryKeys = await ProcessStockSummary(
                    lines,
                    savedGrnNumber,
                    savedDocumentDate);

                await CreateTemporaryStockItemsAsync(
                    lines,
                    summaryKeys,
                    savedSupplierId);

                _messageBoxService.ShowMessage(
                    "GRN " + savedGrnNumber + " was created successfully.",
                    "GRN Creation",
                    MessageButton.OK,
                    MessageIcon.Information);

                ResetGRN();
            }
            catch (Exception ex)
            {
                Log.Error(
                    ex,
                    "GRN save processing encountered an exception. Persisted: {Persisted}, GRN: {GrnNumber}",
                    _isPersisted,
                    savedGrnNumber ?? Header?.GrnNbr);

                var message = _isPersisted
                    ? $"GRN {savedGrnNumber ?? Header?.GrnNbr} was saved, but follow-up processing could not be completed. " +
                      "Please contact support before continuing. Saving this receipt again has been disabled."
                    : "The material receipt could not be saved. Please review the details and try again, or contact support.";

                _messageBoxService.ShowMessage(
                    message,
                    "Material Receipt",
                    MessageButton.OK,
                    MessageIcon.Error);
            }
            finally
            {
                _isSubmitting = false;
                SubmitCommand.NotifyCanExecuteChanged();
            }
        }

        private bool CanDeleteRows()
        {
            return SelectedRows?.Any() ?? false;
        }

        [RelayCommand(CanExecute = nameof(CanDeleteRows))]
        private void DeleteRows()
        {
            var result = _messageBoxService.ShowMessage("Delete all selected rows", "Delete Rows", MessageButton.YesNo, MessageIcon.Question, MessageResult.No);

            if (result == MessageResult.No)
                return;

            List<int> indexs = new List<int>();
            foreach (var row in SelectedRows)
            {
                indexs.Add(Header.GrnLineSumry.IndexOf(row));
            }

            indexs.ForEach(x =>
            {
                if (x >= 0)
                {
                    Header.GrnLineSumry.RemoveAt(x);
                }
            });

           // EvaluateForAllLines();
           // EvaluateHeader();
        }

        [RelayCommand(CanExecute = nameof(CanDeleteSingleRow))]
        private void DeleteSingleRow(GrnLineSummary grnline)
        {
            var result = _messageBoxService.ShowMessage("Delete current row", "Delete Row", MessageButton.YesNo, 
                                                        MessageIcon.Question, MessageResult.No);

            if (result == MessageResult.No)
                   return;

            var index = Header.GrnLineSumry.Remove(grnline);
        }

        private bool CanDeleteSingleRow(GrnLineSummary grnline)
        {
            return grnline is not null && Header.GrnLineSumry.IndexOf(grnline) > -1;
        }


        [RelayCommand]
        private void ResetGRN()
        {
            _isPersisted = false;
            SetHeader();

            SupplierId = null;
            SelectedRows?.Clear();
            SubmitCommand.NotifyCanExecuteChanged();
        }

        private void AttachLineValidationNotifications()
        {
            if (Header?.GrnLineSumry is null)
                return;

            Header.GrnLineSumry.CollectionChanged += GrnLines_CollectionChanged;
            foreach (var line in Header.GrnLineSumry)
                line.PropertyChanged += GrnLine_PropertyChanged;
        }

        private void DetachLineValidationNotifications()
        {
            if (Header?.GrnLineSumry is null)
                return;

            Header.GrnLineSumry.CollectionChanged -= GrnLines_CollectionChanged;
            foreach (var line in Header.GrnLineSumry)
                line.PropertyChanged -= GrnLine_PropertyChanged;
        }

        private void GrnLines_CollectionChanged(
            object? sender,
            NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems is not null)
            {
                foreach (GrnLineSummary line in e.OldItems)
                    line.PropertyChanged -= GrnLine_PropertyChanged;
            }

            if (e.NewItems is not null)
            {
                foreach (GrnLineSummary line in e.NewItems)
                    line.PropertyChanged += GrnLine_PropertyChanged;
            }

            SubmitCommand.NotifyCanExecuteChanged();
        }

        private void GrnLine_PropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(GrnLineSummary.GrossWeight) or
                nameof(GrnLineSummary.StoneWeight) or
                nameof(GrnLineSummary.NetWeight))
            {
                SubmitCommand.NotifyCanExecuteChanged();
            }
        }

        private static bool IsValidLine(GrnLineSummary line) =>
            TryValidateLine(line, 0, out _);

        private static bool TryValidateLines(
            IReadOnlyList<GrnLineSummary> lines,
            out string message)
        {
            for (int index = 0; index < lines.Count; index++)
            {
                if (!TryValidateLine(lines[index], index + 1, out message))
                    return false;
            }

            message = string.Empty;
            return true;
        }

        private static bool TryValidateLine(
            GrnLineSummary line,
            int rowNumber,
            out string message)
        {
            string prefix = rowNumber > 0 ? $"Row {rowNumber}: " : string.Empty;

            if (!line.GrossWeight.HasValue || line.GrossWeight.Value <= 0M)
            {
                message = prefix + "Gross Weight must be greater than zero.";
                return false;
            }

            decimal stone = line.StoneWeight.GetValueOrDefault();
            if (stone < 0M)
            {
                message = prefix + "Stone Weight cannot be negative.";
                return false;
            }

            if (stone > line.GrossWeight.Value)
            {
                message = prefix + "Stone Weight cannot exceed Gross Weight.";
                return false;
            }

            decimal expectedNet = Math.Round(
                line.GrossWeight.Value - stone,
                3,
                MidpointRounding.AwayFromZero);

            if (line.NetWeight != expectedNet)
            {
                message = prefix + "Net Weight must equal Gross Weight minus Stone Weight.";
                return false;
            }

            message = string.Empty;
            return true;
        }

        private async Task CreateTemporaryStockItemsAsync(
            IEnumerable<GrnLineSummary> summaries,
            IReadOnlyDictionary<string, int> summaryKeys,
            string? supplierId)
        {
            foreach (var stock in BuildTemporaryStockItems(
                         summaries,
                         summaryKeys,
                         supplierId))
            {
                await _productStockService.CreateProductStock(stock);
            }
        }

        private static IReadOnlyList<ProductStock> BuildTemporaryStockItems(
            IEnumerable<GrnLineSummary> summaries,
            IReadOnlyDictionary<string, int> summaryKeys,
            string? supplierId)
        {
            var stocks = new List<ProductStock>();

            foreach (var line in summaries)
            {
                if (line.GKey <= 0)
                {
                    throw new InvalidOperationException(
                        $"GRN line summary was not saved for category {line.ProductCategory}.");
                }

                if (string.IsNullOrWhiteSpace(line.ProductCategory) ||
                    !summaryKeys.TryGetValue(line.ProductCategory, out int stockSummaryGkey))
                {
                    throw new InvalidOperationException(
                        $"Stock summary not found for category {line.ProductCategory}.");
                }

                for (int i = 0; i < line.SuppliedQty.GetValueOrDefault(); i++)
                {
                    stocks.Add(new ProductStock
                    {
                        StockSummaryGkey = stockSummaryGkey,
                        GrnLineSummaryGkey = line.GKey,
                        ProductGkey = line.ProductGkey,
                        Category = line.ProductCategory,
                        SupplierId = supplierId,
                        ProductSku = $"TMP-{Guid.NewGuid():N}",
                        SuppliedQty = 1,
                        StockQty = 1,
                        SoldQty = 0,
                        GrossWeight = null,
                        StoneWeight = 0,
                        NetWeight = null,
                        IsProductSold = false,
                        Status = "Pending Tag",
                        IsBarcodePrinted = false,
                        CreatedOn = DateTime.Now
                    });
                }
            }

            return stocks;
        }

        private async Task<Dictionary<string, int>> ProcessStockSummary(
            IEnumerable<GrnLineSummary> grnLineSummary,
            string? grnNumber,
            DateTime? grnDate)
        {
            var summaryKeys = new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var line in grnLineSummary)
            {
                if (line.GKey <= 0)
                {
                    throw new InvalidOperationException(
                        $"GRN line summary was not saved for category {line.ProductCategory}.");
                }

                if (!line.ProductGkey.HasValue ||
                    line.ProductGkey.Value <= 0)
                {
                    throw new InvalidOperationException(
                        $"Product was not identified for category {line.ProductCategory}.");
                }

                if (string.IsNullOrWhiteSpace(line.ProductCategory))
                {
                    throw new InvalidOperationException(
                        "Product category is required for stock movement.");
                }

                var request = new StockMovementRequest
                {
                    ProductGkey = line.ProductGkey.Value,
                    ProductCategory = line.ProductCategory,

                    Direction = StockMovementDirection.In,

                    Quantity = line.SuppliedQty.GetValueOrDefault(),

                    GrossWeight = line.GrossWeight.GetValueOrDefault(),
                    StoneWeight = line.StoneWeight.GetValueOrDefault(),
                    NetWeight = line.NetWeight.GetValueOrDefault(),

                    TransactionDate = grnDate ?? DateTime.Now,

                    // Header/source document
                    RefGkey = Header?.GKey,

                    // Exact GRN line = idempotency source
                    RefLineGkey = line.GKey,

                    DocumentNbr = grnNumber ?? string.Empty,
                    DocumentType = "GRN",
                    TransactionType = "Receipt",

                    Notes = $"Material receipt {grnNumber}"
                };

                await _productStockMovementService.ApplyAsync(request);

                /*
                 * IMPORTANT:
                 * The movement API has already updated PRODUCT_STOCK_SUMMARY.
                 *
                 * We fetch it again ONLY because ProductStock temporary
                 * records require StockSummaryGkey.
                 *
                 * Do NOT modify/save ProductStockSummary here.
                 */
                var savedSummary =
                    await _productStockSummaryService
                        .GetProductStockSummaryByCategory(
                            line.ProductCategory);

                if (savedSummary is null ||
                    savedSummary.GKey <= 0)
                {
                    throw new InvalidOperationException(
                        $"Stock summary was not found after processing category {line.ProductCategory}.");
                }

                summaryKeys[line.ProductCategory] =
                    savedSummary.GKey;
            }

            return summaryKeys;
        }


        private void EvaluateFormula<T>(T item, bool isInit = false)
            where T : class
        {
            if (item is null)
                return;

            if (item is GrnLineSummary line)
            {
                decimal gross = line.GrossWeight.GetValueOrDefault();
                decimal stone = line.StoneWeight.GetValueOrDefault();

                line.NetWeight = Math.Round(
                    gross - stone,
                    3,
                    MidpointRounding.AwayFromZero);

                return;
            }

            var formulas = FormulaStore.Instance.GetFormulas<T>();

            if (formulas is null)
                return;

            foreach (var formula in formulas)
            {
                if (formula is null)
                    continue;

                var val = formula.Evaluate<T, decimal>(item, 0M);

                if (item is GrnLineSummary grnLineSumry &&
                    copyGRNLineSumryExpression is not null &&
                    copyGRNLineSumryExpression.TryGetValue(
                        formula.FieldName,
                        out var setter))
                {
                    setter(grnLineSumry, val);
                }
            }
        }

        private void PopulateUnboundLineDataMap()
        {
            if (copyGRNLineSumryExpression is null) copyGRNLineSumryExpression = new();

            copyGRNLineSumryExpression.Add($"{nameof(GrnLineSummary.NetWeight)}", (item, val) => item.NetWeight = val);
        }

        }
}
