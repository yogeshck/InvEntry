using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Models;

public partial class MijmsContext : DbContext
{
    public MijmsContext()
    {
    }

    public MijmsContext(DbContextOptions<MijmsContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CashTransaction> CashTransactions { get; set; }

    public virtual DbSet<DailyRate> DailyRates { get; set; }

    public virtual DbSet<DailyStockSummary> DailyStockSummaries { get; set; }

    public virtual DbSet<EstimateHeader> EstimateHeaders { get; set; }

    public virtual DbSet<EstimateLine> EstimateLines { get; set; }

    public virtual DbSet<GrnHeader> GrnHeaders { get; set; }

    public virtual DbSet<GrnLine> GrnLines { get; set; }

    public virtual DbSet<GrnLineSummary> GrnLineSummaries { get; set; }

    public virtual DbSet<InvoiceArReceipt> InvoiceArReceipts { get; set; }

    public virtual DbSet<InvoiceHeader> InvoiceHeaders { get; set; }

    public virtual DbSet<InvoiceLine> InvoiceLines { get; set; }

    public virtual DbSet<LedgersHeader> LedgersHeaders { get; set; }

    public virtual DbSet<LedgersTransaction> LedgersTransactions { get; set; }

    public virtual DbSet<Metal> Metals { get; set; }

    public virtual DbSet<MtblLedger> MtblLedgers { get; set; }

    public virtual DbSet<MtblReference> MtblReferences { get; set; }

    public virtual DbSet<MtblVoucherType> MtblVoucherTypes { get; set; }

    public virtual DbSet<OldMetalTransaction> OldMetalTransactions { get; set; }

    public virtual DbSet<OpeningStock> OpeningStocks { get; set; }

    public virtual DbSet<OrgAddress> OrgAddresses { get; set; }

    public virtual DbSet<OrgBankDetail> OrgBankDetails { get; set; }

    public virtual DbSet<OrgCompany> OrgCompanies { get; set; }

    public virtual DbSet<OrgContact> OrgContacts { get; set; }

    public virtual DbSet<OrgContactEmail> OrgContactEmails { get; set; }

    public virtual DbSet<OrgCustomer> OrgCustomers { get; set; }

    public virtual DbSet<OrgCustomerAddressView> OrgCustomerAddressViews { get; set; }

    public virtual DbSet<OrgGeoLocation> OrgGeoLocations { get; set; }

    public virtual DbSet<OrgPlace> OrgPlaces { get; set; }

    public virtual DbSet<OrgThisCompanyView> OrgThisCompanyViews { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductCategory> ProductCategories { get; set; }

    public virtual DbSet<ProductGroup> ProductGroups { get; set; }

    public virtual DbSet<ProductStock> ProductStocks { get; set; }

    public virtual DbSet<ProductStockSummary> ProductStockSummaries { get; set; }

    public virtual DbSet<ProductTransaction> ProductTransactions { get; set; }

    public virtual DbSet<ProductTransactionSummary> ProductTransactionSummaries { get; set; }

    public virtual DbSet<ProductView> ProductViews { get; set; }

    public virtual DbSet<RepDailyStockSummary> RepDailyStockSummaries { get; set; }

    public virtual DbSet<Test> Tests { get; set; }

    public virtual DbSet<Voucher> Vouchers { get; set; }

    public virtual DbSet<VoucherType> VoucherTypes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=.\\SQLEXPRESS;Initial Catalog=mijms;TrustServerCertificate=True;Trusted_Connection=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CashTransaction>(entity =>
        {
            entity.HasKey(e => e.Gkey).HasName("PK_cash_transaction_summary");

            entity.ToTable("CASH_TRANSACTION");

            entity.Property(e => e.Gkey).HasColumnName("GKEY");
            entity.Property(e => e.ClosingBalance)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("CLOSING_BALANCE");
            entity.Property(e => e.LedgerBook)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("LEDGER_BOOK");
            entity.Property(e => e.LedgerKey).HasColumnName("LEDGER_KEY");
            entity.Property(e => e.OpeningBalance)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("OPENING_BALANCE");
            entity.Property(e => e.Payments)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("PAYMENTS");
            entity.Property(e => e.Receipts)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("RECEIPTS");
            entity.Property(e => e.TransactionDate).HasColumnName("TRANSACTION_DATE");
        });

        modelBuilder.Entity<DailyRate>(entity =>
        {
            entity.HasKey(e => e.Gkey);

            entity.ToTable("DAILY_RATE");

            entity.Property(e => e.Gkey).HasColumnName("GKEY");
            entity.Property(e => e.Carat)
                .HasMaxLength(20)
                .HasColumnName("CARAT");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(30)
                .HasColumnName("CREATED_BY");
            entity.Property(e => e.CreatedOn).HasColumnName("CREATED_ON");
            entity.Property(e => e.EffectiveDate).HasColumnName("EFFECTIVE_DATE");
            entity.Property(e => e.IsDisplay).HasColumnName("IS_DISPLAY");
            entity.Property(e => e.Metal)
                .HasMaxLength(20)
                .HasColumnName("METAL");
            entity.Property(e => e.ModifiedBy)
                .HasMaxLength(20)
                .HasColumnName("MODIFIED_BY");
            entity.Property(e => e.ModifiedOn).HasColumnName("MODIFIED_ON");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(9, 2)")
                .HasColumnName("PRICE");
            entity.Property(e => e.Purity)
                .HasMaxLength(20)
                .HasColumnName("PURITY");
        });

        modelBuilder.Entity<DailyStockSummary>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("daily_stock_summary");

            entity.Property(e => e.OpeningQtyFirstTransaction)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("opening_qty_first_transaction");
            entity.Property(e => e.ProductCategory)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PRODUCT_CATEGORY");
            entity.Property(e => e.StockIn).HasColumnType("decimal(38, 3)");
            entity.Property(e => e.StockOut).HasColumnType("decimal(38, 3)");
            entity.Property(e => e.TotalTransactedQty)
                .HasColumnType("decimal(38, 3)")
                .HasColumnName("total_transacted_qty");
            entity.Property(e => e.TransactionDate).HasColumnName("TRANSACTION_DATE");
        });

        modelBuilder.Entity<EstimateHeader>(entity =>
        {
            entity.HasKey(e => e.Gkey);

            entity.ToTable("ESTIMATE_HEADER");

            entity.Property(e => e.Gkey).HasColumnName("GKEY");
            entity.Property(e => e.AdvanceAdj)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("ADVANCE_ADJ");
            entity.Property(e => e.AmountPayable)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("AMOUNT_PAYABLE");
            entity.Property(e => e.CgstAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("CGST_AMOUNT");
            entity.Property(e => e.CgstPercent)
                .HasColumnType("decimal(4, 2)")
                .HasColumnName("CGST_PERCENT");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("CREATED_BY");
            entity.Property(e => e.CreatedOn)
                .HasPrecision(6)
                .HasColumnName("CREATED_ON");
            entity.Property(e => e.CustGkey).HasColumnName("CUST_GKEY");
            entity.Property(e => e.CustMobile)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("CUST_MOBILE");
            entity.Property(e => e.DeliveryMethod)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("DELIVERY_METHOD");
            entity.Property(e => e.DeliveryRef).HasColumnName("DELIVERY_REF");
            entity.Property(e => e.DiscountAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("DISCOUNT_AMOUNT");
            entity.Property(e => e.DiscountPercent)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("DISCOUNT_PERCENT");
            entity.Property(e => e.EstBalance)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("EST_BALANCE");
            entity.Property(e => e.EstDate)
                .HasPrecision(6)
                .HasColumnName("EST_DATE");
            entity.Property(e => e.EstNbr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("EST_NBR");
            entity.Property(e => e.EstNotes)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("EST_NOTES");
            entity.Property(e => e.EstRefund)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("EST_REFUND");
            entity.Property(e => e.EstTaxableAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("EST_TAXABLE_AMOUNT");
            entity.Property(e => e.EstlTaxTotal)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ESTL_TAX_TOTAL");
            entity.Property(e => e.GrossRcbAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("GROSS_RCB_AMOUNT");
            entity.Property(e => e.GstLocBuyer)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("GST_LOC_BUYER");
            entity.Property(e => e.GstLocSeller)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("GST_LOC_SELLER");
            entity.Property(e => e.IgstAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("IGST_AMOUNT");
            entity.Property(e => e.IgstPercent)
                .HasColumnType("decimal(4, 2)")
                .HasColumnName("IGST_PERCENT");
            entity.Property(e => e.IsTaxApplicable).HasColumnName("IS_TAX_APPLICABLE");
            entity.Property(e => e.ModifiedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("MODIFIED_BY");
            entity.Property(e => e.ModifiedOn)
                .HasPrecision(6)
                .HasColumnName("MODIFIED_ON");
            entity.Property(e => e.OldGoldAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("OLD_GOLD_AMOUNT");
            entity.Property(e => e.OldSilverAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("OLD_SILVER_AMOUNT");
            entity.Property(e => e.OrderDate)
                .HasPrecision(6)
                .HasColumnName("ORDER_DATE");
            entity.Property(e => e.OrderNbr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ORDER_NBR");
            entity.Property(e => e.PaymentDueDate)
                .HasPrecision(6)
                .HasColumnName("PAYMENT_DUE_DATE");
            entity.Property(e => e.PaymentMode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PAYMENT_MODE");
            entity.Property(e => e.RdAmountAdj)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("RD_AMOUNT_ADJ");
            entity.Property(e => e.RecdAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("RECD_AMOUNT");
            entity.Property(e => e.RoundOff)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("ROUND_OFF");
            entity.Property(e => e.SgstAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("SGST_AMOUNT");
            entity.Property(e => e.SgstPercent)
                .HasColumnType("decimal(4, 2)")
                .HasColumnName("SGST_PERCENT");
            entity.Property(e => e.TaxType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("TAX_TYPE");
            entity.Property(e => e.TenantGkey).HasColumnName("TENANT_GKEY");
        });

        modelBuilder.Entity<EstimateLine>(entity =>
        {
            entity.HasKey(e => e.Gkey);

            entity.ToTable("ESTIMATE_LINE");

            entity.Property(e => e.Gkey).HasColumnName("GKEY");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("CREATED_BY");
            entity.Property(e => e.CreatedOn)
                .HasPrecision(6)
                .HasColumnName("CREATED_ON");
            entity.Property(e => e.EstLineNbr).HasColumnName("EST_LINE_NBR");
            entity.Property(e => e.EstNote)
                .IsUnicode(false)
                .HasColumnName("EST_NOTE");
            entity.Property(e => e.EstimateHdrGkey).HasColumnName("ESTIMATE_HDR_GKEY");
            entity.Property(e => e.EstimateId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ESTIMATE_ID");
            entity.Property(e => e.EstlBilledPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ESTL_BILLED_PRICE");
            entity.Property(e => e.EstlCgstAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ESTL_CGST_AMOUNT");
            entity.Property(e => e.EstlCgstPercent)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ESTL_CGST_PERCENT");
            entity.Property(e => e.EstlGrossAmt)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ESTL_GROSS_AMT");
            entity.Property(e => e.EstlIgstAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ESTL_IGST_AMOUNT");
            entity.Property(e => e.EstlIgstPercent)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ESTL_IGST_PERCENT");
            entity.Property(e => e.EstlMakingCharges)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ESTL_MAKING_CHARGES");
            entity.Property(e => e.EstlOtherCharges)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ESTL_OTHER_CHARGES");
            entity.Property(e => e.EstlPayableAmt)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ESTL_PAYABLE_AMT");
            entity.Property(e => e.EstlSgstAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ESTL_SGST_AMOUNT");
            entity.Property(e => e.EstlSgstPercent)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ESTL_SGST_PERCENT");
            entity.Property(e => e.EstlStoneAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ESTL_STONE_AMOUNT");
            entity.Property(e => e.EstlTaxableAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ESTL_TAXABLE_AMOUNT");
            entity.Property(e => e.EstlTotal)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ESTL_TOTAL");
            entity.Property(e => e.EstlWastageAmt)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ESTL_WASTAGE_AMT");
            entity.Property(e => e.HsnCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("HSN_CODE");
            entity.Property(e => e.IsTaxable).HasColumnName("IS_TAXABLE");
            entity.Property(e => e.ItemNotes)
                .IsUnicode(false)
                .HasColumnName("ITEM_NOTES");
            entity.Property(e => e.ItemPacked).HasColumnName("ITEM_PACKED");
            entity.Property(e => e.Metal)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("METAL");
            entity.Property(e => e.ModifiedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("MODIFIED_BY");
            entity.Property(e => e.ModifiedOn)
                .HasPrecision(6)
                .HasColumnName("MODIFIED_ON");
            entity.Property(e => e.ProdCategory)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("PROD_CATEGORY");
            entity.Property(e => e.ProdGrossWeight)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("PROD_GROSS_WEIGHT");
            entity.Property(e => e.ProdNetWeight)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("PROD_NET_WEIGHT");
            entity.Property(e => e.ProdPackCode)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("PROD_PACK_CODE");
            entity.Property(e => e.ProdQty).HasColumnName("PROD_QTY");
            entity.Property(e => e.ProdStoneWeight)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("PROD_STONE_WEIGHT");
            entity.Property(e => e.ProductDesc)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("PRODUCT_DESC");
            entity.Property(e => e.ProductGkey).HasColumnName("PRODUCT_GKEY");
            entity.Property(e => e.ProductId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PRODUCT_ID");
            entity.Property(e => e.ProductName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("PRODUCT_NAME");
            entity.Property(e => e.ProductPurity)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("PRODUCT_PURITY");
            entity.Property(e => e.TaxAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("TAX_AMOUNT");
            entity.Property(e => e.TaxPercent)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("TAX_PERCENT");
            entity.Property(e => e.TaxType)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("TAX_TYPE");
            entity.Property(e => e.TenantGkey).HasColumnName("TENANT_GKEY");
            entity.Property(e => e.VaAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("VA_AMOUNT");
            entity.Property(e => e.VaPercent)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("VA_PERCENT");
        });

        modelBuilder.Entity<GrnHeader>(entity =>
        {
            entity.HasKey(e => e.Gkey);

            entity.ToTable("GRN_HEADER");

            entity.Property(e => e.Gkey).HasColumnName("GKEY");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("CREATED_BY");
            entity.Property(e => e.CreatedOn)
                .HasPrecision(6)
                .HasColumnName("CREATED_ON");
            entity.Property(e => e.CustomerOrderNbr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("CUSTOMER_ORDER_NBR");
            entity.Property(e => e.DocumentDate)
                .HasPrecision(6)
                .HasColumnName("DOCUMENT_DATE");
            entity.Property(e => e.DocumentRef)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("DOCUMENT_REF");
            entity.Property(e => e.DocumentType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("DOCUMENT_TYPE");
            entity.Property(e => e.GrnDate)
                .HasPrecision(6)
                .HasColumnName("GRN_DATE");
            entity.Property(e => e.GrnNbr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("GRN_NBR");
            entity.Property(e => e.ItemReceivedDate).HasColumnName("ITEM_RECEIVED_DATE");
            entity.Property(e => e.ModifiedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("MODIFIED_BY");
            entity.Property(e => e.ModifiedOn)
                .HasPrecision(6)
                .HasColumnName("MODIFIED_ON");
            entity.Property(e => e.OrderDate)
                .HasPrecision(6)
                .HasColumnName("ORDER_DATE");
            entity.Property(e => e.OrderNbr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ORDER_NBR");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("STATUS");
            entity.Property(e => e.SupplierId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("SUPPLIER_ID");
            entity.Property(e => e.SupplierRefNbr)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("SUPPLIER_REF_NBR");
        });

        modelBuilder.Entity<GrnLine>(entity =>
        {
            entity.HasKey(e => e.Gkey);

            entity.ToTable("GRN_LINE");

            entity.Property(e => e.Gkey).HasColumnName("GKEY");
            entity.Property(e => e.AcceptedQty).HasColumnName("ACCEPTED_QTY");
            entity.Property(e => e.GrnHdrGkey).HasColumnName("GRN_HDR_GKEY");
            entity.Property(e => e.GrnLineSumryGkey).HasColumnName("GRN_LINE_SUMRY_GKEY");
            entity.Property(e => e.GrossWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("GROSS_WEIGHT");
            entity.Property(e => e.LineNbr).HasColumnName("LINE_NBR");
            entity.Property(e => e.NetWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("NET_WEIGHT");
            entity.Property(e => e.Notes)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("NOTES");
            entity.Property(e => e.OrderedQty).HasColumnName("ORDERED_QTY");
            entity.Property(e => e.ProductDesc)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("product_desc");
            entity.Property(e => e.ProductGkey).HasColumnName("PRODUCT_GKEY");
            entity.Property(e => e.ProductId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("product_ID");
            entity.Property(e => e.ProductPurity)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PRODUCT_PURITY");
            entity.Property(e => e.ProductSku)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PRODUCT_SKU");
            entity.Property(e => e.ReceivedQty).HasColumnName("RECEIVED_QTY");
            entity.Property(e => e.RejectedQty).HasColumnName("REJECTED_QTY");
            entity.Property(e => e.ReturnedQty).HasColumnName("RETURNED_QTY");
            entity.Property(e => e.Size)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("SIZE");
            entity.Property(e => e.SizeId).HasColumnName("SIZE_ID");
            entity.Property(e => e.SizeUom)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("SIZE_UOM");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("STATUS");
            entity.Property(e => e.StoneWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("STONE_WEIGHT");
            entity.Property(e => e.SuppMakingCharges)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("SUPP_MAKING_CHARGES");
            entity.Property(e => e.SuppProductDesc)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("supp_product_desc");
            entity.Property(e => e.SuppProductNbr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("supp_product_nbr");
            entity.Property(e => e.SuppRate)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("SUPP_RATE");
            entity.Property(e => e.SuppVaPercent)
                .HasColumnType("decimal(4, 2)")
                .HasColumnName("SUPP_VA_PERCENT");
            entity.Property(e => e.SuppWastageAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("SUPP_WASTAGE_AMOUNT");
            entity.Property(e => e.SuppWastagePercent)
                .HasColumnType("decimal(4, 2)")
                .HasColumnName("SUPP_WASTAGE_PERCENT");
            entity.Property(e => e.SuppliedQty).HasColumnName("SUPPLIED_QTY");
            entity.Property(e => e.Uom)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("UOM");
        });

        modelBuilder.Entity<GrnLineSummary>(entity =>
        {
            entity.HasKey(e => e.Gkey).HasName("PK_grn_line_summary");

            entity.ToTable("GRN_LINE_SUMMARY");

            entity.Property(e => e.Gkey).HasColumnName("gkey");
            entity.Property(e => e.GrnHdrGkey).HasColumnName("grn_hdr_gkey");
            entity.Property(e => e.GrossWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("gross_weight");
            entity.Property(e => e.LineNbr).HasColumnName("line_nbr");
            entity.Property(e => e.NetWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("net_weight");
            entity.Property(e => e.ProductCategory)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("product_category");
            entity.Property(e => e.ProductGkey).HasColumnName("product_gkey");
            entity.Property(e => e.ProductPurity)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("product_purity");
            entity.Property(e => e.StoneWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("stone_weight");
            entity.Property(e => e.SuppliedQty).HasColumnName("supplied_qty");
            entity.Property(e => e.Uom)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("uom");
        });

        modelBuilder.Entity<InvoiceArReceipt>(entity =>
        {
            entity.HasKey(e => e.Gkey).HasName("PK_ar_invoice_receipts");

            entity.ToTable("INVOICE_AR_RECEIPTS");

            entity.Property(e => e.Gkey).HasColumnName("gkey");
            entity.Property(e => e.AdjustedAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("adjusted_amount");
            entity.Property(e => e.BalBeforeAdj)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("bal_before_adj");
            entity.Property(e => e.BalanceAfterAdj)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("balance_after_adj");
            entity.Property(e => e.BankName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("bank_name");
            entity.Property(e => e.CompanyBankAccountNbr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("company_bank_account_nbr");
            entity.Property(e => e.CustGkey).HasColumnName("cust_gkey");
            entity.Property(e => e.ExternalTransactionDate).HasColumnName("external_transaction_date");
            entity.Property(e => e.ExternalTransactionId)
                .HasMaxLength(50)
                .HasColumnName("external_transaction_id");
            entity.Property(e => e.InternalVoucherDate).HasColumnName("internal_voucher_date");
            entity.Property(e => e.InternalVoucherNbr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("internal_voucher_nbr");
            entity.Property(e => e.InvoiceGkey).HasColumnName("invoice_gkey");
            entity.Property(e => e.InvoiceNbr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("invoice_nbr");
            entity.Property(e => e.InvoiceReceiptNbr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("invoice_receipt_nbr");
            entity.Property(e => e.InvoiceReceivableAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("invoice_receivable_amount");
            entity.Property(e => e.ModeOfReceipt)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("mode_of_receipt");
            entity.Property(e => e.OtherReference)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("other_reference");
            entity.Property(e => e.SenderBankAccountNbr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("sender_bank_account_nbr");
            entity.Property(e => e.SenderBankBranch)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("sender_bank_branch");
            entity.Property(e => e.SenderBankGkey).HasColumnName("sender_bank_gkey");
            entity.Property(e => e.SenderBankIfscCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("sender_bank_ifsc_code");
            entity.Property(e => e.SeqNbr).HasColumnName("seq_nbr");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.TransactionType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("transaction_type");
        });

        modelBuilder.Entity<InvoiceHeader>(entity =>
        {
            entity.HasKey(e => e.Gkey);

            entity.ToTable("INVOICE_HEADER");

            entity.Property(e => e.Gkey).HasColumnName("GKEY");
            entity.Property(e => e.AdvanceAdj)
                .HasDefaultValueSql("('0.00')")
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("ADVANCE_ADJ");
            entity.Property(e => e.AmountPayable)
                .HasDefaultValueSql("('0.00')")
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("AMOUNT_PAYABLE");
            entity.Property(e => e.CgstAmount)
                .HasDefaultValueSql("('0.00')")
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("CGST_AMOUNT");
            entity.Property(e => e.CgstPercent)
                .HasDefaultValueSql("('0.00')")
                .HasColumnType("decimal(4, 2)")
                .HasColumnName("CGST_PERCENT");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("CREATED_BY");
            entity.Property(e => e.CreatedOn)
                .HasPrecision(6)
                .HasColumnName("CREATED_ON");
            entity.Property(e => e.CustGkey).HasColumnName("CUST_GKEY");
            entity.Property(e => e.CustMobile)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("CUST_MOBILE");
            entity.Property(e => e.DeliveryMethod)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("DELIVERY_METHOD");
            entity.Property(e => e.DeliveryRef).HasColumnName("DELIVERY_REF");
            entity.Property(e => e.DiscountAmount)
                .HasDefaultValueSql("('0.00')")
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("DISCOUNT_AMOUNT");
            entity.Property(e => e.DiscountPercent)
                .HasDefaultValueSql("('0.00')")
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("DISCOUNT_PERCENT");
            entity.Property(e => e.GrossRcbAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("GROSS_RCB_AMOUNT");
            entity.Property(e => e.GstLocBuyer)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("GST_LOC_BUYER");
            entity.Property(e => e.GstLocSeller)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("GST_LOC_SELLER");
            entity.Property(e => e.IgstAmount)
                .HasDefaultValueSql("('0.00')")
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("IGST_AMOUNT");
            entity.Property(e => e.IgstPercent)
                .HasDefaultValueSql("('0.00')")
                .HasColumnType("decimal(4, 2)")
                .HasColumnName("IGST_PERCENT");
            entity.Property(e => e.InvBalance)
                .HasDefaultValueSql("('0.00')")
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("INV_BALANCE");
            entity.Property(e => e.InvDate)
                .HasPrecision(6)
                .HasColumnName("INV_DATE");
            entity.Property(e => e.InvNbr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("INV_NBR");
            entity.Property(e => e.InvNotes)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("INV_NOTES");
            entity.Property(e => e.InvRefund)
                .HasDefaultValueSql("('0.00')")
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("INV_REFUND");
            entity.Property(e => e.InvTaxableAmount)
                .HasDefaultValueSql("('0.00')")
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("INV_TAXABLE_AMOUNT");
            entity.Property(e => e.InvlTaxTotal)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("INVL_TAX_TOTAL");
            entity.Property(e => e.IsTaxApplicable)
                .HasDefaultValue(true)
                .HasColumnName("IS_TAX_APPLICABLE");
            entity.Property(e => e.ModifiedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("MODIFIED_BY");
            entity.Property(e => e.ModifiedOn)
                .HasPrecision(6)
                .HasColumnName("MODIFIED_ON");
            entity.Property(e => e.OldGoldAmount)
                .HasDefaultValueSql("('0.00')")
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("OLD_GOLD_AMOUNT");
            entity.Property(e => e.OldSilverAmount)
                .HasDefaultValueSql("('0.00')")
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("OLD_SILVER_AMOUNT");
            entity.Property(e => e.OrderDate)
                .HasPrecision(6)
                .HasColumnName("ORDER_DATE");
            entity.Property(e => e.OrderNbr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ORDER_NBR");
            entity.Property(e => e.PaymentDueDate)
                .HasPrecision(6)
                .HasColumnName("PAYMENT_DUE_DATE");
            entity.Property(e => e.PaymentMode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PAYMENT_MODE");
            entity.Property(e => e.RdAmountAdj)
                .HasDefaultValueSql("('0.00')")
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("RD_AMOUNT_ADJ");
            entity.Property(e => e.RecdAmount)
                .HasDefaultValueSql("('0.00')")
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("RECD_AMOUNT");
            entity.Property(e => e.RoundOff)
                .HasDefaultValueSql("('0.00')")
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("ROUND_OFF");
            entity.Property(e => e.SalesPerson)
                .HasMaxLength(50)
                .HasColumnName("SALES_PERSON");
            entity.Property(e => e.SgstAmount)
                .HasDefaultValueSql("('0.00')")
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("SGST_AMOUNT");
            entity.Property(e => e.SgstPercent)
                .HasDefaultValueSql("('0.00')")
                .HasColumnType("decimal(4, 2)")
                .HasColumnName("SGST_PERCENT");
            entity.Property(e => e.TaxType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("TAX_TYPE");
            entity.Property(e => e.TenantGkey).HasColumnName("TENANT_GKEY");
        });

        modelBuilder.Entity<InvoiceLine>(entity =>
        {
            entity.HasKey(e => e.Gkey);

            entity.ToTable("INVOICE_LINE");

            entity.Property(e => e.Gkey).HasColumnName("GKEY");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("CREATED_BY");
            entity.Property(e => e.CreatedOn)
                .HasPrecision(6)
                .HasColumnName("CREATED_ON");
            entity.Property(e => e.HsnCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("HSN_CODE");
            entity.Property(e => e.InvLineNbr).HasColumnName("INV_LINE_NBR");
            entity.Property(e => e.InvNote)
                .IsUnicode(false)
                .HasColumnName("INV_NOTE");
            entity.Property(e => e.InvlBilledPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("INVL_BILLED_PRICE");
            entity.Property(e => e.InvlCgstAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("INVL_CGST_AMOUNT");
            entity.Property(e => e.InvlCgstPercent)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("INVL_CGST_PERCENT");
            entity.Property(e => e.InvlGrossAmt)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("INVL_GROSS_AMT");
            entity.Property(e => e.InvlIgstAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("INVL_IGST_AMOUNT");
            entity.Property(e => e.InvlIgstPercent)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("INVL_IGST_PERCENT");
            entity.Property(e => e.InvlMakingCharges)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("INVL_MAKING_CHARGES");
            entity.Property(e => e.InvlOtherCharges)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("INVL_OTHER_CHARGES");
            entity.Property(e => e.InvlPayableAmt)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("INVL_PAYABLE_AMT");
            entity.Property(e => e.InvlSgstAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("INVL_SGST_AMOUNT");
            entity.Property(e => e.InvlSgstPercent)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("INVL_SGST_PERCENT");
            entity.Property(e => e.InvlStoneAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("INVL_STONE_AMOUNT");
            entity.Property(e => e.InvlTaxableAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("INVL_TAXABLE_AMOUNT");
            entity.Property(e => e.InvlTotal)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("INVL_TOTAL");
            entity.Property(e => e.InvlWastageAmt)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("INVL_WASTAGE_AMT");
            entity.Property(e => e.InvoiceHdrGkey).HasColumnName("INVOICE_HDR_GKEY");
            entity.Property(e => e.InvoiceId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("INVOICE_ID");
            entity.Property(e => e.IsTaxable).HasColumnName("IS_TAXABLE");
            entity.Property(e => e.ItemNotes)
                .IsUnicode(false)
                .HasColumnName("ITEM_NOTES");
            entity.Property(e => e.ItemPacked).HasColumnName("ITEM_PACKED");
            entity.Property(e => e.Metal)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("METAL");
            entity.Property(e => e.ModifiedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("MODIFIED_BY");
            entity.Property(e => e.ModifiedOn)
                .HasPrecision(6)
                .HasColumnName("MODIFIED_ON");
            entity.Property(e => e.ProdCategory)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("PROD_CATEGORY");
            entity.Property(e => e.ProdGrossWeight)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("PROD_GROSS_WEIGHT");
            entity.Property(e => e.ProdNetWeight)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("PROD_NET_WEIGHT");
            entity.Property(e => e.ProdPackCode)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("PROD_PACK_CODE");
            entity.Property(e => e.ProdQty).HasColumnName("PROD_QTY");
            entity.Property(e => e.ProdStoneWeight)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("PROD_STONE_WEIGHT");
            entity.Property(e => e.ProductDesc)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("PRODUCT_DESC");
            entity.Property(e => e.ProductGkey).HasColumnName("PRODUCT_GKEY");
            entity.Property(e => e.ProductId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PRODUCT_ID");
            entity.Property(e => e.ProductName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("PRODUCT_NAME");
            entity.Property(e => e.ProductPurity)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("PRODUCT_PURITY");
            entity.Property(e => e.ProductSku)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PRODUCT_SKU");
            entity.Property(e => e.TaxAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("TAX_AMOUNT");
            entity.Property(e => e.TaxPercent)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("TAX_PERCENT");
            entity.Property(e => e.TaxType)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("TAX_TYPE");
            entity.Property(e => e.TenantGkey).HasColumnName("TENANT_GKEY");
            entity.Property(e => e.VaAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("VA_AMOUNT");
            entity.Property(e => e.VaPercent)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("VA_PERCENT");
        });

        modelBuilder.Entity<LedgersHeader>(entity =>
        {
            entity.HasKey(e => e.Gkey);

            entity.ToTable("LEDGERS_HEADER");

            entity.Property(e => e.Gkey).HasColumnName("GKEY");
            entity.Property(e => e.BalanceAsOn).HasColumnName("BALANCE_AS_ON");
            entity.Property(e => e.CurrentBalance)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("CURRENT_BALANCE");
            entity.Property(e => e.CustGkey).HasColumnName("CUST_GKEY");
            entity.Property(e => e.MtblLedgersGkey).HasColumnName("MTBL_LEDGERS_GKEY");
        });

        modelBuilder.Entity<LedgersTransaction>(entity =>
        {
            entity.HasKey(e => e.Gkey);

            entity.ToTable("LEDGERS_TRANSACTIONS");

            entity.Property(e => e.Gkey).HasColumnName("GKEY");
            entity.Property(e => e.DocumentDate).HasColumnName("DOCUMENT_DATE");
            entity.Property(e => e.DocumentNbr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("DOCUMENT_NBR");
            entity.Property(e => e.DrCr)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("DR_CR");
            entity.Property(e => e.LedgerHdrGkey).HasColumnName("LEDGER_HDR_GKEY");
            entity.Property(e => e.Status).HasColumnName("STATUS");
            entity.Property(e => e.TransactionAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("TRANSACTION_AMOUNT");
            entity.Property(e => e.TransactionDate).HasColumnName("TRANSACTION_DATE");
        });

        modelBuilder.Entity<Metal>(entity =>
        {
            entity.HasKey(e => e.Gkey).HasName("METALS_PK");

            entity.ToTable("METALS");

            entity.Property(e => e.Gkey)
                .ValueGeneratedNever()
                .HasColumnName("GKEY");
            entity.Property(e => e.MetalName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("METAL_NAME");
        });

        modelBuilder.Entity<MtblLedger>(entity =>
        {
            entity.HasKey(e => e.Gkey);

            entity.ToTable("MTBL_LEDGERS");

            entity.Property(e => e.Gkey).HasColumnName("GKEY");
            entity.Property(e => e.AccountGroupName)
                .IsUnicode(false)
                .HasColumnName("ACCOUNT_GROUP_NAME");
            entity.Property(e => e.Description)
                .IsUnicode(false)
                .HasColumnName("DESCRIPTION");
            entity.Property(e => e.LedgerAccountCode).HasColumnName("LEDGER_ACCOUNT_CODE");
            entity.Property(e => e.LedgerName).HasColumnName("LEDGER_NAME");
            entity.Property(e => e.MemberGkey).HasColumnName("MEMBER_GKEY");
            entity.Property(e => e.MemberType).HasColumnName("MEMBER_TYPE");
            entity.Property(e => e.PrimaryGroup).HasColumnName("PRIMARY_GROUP");
            entity.Property(e => e.Status).HasColumnName("STATUS");
            entity.Property(e => e.SubGroup).HasColumnName("SUB_GROUP");
            entity.Property(e => e.TenantGkey).HasColumnName("TENANT_GKEY");
        });

        modelBuilder.Entity<MtblReference>(entity =>
        {
            entity.HasKey(e => e.Gkey).HasName("PK_mtbl_references");

            entity.ToTable("MTBL_REFERENCES");

            entity.Property(e => e.Gkey).HasColumnName("gkey");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.Module)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("module");
            entity.Property(e => e.RefCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ref_code");
            entity.Property(e => e.RefDesc)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("ref_desc");
            entity.Property(e => e.RefName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ref_name");
            entity.Property(e => e.RefValue)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ref_value");
            entity.Property(e => e.SortSeq).HasColumnName("sort_seq");
        });

        modelBuilder.Entity<MtblVoucherType>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("MTBL_VOUCHER_TYPES");

            entity.Property(e => e.Gkey)
                .ValueGeneratedOnAdd()
                .HasColumnName("gkey");
            entity.Property(e => e.VoucherType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("voucher_type");
        });

        modelBuilder.Entity<OldMetalTransaction>(entity =>
        {
            entity.HasKey(e => e.Gkey).HasName("PK_old_metal_transaction");

            entity.ToTable("OLD_METAL_TRANSACTION");

            entity.Property(e => e.Gkey).HasColumnName("gkey");
            entity.Property(e => e.CustGkey).HasColumnName("cust_gkey");
            entity.Property(e => e.CustMobile)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("cust_mobile");
            entity.Property(e => e.DocRefDate).HasColumnName("doc_ref_date");
            entity.Property(e => e.DocRefGkey).HasColumnName("doc_ref_gkey");
            entity.Property(e => e.DocRefNbr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("doc_ref_nbr");
            entity.Property(e => e.FinalPurchasePrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("final_purchase_price");
            entity.Property(e => e.GrossWeight)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("gross_weight");
            entity.Property(e => e.Metal)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("metal");
            entity.Property(e => e.NetWeight)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("net_weight");
            entity.Property(e => e.ProductCategory)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("product_category");
            entity.Property(e => e.ProductGkey).HasColumnName("product_gkey");
            entity.Property(e => e.ProductId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("product_id");
            entity.Property(e => e.Purity)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("purity");
            entity.Property(e => e.Remarks)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("remarks");
            entity.Property(e => e.StoneWeight)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("stone_weight");
            entity.Property(e => e.TotalProposedPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("total_proposed_price");
            entity.Property(e => e.TransDate).HasColumnName("trans_date");
            entity.Property(e => e.TransNbr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("trans_nbr");
            entity.Property(e => e.TransType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("trans_type");
            entity.Property(e => e.TransactedRate)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("transacted_rate");
            entity.Property(e => e.Uom)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("uom");
            entity.Property(e => e.WastagePercent)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("wastage_percent");
            entity.Property(e => e.WastageWeight)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("wastage_weight");
        });

        modelBuilder.Entity<OpeningStock>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("OPENING_STOCK");

            entity.Property(e => e.OpeningGrossWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("OPENING_GROSS_WEIGHT");
            entity.Property(e => e.ProductCategory)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("product_category");
            entity.Property(e => e.TransactionDate).HasColumnName("transaction_date");
        });

        modelBuilder.Entity<OrgAddress>(entity =>
        {
            entity.HasKey(e => e.Gkey);

            entity.ToTable("ORG_ADDRESS");

            entity.Property(e => e.Gkey).HasColumnName("GKEY");
            entity.Property(e => e.AddressLine1)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("ADDRESS_LINE1");
            entity.Property(e => e.AddressLine2)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("ADDRESS_LINE2");
            entity.Property(e => e.AddressLine3)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("ADDRESS_LINE3");
            entity.Property(e => e.Area)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("AREA");
            entity.Property(e => e.City)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("CITY");
            entity.Property(e => e.Country)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("COUNTRY");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("CREATED_BY");
            entity.Property(e => e.CreatedOn)
                .HasPrecision(6)
                .HasColumnName("CREATED_ON");
            entity.Property(e => e.District)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("DISTRICT");
            entity.Property(e => e.GstStateCode)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("GST_STATE_CODE");
            entity.Property(e => e.ModifiedBy)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("MODIFIED_BY");
            entity.Property(e => e.ModifiedOn)
                .HasPrecision(6)
                .HasColumnName("MODIFIED_ON");
            entity.Property(e => e.Pincode).HasColumnName("PINCODE");
            entity.Property(e => e.State)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("STATE");
            entity.Property(e => e.TenantGkey).HasColumnName("TENANT_GKEY");
        });

        modelBuilder.Entity<OrgBankDetail>(entity =>
        {
            entity.HasKey(e => e.Gkey).HasName("BANK_DETAILS_PK");

            entity.ToTable("ORG_BANK_DETAILS");

            entity.Property(e => e.Gkey)
                .ValueGeneratedNever()
                .HasColumnName("GKEY");
            entity.Property(e => e.BankAccountNbr)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("BANK_ACCOUNT_NBR");
            entity.Property(e => e.BankCustRefNbr)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("BANK_CUST_REF_NBR");
            entity.Property(e => e.BankName)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("BANK_NAME");
            entity.Property(e => e.BranchName)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("BRANCH_NAME");
            entity.Property(e => e.IfscCode)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("IFSC_CODE");
            entity.Property(e => e.IsPrimary)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("IS_PRIMARY");
            entity.Property(e => e.LocationGkey).HasColumnName("LOCATION_GKEY");
            entity.Property(e => e.MobileEnabled)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("MOBILE_ENABLED");
            entity.Property(e => e.OnlineEnabled)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasComment("True/  False")
                .HasColumnName("ONLINE_ENABLED");
            entity.Property(e => e.UseByRefGkey).HasColumnName("USE_BY_REF_GKEY");
            entity.Property(e => e.UseByRefName)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("USE_BY_REF_NAME");
        });

        modelBuilder.Entity<OrgCompany>(entity =>
        {
            entity.HasKey(e => e.Gkey).HasName("ORG_COMPANY_PK");

            entity.ToTable("ORG_COMPANY");

            entity.Property(e => e.Gkey)
                .ValueGeneratedNever()
                .HasColumnName("GKEY");
            entity.Property(e => e.AccountId)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("ACCOUNT_ID");
            entity.Property(e => e.AddressGkey).HasColumnName("ADDRESS_GKEY");
            entity.Property(e => e.CinNbr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("CIN_NBR");
            entity.Property(e => e.ContactNbr1)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("CONTACT_NBR1");
            entity.Property(e => e.ContactNbr2)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("CONTACT_NBR2");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("CREATED_BY");
            entity.Property(e => e.CreatedOn)
                .HasPrecision(6)
                .HasColumnName("CREATED_ON");
            entity.Property(e => e.DeleteFlag).HasColumnName("DELETE_FLAG");
            entity.Property(e => e.DraftId).HasColumnName("DRAFT_ID");
            entity.Property(e => e.EmailId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("EMAIL_ID");
            entity.Property(e => e.GstNbr)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("GST_NBR");
            entity.Property(e => e.InvId).HasColumnName("INV_ID");
            entity.Property(e => e.ModifiedBy)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("MODIFIED_BY");
            entity.Property(e => e.ModifiedOn)
                .HasPrecision(6)
                .HasColumnName("MODIFIED_ON");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("NAME");
            entity.Property(e => e.Notes)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("NOTES");
            entity.Property(e => e.PanNbr)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("PAN_NBR");
            entity.Property(e => e.ServiceTaxNbr)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("SERVICE_TAX_NBR");
            entity.Property(e => e.Status)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("STATUS");
            entity.Property(e => e.Tagline)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("TAGLINE");
            entity.Property(e => e.TenantGkey).HasColumnName("TENANT_GKEY");
            entity.Property(e => e.ThisCompany).HasColumnName("THIS_COMPANY");
            entity.Property(e => e.TinNbr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("TIN_NBR");
        });

        modelBuilder.Entity<OrgContact>(entity =>
        {
            entity.HasKey(e => e.Gkey).HasName("MOBILE_PK");

            entity.ToTable("ORG_CONTACT");

            entity.Property(e => e.Gkey)
                .ValueGeneratedNever()
                .HasColumnName("GKEY");
            entity.Property(e => e.CustRefGkey).HasColumnName("CUST_REF_GKEY");
            entity.Property(e => e.MobileNbr)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("MOBILE_NBR");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("STATUS");
        });

        modelBuilder.Entity<OrgContactEmail>(entity =>
        {
            entity.HasKey(e => e.Gkey).HasName("EMAIL_PK");

            entity.ToTable("ORG_CONTACT_EMAIL");

            entity.Property(e => e.Gkey)
                .ValueGeneratedNever()
                .HasColumnName("GKEY");
            entity.Property(e => e.ActiveFlag)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("ACTIVE_FLAG");
            entity.Property(e => e.CustRefGkey).HasColumnName("CUST_REF_GKEY");
            entity.Property(e => e.EmailId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("EMAIL_ID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("STATUS");
        });

        modelBuilder.Entity<OrgCustomer>(entity =>
        {
            entity.HasKey(e => e.Gkey).HasName("ORG_CUSTOMER_PK");

            entity.ToTable("ORG_CUSTOMER");

            entity.Property(e => e.Gkey).HasColumnName("GKEY");
            entity.Property(e => e.AddressGkey).HasColumnName("ADDRESS_GKEY");
            entity.Property(e => e.ClientId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("CLIENT_ID");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("CREATED_BY");
            entity.Property(e => e.CreatedOn)
                .HasPrecision(6)
                .HasColumnName("CREATED_ON");
            entity.Property(e => e.CreditAvailed)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("CREDIT_AVAILED");
            entity.Property(e => e.CustomerName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("CUSTOMER_NAME");
            entity.Property(e => e.CustomerSince)
                .HasColumnType("datetime")
                .HasColumnName("CUSTOMER_SINCE");
            entity.Property(e => e.CustomerType)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("CUSTOMER_TYPE");
            entity.Property(e => e.DeleteFlag).HasColumnName("DELETE_FLAG");
            entity.Property(e => e.GstStateCode)
                .HasMaxLength(5)
                .HasColumnName("GST_STATE_CODE");
            entity.Property(e => e.GstinNbr)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("GSTIN_NBR");
            entity.Property(e => e.LedgerName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("LEDGER_NAME");
            entity.Property(e => e.LocationGkey).HasColumnName("LOCATION_GKEY");
            entity.Property(e => e.MobileNbr)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("MOBILE_NBR");
            entity.Property(e => e.ModifiedBy)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("MODIFIED_BY");
            entity.Property(e => e.ModifiedOn)
                .HasPrecision(6)
                .HasColumnName("MODIFIED_ON");
            entity.Property(e => e.Notes)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("NOTES");
            entity.Property(e => e.PanNbr)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PAN_NBR");
            entity.Property(e => e.Salutations)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("SALUTATIONS");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("STATUS");
            entity.Property(e => e.TenantGkey).HasColumnName("TENANT_GKEY");
        });

        modelBuilder.Entity<OrgCustomerAddressView>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("ORG_CUSTOMER_ADDRESS_VIEW");

            entity.Property(e => e.AddLine1)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("ADD_LINE1");
            entity.Property(e => e.AddLine2)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("ADD_LINE2");
            entity.Property(e => e.AddLine3)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("ADD_LINE3");
            entity.Property(e => e.AddressGkey).HasColumnName("ADDRESS_GKEY");
            entity.Property(e => e.Area)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("AREA");
            entity.Property(e => e.City)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("CITY");
            entity.Property(e => e.ClientId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("CLIENT_ID");
            entity.Property(e => e.Country)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("COUNTRY");
            entity.Property(e => e.CreditAvailed)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("CREDIT_AVAILED");
            entity.Property(e => e.CustGkey).HasColumnName("CUST_GKEY");
            entity.Property(e => e.CustGstCode)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("CUST_GST_CODE");
            entity.Property(e => e.CustName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("CUST_NAME");
            entity.Property(e => e.CustSince)
                .HasColumnType("datetime")
                .HasColumnName("CUST_SINCE");
            entity.Property(e => e.CustStatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("CUST_STATUS");
            entity.Property(e => e.CustType)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("CUST_TYPE");
            entity.Property(e => e.District)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("DISTRICT");
            entity.Property(e => e.GstinNbr)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("GSTIN_NBR");
            entity.Property(e => e.LocationGkey).HasColumnName("LOCATION_GKEY");
            entity.Property(e => e.MobileNbr)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("MOBILE_NBR");
            entity.Property(e => e.PanNbr)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PAN_NBR");
            entity.Property(e => e.Salutations)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("SALUTATIONS");
            entity.Property(e => e.State)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("STATE");
        });

        modelBuilder.Entity<OrgGeoLocation>(entity =>
        {
            entity.HasKey(e => e.Gkey).HasName("PK__ORG_GEO___5F3369A044869AB7");

            entity.ToTable("ORG_GEO_LOCATIONS");

            entity.Property(e => e.Gkey)
                .ValueGeneratedNever()
                .HasColumnName("GKEY");
            entity.Property(e => e.ExternalId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("EXTERNAL_ID");
            entity.Property(e => e.LocationType)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("LOCATION_TYPE");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("NAME");
            entity.Property(e => e.ParentId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("PARENT_ID");
            entity.Property(e => e.Pincode)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("PINCODE");
        });

        modelBuilder.Entity<OrgPlace>(entity =>
        {
            entity.HasKey(e => e.Gkey);

            entity.ToTable("ORG_PLACES");

            entity.Property(e => e.Gkey).HasColumnName("gkey");
            entity.Property(e => e.DistrictName)
                .HasMaxLength(100)
                .HasColumnName("district_name");
            entity.Property(e => e.LocalityVillageName)
                .HasMaxLength(100)
                .HasColumnName("locality_village_name");
            entity.Property(e => e.Pincode).HasColumnName("pincode");
            entity.Property(e => e.PostOfficeName)
                .HasMaxLength(100)
                .HasColumnName("post_office_name");
            entity.Property(e => e.StateName)
                .HasMaxLength(100)
                .HasColumnName("state_name");
            entity.Property(e => e.SubDistrictName)
                .HasMaxLength(100)
                .HasColumnName("sub_district_name");
        });

        modelBuilder.Entity<OrgThisCompanyView>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("ORG_THIS_COMPANY_VIEW");

            entity.Property(e => e.AddressLine1)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("ADDRESS_LINE1");
            entity.Property(e => e.AddressLine2)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("ADDRESS_LINE2");
            entity.Property(e => e.AddressLine3)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("ADDRESS_LINE3");
            entity.Property(e => e.Area)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("AREA");
            entity.Property(e => e.City)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("CITY");
            entity.Property(e => e.CompanyName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("company_name");
            entity.Property(e => e.ContactNbr1)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("CONTACT_NBR1");
            entity.Property(e => e.ContactNbr2)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("CONTACT_NBR2");
            entity.Property(e => e.Country)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("COUNTRY");
            entity.Property(e => e.District)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("DISTRICT");
            entity.Property(e => e.EmailId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("EMAIL_ID");
            entity.Property(e => e.GstCode)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("gst_code");
            entity.Property(e => e.GstNbr)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("GST_NBR");
            entity.Property(e => e.PanNbr)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("PAN_NBR");
            entity.Property(e => e.Pincode).HasColumnName("PINCODE");
            entity.Property(e => e.ServiceTaxNbr)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("SERVICE_TAX_NBR");
            entity.Property(e => e.State)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("STATE");
            entity.Property(e => e.Tagline)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("TAGLINE");
            entity.Property(e => e.TenantGkey).HasColumnName("TENANT_GKEY");
            entity.Property(e => e.ThisCompany).HasColumnName("THIS_COMPANY");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Gkey);

            entity.ToTable("PRODUCT");

            entity.Property(e => e.Gkey).HasColumnName("GKEY");
            entity.Property(e => e.BaseUnit)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("BASE_UNIT");
            entity.Property(e => e.Brand)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("BRAND");
            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("CATEGORY");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("CREATED_BY");
            entity.Property(e => e.CreatedOn)
                .HasPrecision(6)
                .HasColumnName("CREATED_ON");
            entity.Property(e => e.Description)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("DESCRIPTION");
            entity.Property(e => e.HsnCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("HSN_CODE");
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ID");
            entity.Property(e => e.ImageRef)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("IMAGE_REF");
            entity.Property(e => e.IsActive).HasColumnName("IS_ACTIVE");
            entity.Property(e => e.IsTaxable).HasColumnName("IS_TAXABLE");
            entity.Property(e => e.Metal)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("METAL");
            entity.Property(e => e.Model)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("MODEL");
            entity.Property(e => e.ModifiedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("MODIFIED_BY");
            entity.Property(e => e.ModifiedOn)
                .HasPrecision(6)
                .HasColumnName("MODIFIED_ON");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("NAME");
            entity.Property(e => e.ProductAttributeGkey).HasColumnName("PRODUCT_ATTRIBUTE_GKEY");
            entity.Property(e => e.Purity)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PURITY");
            entity.Property(e => e.PurityPercent)
                .HasColumnType("decimal(4, 2)")
                .HasColumnName("PURITY_PERCENT");
            entity.Property(e => e.SetGkey).HasColumnName("SET_GKEY");
            entity.Property(e => e.StockGroup)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("STOCK_GROUP");
            entity.Property(e => e.TaxRule)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("TAX_RULE");
            entity.Property(e => e.Uom)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("UOM");
        });

        modelBuilder.Entity<ProductCategory>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PRODUCT_CATEGORY");

            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("NAME");
            entity.Property(e => e.Sn).HasColumnName("SN");
        });

        modelBuilder.Entity<ProductGroup>(entity =>
        {
            entity.HasKey(e => e.Gkey).HasName("PRODUCT_GROUP_PK");

            entity.ToTable("PRODUCT_GROUP");

            entity.Property(e => e.Gkey)
                .ValueGeneratedNever()
                .HasColumnName("GKEY");
            entity.Property(e => e.Category)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("CATEGORY");
            entity.Property(e => e.GroupName)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("GROUP_NAME");
            entity.Property(e => e.Purity)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("PURITY");
        });

        modelBuilder.Entity<ProductStock>(entity =>
        {
            entity.HasKey(e => e.Gkey);

            entity.ToTable("PRODUCT_STOCK");

            entity.Property(e => e.Gkey).HasColumnName("GKEY");
            entity.Property(e => e.AdjustedWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("ADJUSTED_WEIGHT");
            entity.Property(e => e.BalanceWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("BALANCE_WEIGHT");
            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("CATEGORY");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("CREATED_BY");
            entity.Property(e => e.CreatedOn)
                .HasPrecision(6)
                .HasColumnName("CREATED_ON");
            entity.Property(e => e.GrossWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("GROSS_WEIGHT");
            entity.Property(e => e.IsBarcodePrinted).HasColumnName("IS_BARCODE_PRINTED");
            entity.Property(e => e.IsProductSold).HasColumnName("IS_PRODUCT_SOLD");
            entity.Property(e => e.ModifiedBy)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("MODIFIED_BY");
            entity.Property(e => e.ModifiedOn)
                .HasPrecision(6)
                .HasColumnName("MODIFIED_ON");
            entity.Property(e => e.NetWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("NET_WEIGHT");
            entity.Property(e => e.ProductGkey).HasColumnName("PRODUCT_GKEY");
            entity.Property(e => e.ProductSku)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PRODUCT_SKU");
            entity.Property(e => e.Size)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("SIZE");
            entity.Property(e => e.SizeId).HasColumnName("SIZE_ID");
            entity.Property(e => e.SizeUom)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("SIZE_UOM");
            entity.Property(e => e.SoldQty).HasColumnName("SOLD_QTY");
            entity.Property(e => e.SoldWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("SOLD_WEIGHT");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("STATUS");
            entity.Property(e => e.StockQty).HasColumnName("STOCK_QTY");
            entity.Property(e => e.StockSummaryGkey).HasColumnName("STOCK_SUMMARY_GKEY");
            entity.Property(e => e.StoneWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("STONE_WEIGHT");
            entity.Property(e => e.SuppliedGrossWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("SUPPLIED_GROSS_WEIGHT");
            entity.Property(e => e.SuppliedQty).HasColumnName("SUPPLIED_QTY");
            entity.Property(e => e.SupplierId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("SUPPLIER_ID");
            entity.Property(e => e.VaPercent)
                .HasColumnType("decimal(4, 2)")
                .HasColumnName("VA_PERCENT");
            entity.Property(e => e.WastageAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("WASTAGE_AMOUNT");
            entity.Property(e => e.WastagePercent)
                .HasColumnType("decimal(4, 2)")
                .HasColumnName("WASTAGE_PERCENT");
        });

        modelBuilder.Entity<ProductStockSummary>(entity =>
        {
            entity.HasKey(e => e.Gkey);

            entity.ToTable("PRODUCT_STOCK_SUMMARY");

            entity.Property(e => e.Gkey).HasColumnName("GKEY");
            entity.Property(e => e.AdjustedQty).HasColumnName("ADJUSTED_QTY");
            entity.Property(e => e.AdjustedWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("ADJUSTED_WEIGHT");
            entity.Property(e => e.BalanceWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("BALANCE_WEIGHT");
            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("CATEGORY");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("CREATED_BY");
            entity.Property(e => e.CreatedOn)
                .HasPrecision(6)
                .HasColumnName("CREATED_ON");
            entity.Property(e => e.GrossWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("GROSS_WEIGHT");
            entity.Property(e => e.ModifiedBy)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("MODIFIED_BY");
            entity.Property(e => e.ModifiedOn)
                .HasPrecision(6)
                .HasColumnName("MODIFIED_ON");
            entity.Property(e => e.NetWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("NET_WEIGHT");
            entity.Property(e => e.ProductGkey).HasColumnName("PRODUCT_GKEY");
            entity.Property(e => e.ProductSku)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PRODUCT_SKU");
            entity.Property(e => e.SoldQty).HasColumnName("SOLD_QTY");
            entity.Property(e => e.SoldWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("SOLD_WEIGHT");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("STATUS");
            entity.Property(e => e.StockQty).HasColumnName("STOCK_QTY");
            entity.Property(e => e.StoneWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("STONE_WEIGHT");
            entity.Property(e => e.SuppliedGrossWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("SUPPLIED_GROSS_WEIGHT");
            entity.Property(e => e.SuppliedQty).HasColumnName("SUPPLIED_QTY");
            entity.Property(e => e.Uom)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("UOM");
            entity.Property(e => e.VaPercent)
                .HasColumnType("decimal(4, 2)")
                .HasColumnName("VA_PERCENT");
            entity.Property(e => e.WastageAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("WASTAGE_AMOUNT");
            entity.Property(e => e.WastagePercent)
                .HasColumnType("decimal(4, 2)")
                .HasColumnName("WASTAGE_PERCENT");
        });

        modelBuilder.Entity<ProductTransaction>(entity =>
        {
            entity.HasKey(e => e.Gkey);

            entity.ToTable("PRODUCT_TRANSACTION");

            entity.Property(e => e.Gkey).HasColumnName("GKEY");
            entity.Property(e => e.CbQty).HasColumnName("CB_QTY");
            entity.Property(e => e.ClosingGrossWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("CLOSING_GROSS_WEIGHT");
            entity.Property(e => e.ClosingNetWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("CLOSING_NET_WEIGHT");
            entity.Property(e => e.ClosingStoneWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("CLOSING_STONE_WEIGHT");
            entity.Property(e => e.DocumentDate).HasColumnName("DOCUMENT_DATE");
            entity.Property(e => e.DocumentNbr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("DOCUMENT_NBR");
            entity.Property(e => e.DocumentType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("DOCUMENT_TYPE");
            entity.Property(e => e.Notes)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("NOTES");
            entity.Property(e => e.ObQty).HasColumnName("OB_QTY");
            entity.Property(e => e.OpeningGrossWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("OPENING_GROSS_WEIGHT");
            entity.Property(e => e.OpeningNetWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("OPENING_NET_WEIGHT");
            entity.Property(e => e.OpeningStoneWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("OPENING_STONE_WEIGHT");
            entity.Property(e => e.ProductCategory)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PRODUCT_CATEGORY");
            entity.Property(e => e.ProductSku)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PRODUCT_SKU");
            entity.Property(e => e.RefGkey).HasColumnName("REF_GKEY");
            entity.Property(e => e.TransactionDate).HasColumnName("TRANSACTION_DATE");
            entity.Property(e => e.TransactionGrossWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("TRANSACTION_GROSS_WEIGHT");
            entity.Property(e => e.TransactionNetWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("TRANSACTION_NET_WEIGHT");
            entity.Property(e => e.TransactionQty).HasColumnName("TRANSACTION_QTY");
            entity.Property(e => e.TransactionStoneWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("TRANSACTION_STONE_WEIGHT");
            entity.Property(e => e.TransactionType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("TRANSACTION_TYPE");
            entity.Property(e => e.TransactionValue)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("TRANSACTION_VALUE");
            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("UNIT_PRICE");
            entity.Property(e => e.VoucherType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("VOUCHER_TYPE");
        });

        modelBuilder.Entity<ProductTransactionSummary>(entity =>
        {
            entity.HasKey(e => e.Gkey);

            entity.ToTable("PRODUCT_TRANSACTION_SUMMARY");

            entity.Property(e => e.Gkey).HasColumnName("GKEY");
            entity.Property(e => e.ClosingGrossWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("CLOSING_GROSS_WEIGHT");
            entity.Property(e => e.ClosingNetWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("CLOSING_NET_WEIGHT");
            entity.Property(e => e.ClosingQty).HasColumnName("CLOSING_QTY");
            entity.Property(e => e.ClosingStoneWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("CLOSING_STONE_WEIGHT");
            entity.Property(e => e.OpeningGrossWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("OPENING_GROSS_WEIGHT");
            entity.Property(e => e.OpeningNetWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("OPENING_NET_WEIGHT");
            entity.Property(e => e.OpeningQty).HasColumnName("OPENING_QTY");
            entity.Property(e => e.OpeningStoneWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("OPENING_STONE_WEIGHT");
            entity.Property(e => e.ProductCategory)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PRODUCT_CATEGORY");
            entity.Property(e => e.ProductSku)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PRODUCT_SKU");
            entity.Property(e => e.StockInGrossWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("STOCK_IN_GROSS_WEIGHT");
            entity.Property(e => e.StockInNetWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("STOCK_IN_NET_WEIGHT");
            entity.Property(e => e.StockInQty).HasColumnName("STOCK_IN_QTY");
            entity.Property(e => e.StockInStoneWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("STOCK_IN_STONE_WEIGHT");
            entity.Property(e => e.StockOutGrossWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("STOCK_OUT_GROSS_WEIGHT");
            entity.Property(e => e.StockOutNetWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("STOCK_OUT_NET_WEIGHT");
            entity.Property(e => e.StockOutQty).HasColumnName("STOCK_OUT_QTY");
            entity.Property(e => e.StockOutStoneWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("STOCK_OUT_STONE_WEIGHT");
            entity.Property(e => e.TransactionDate).HasColumnName("TRANSACTION_DATE");
        });

        modelBuilder.Entity<ProductView>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("PRODUCT_VIEW");

            entity.Property(e => e.BalanceWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("BALANCE_WEIGHT");
            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("CATEGORY");
            entity.Property(e => e.Description)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("DESCRIPTION");
            entity.Property(e => e.Gkey).HasColumnName("gkey");
            entity.Property(e => e.GrossWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("GROSS_WEIGHT");
            entity.Property(e => e.HsnCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("HSN_CODE");
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ID");
            entity.Property(e => e.IsTaxable).HasColumnName("IS_TAXABLE");
            entity.Property(e => e.Metal)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("METAL");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("NAME");
            entity.Property(e => e.NetWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("NET_WEIGHT");
            entity.Property(e => e.ProductSku)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PRODUCT_SKU");
            entity.Property(e => e.Purity)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PURITY");
            entity.Property(e => e.SoldQty).HasColumnName("SOLD_QTY");
            entity.Property(e => e.SoldWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("SOLD_WEIGHT");
            entity.Property(e => e.StockQty).HasColumnName("STOCK_QTY");
            entity.Property(e => e.StoneWeight)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("STONE_WEIGHT");
            entity.Property(e => e.Uom)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("UOM");
            entity.Property(e => e.VaPercent)
                .HasColumnType("decimal(4, 2)")
                .HasColumnName("VA_PERCENT");
            entity.Property(e => e.WastageAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("WASTAGE_AMOUNT");
            entity.Property(e => e.WastagePercent)
                .HasColumnType("decimal(4, 2)")
                .HasColumnName("WASTAGE_PERCENT");
        });

        modelBuilder.Entity<RepDailyStockSummary>(entity =>
        {
            entity.HasKey(e => e.Gkey);

            entity.ToTable("REP_DAILY_STOCK_SUMMARY");

            entity.Property(e => e.Gkey).HasColumnName("GKEY");
            entity.Property(e => e.ClosingStock)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("CLOSING_STOCK");
            entity.Property(e => e.ClosingStockQty).HasColumnName("CLOSING_STOCK_QTY");
            entity.Property(e => e.Metal)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("METAL");
            entity.Property(e => e.OpeningStock)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("OPENING_STOCK");
            entity.Property(e => e.OpeningStockQty).HasColumnName("OPENING_STOCK_QTY");
            entity.Property(e => e.ProductCategory)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PRODUCT_CATEGORY");
            entity.Property(e => e.StockIn)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("STOCK_IN");
            entity.Property(e => e.StockInQty).HasColumnName("STOCK_IN_QTY");
            entity.Property(e => e.StockOut)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("STOCK_OUT");
            entity.Property(e => e.StockOutQty).HasColumnName("STOCK_OUT_QTY");
            entity.Property(e => e.StockTransferIn)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("STOCK_TRANSFER_IN");
            entity.Property(e => e.StockTransferInQty).HasColumnName("STOCK_TRANSFER_IN_QTY");
            entity.Property(e => e.StockTransferOut)
                .HasColumnType("decimal(18, 3)")
                .HasColumnName("STOCK_TRANSFER_OUT");
            entity.Property(e => e.StockTrnsferOutQty).HasColumnName("STOCK_TRNSFER_OUT_QTY");
            entity.Property(e => e.TransactionDate).HasColumnName("TRANSACTION_DATE");
        });

        modelBuilder.Entity<Test>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("test");

            entity.Property(e => e.OpeningQtyFirstTransaction)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("opening_qty_first_transaction");
            entity.Property(e => e.ProductCategory)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PRODUCT_CATEGORY");
            entity.Property(e => e.StockIn).HasColumnType("decimal(38, 3)");
            entity.Property(e => e.StockOut).HasColumnType("decimal(38, 3)");
            entity.Property(e => e.TotalTransactedQty)
                .HasColumnType("decimal(38, 3)")
                .HasColumnName("total_transacted_qty");
            entity.Property(e => e.TransactionDate).HasColumnName("TRANSACTION_DATE");
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.HasKey(e => e.Gkey).HasName("PK_fin_day_book");

            entity.ToTable("VOUCHER");

            entity.Property(e => e.Gkey).HasColumnName("gkey");
            entity.Property(e => e.CbAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("cb_amount");
            entity.Property(e => e.CustomerGkey).HasColumnName("customer_gkey");
            entity.Property(e => e.FromLedgerGkey).HasColumnName("from_ledger_gkey");
            entity.Property(e => e.FundTransferDate).HasColumnName("fund_transfer_date");
            entity.Property(e => e.FundTransferMode).HasColumnName("fund_transfer_mode");
            entity.Property(e => e.FundTransferRefGkey).HasColumnName("fund_transfer_ref_gkey");
            entity.Property(e => e.Mode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("mode");
            entity.Property(e => e.ObAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ob_amount");
            entity.Property(e => e.RefDocDate).HasColumnName("ref_doc_date");
            entity.Property(e => e.RefDocGkey).HasColumnName("ref_doc_gkey");
            entity.Property(e => e.RefDocNbr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ref_doc_nbr");
            entity.Property(e => e.SeqNbr).HasColumnName("seq_nbr");
            entity.Property(e => e.ToLedgerGkey).HasColumnName("to_ledger_gkey");
            entity.Property(e => e.TransAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("trans_amount");
            entity.Property(e => e.TransDate).HasColumnName("trans_date");
            entity.Property(e => e.TransDesc)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("trans_desc");
            entity.Property(e => e.TransType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("trans_type");
            entity.Property(e => e.VoucherDate).HasColumnName("voucher_date");
            entity.Property(e => e.VoucherNbr)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("voucher_nbr");
            entity.Property(e => e.VoucherType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("voucher_type");
        });

        modelBuilder.Entity<VoucherType>(entity =>
        {
            entity.HasKey(e => e.Gkey);

            entity.ToTable("VOUCHER_TYPES");

            entity.Property(e => e.Gkey)
                .ValueGeneratedNever()
                .HasColumnName("GKEY");
            entity.Property(e => e.Abbreviation)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ABBREVIATION");
            entity.Property(e => e.DocNbrLength).HasColumnName("DOC_NBR_LENGTH");
            entity.Property(e => e.DocNbrMethod)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("DOC_NBR_METHOD");
            entity.Property(e => e.DocNbrPrefill)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("DOC_NBR_PREFILL");
            entity.Property(e => e.DocNbrPrefix)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("DOC_NBR_PREFIX");
            entity.Property(e => e.DocNbrSuffix)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("DOC_NBR_SUFFIX");
            entity.Property(e => e.DocumentType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("DOCUMENT_TYPE");
            entity.Property(e => e.IsActive).HasColumnName("IS_ACTIVE");
            entity.Property(e => e.IsTaxable).HasColumnName("IS_TAXABLE");
            entity.Property(e => e.LastUsedNumber).HasColumnName("LAST_USED_NUMBER");
            entity.Property(e => e.MtblVoucherTypeGkey).HasColumnName("MTBL_VOUCHER_TYPE_GKEY");
            entity.Property(e => e.Narration)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("NARRATION");
            entity.Property(e => e.UsedFor)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("USED_FOR");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
