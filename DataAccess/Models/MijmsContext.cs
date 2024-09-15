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

    public virtual DbSet<InvoiceHeader> InvoiceHeaders { get; set; }

    public virtual DbSet<InvoiceLine> InvoiceLines { get; set; }

    public virtual DbSet<Metal> Metals { get; set; }

    public virtual DbSet<OrgAddress> OrgAddresses { get; set; }

    public virtual DbSet<OrgBankDetail> OrgBankDetails { get; set; }

    public virtual DbSet<OrgCompany> OrgCompanies { get; set; }

    public virtual DbSet<OrgContact> OrgContacts { get; set; }

    public virtual DbSet<OrgContactEmail> OrgContactEmails { get; set; }

    public virtual DbSet<OrgCustomer> OrgCustomers { get; set; }

    public virtual DbSet<OrgGeoLocation> OrgGeoLocations { get; set; }

    public virtual DbSet<OrgThisCompanyView> OrgThisCompanyViews { get; set; }

    public virtual DbSet<ProductGroup> ProductGroups { get; set; }

    public virtual DbSet<ProductStock> ProductStocks { get; set; }

    public virtual DbSet<ProductTransaction> ProductTransactions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=YOGESH-PC-INFIN\\SQLEXPRESS;Initial Catalog=mijms;TrustServerCertificate=True;Trusted_Connection=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("CREATED_BY");
            entity.Property(e => e.CreatedOn)
                .HasPrecision(6)
                .HasColumnName("CREATED_ON");
            entity.Property(e => e.CustGkey)
                .HasColumnType("decimal(19, 0)")
                .HasColumnName("CUST_GKEY");
            entity.Property(e => e.CustMobile)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("CUST_MOBILE");
            entity.Property(e => e.DeliveryMethod)
                .HasMaxLength(20)
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
            entity.Property(e => e.GstLocSeller)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("GST_LOC_SELLER");
            entity.Property(e => e.GstLocSupplier)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("GST_LOC_SUPPLIER");
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
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("INV_NBR");
            entity.Property(e => e.InvNotes)
                .HasMaxLength(255)
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
            entity.Property(e => e.IsTaxApplicable).HasColumnName("IS_TAX_APPLICABLE");
            entity.Property(e => e.ModifiedBy)
                .HasMaxLength(30)
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
                .HasColumnType("datetime")
                .HasColumnName("ORDER_DATE");
            entity.Property(e => e.OrderNbr)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("ORDER_NBR");
            entity.Property(e => e.PaymentDueDate)
                .HasPrecision(6)
                .HasColumnName("PAYMENT_DUE_DATE");
            entity.Property(e => e.PaymentMode)
                .HasMaxLength(25)
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
            entity.Property(e => e.SgstAmount)
                .HasDefaultValueSql("('0.00')")
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("SGST_AMOUNT");
            entity.Property(e => e.SgstPercent)
                .HasDefaultValueSql("('0.00')")
                .HasColumnType("decimal(4, 2)")
                .HasColumnName("SGST_PERCENT");
            entity.Property(e => e.TaxType)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("TAX_TYPE");
            entity.Property(e => e.TenantGkey)
                .HasMaxLength(50)
                .HasColumnName("TENANT_GKEY");
        });

        modelBuilder.Entity<InvoiceLine>(entity =>
        {
            entity.HasKey(e => e.Gkey);

            entity.ToTable("INVOICE_LINE");

            entity.Property(e => e.Gkey).HasColumnName("GKEY");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .HasColumnName("CREATED_BY");
            entity.Property(e => e.CreatedOn)
                .HasPrecision(6)
                .HasColumnName("CREATED_ON");
            entity.Property(e => e.HsnCode)
                .HasMaxLength(255)
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
                .HasColumnName("INVOICE_ID");
            entity.Property(e => e.IsTaxable).HasColumnName("IS_TAXABLE");
            entity.Property(e => e.ItemNotes)
                .IsUnicode(false)
                .HasColumnName("ITEM_NOTES");
            entity.Property(e => e.ItemPacked).HasColumnName("ITEM_PACKED");
            entity.Property(e => e.Metal)
                .HasMaxLength(20)
                .HasColumnName("METAL");
            entity.Property(e => e.ModifiedBy)
                .HasMaxLength(50)
                .HasColumnName("MODIFIED_BY");
            entity.Property(e => e.ModifiedOn)
                .HasPrecision(6)
                .HasColumnName("MODIFIED_ON");
            entity.Property(e => e.ProdCategory)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("PROD_CATEGORY");
            entity.Property(e => e.ProdGrossWeight)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("PROD_GROSS_WEIGHT");
            entity.Property(e => e.ProdNetWeight)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("PROD_NET_WEIGHT");
            entity.Property(e => e.ProdPackCode)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("PROD_PACK_CODE");
            entity.Property(e => e.ProdQty)
                .HasDefaultValue(1)
                .HasColumnName("PROD_QTY");
            entity.Property(e => e.ProdStoneWeight)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("PROD_STONE_WEIGHT");
            entity.Property(e => e.ProductDesc)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("PRODUCT_DESC");
            entity.Property(e => e.ProductGkey).HasColumnName("PRODUCT_GKEY");
            entity.Property(e => e.ProductId)
                .HasMaxLength(50)
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
            entity.Property(e => e.TenantGkey)
                .HasMaxLength(50)
                .HasColumnName("TENANT_GKEY");
            entity.Property(e => e.VaAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("VA_AMOUNT");
            entity.Property(e => e.VaPercent)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("VA_PERCENT");
        });

        modelBuilder.Entity<Metal>(entity =>
        {
            entity.HasKey(e => e.Gkey).HasName("METALS_PK");

            entity.ToTable("METALS");

            entity.HasIndex(e => e.Gkey, "METALS_PK_2").IsUnique();

            entity.Property(e => e.Gkey)
                .ValueGeneratedNever()
                .HasColumnName("GKEY");
            entity.Property(e => e.MetalName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("METAL_NAME");
        });

        modelBuilder.Entity<OrgAddress>(entity =>
        {
            entity.HasKey(e => e.Gkey).HasName("ORG_ADDRESS_PK");

            entity.ToTable("ORG_ADDRESS");

            entity.HasIndex(e => e.Gkey, "ORG_ADDRESS_PK_2").IsUnique();

            entity.Property(e => e.Gkey)
                .ValueGeneratedNever()
                .HasColumnName("GKEY");
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
            entity.Property(e => e.State)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("STATE");
            entity.Property(e => e.TenantGkey)
                .HasColumnType("decimal(19, 0)")
                .HasColumnName("TENANT_GKEY");
        });

        modelBuilder.Entity<OrgBankDetail>(entity =>
        {
            entity.HasKey(e => e.Gkey).HasName("BANK_DETAILS_PK");

            entity.ToTable("ORG_BANK_DETAILS");

            entity.HasIndex(e => e.Gkey, "BANK_DETAILS_PK_2").IsUnique();

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
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("CREATED_BY");
            entity.Property(e => e.CreatedOn)
                .HasPrecision(6)
                .HasColumnName("CREATED_ON");
            entity.Property(e => e.DeleteFlag).HasColumnName("DELETE_FLAG");
            entity.Property(e => e.DraftId).HasColumnName("DRAFT_ID");
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

            entity.HasOne(d => d.AddressGkeyNavigation).WithMany(p => p.OrgCompanies)
                .HasForeignKey(d => d.AddressGkey)
                .HasConstraintName("FK_ORG_COMPANY_ORG_ADDRESS");
        });

        modelBuilder.Entity<OrgContact>(entity =>
        {
            entity.HasKey(e => e.Gkey).HasName("MOBILE_PK");

            entity.ToTable("ORG_CONTACT");

            entity.HasIndex(e => e.Gkey, "MOBILE_PK_2").IsUnique();

            entity.Property(e => e.Gkey)
                .ValueGeneratedNever()
                .HasColumnName("GKEY");
            entity.Property(e => e.CustRefGkey)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("CUST_REF_GKEY");
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

            entity.HasIndex(e => e.Gkey, "EMAIL_PK_2").IsUnique();

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

            entity.HasIndex(e => e.Gkey, "ORG_CUSTOMER_PK_2").IsUnique();

            entity.Property(e => e.Gkey)
                .ValueGeneratedNever()
                .HasColumnName("GKEY");
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

        modelBuilder.Entity<OrgGeoLocation>(entity =>
        {
            entity.HasKey(e => e.Gkey).HasName("PK__ORG_GEO___5F3369A0459AE569");

            entity.ToTable("ORG_GEO_LOCATIONS");

            entity.HasIndex(e => e.Gkey, "SYS_C009033").IsUnique();

            entity.Property(e => e.Gkey)
                .HasMaxLength(255)
                .IsUnicode(false)
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

        modelBuilder.Entity<OrgThisCompanyView>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("ORG_THIS_COMPANY_VIEW");

            entity.Property(e => e.AddressLine1)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("address_line1");
            entity.Property(e => e.AddressLine2)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("address_line2");
            entity.Property(e => e.Area)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("area");
            entity.Property(e => e.City)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("city");
            entity.Property(e => e.CompanyName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("company_name");
            entity.Property(e => e.Country)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("country");
            entity.Property(e => e.District)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("district");
            entity.Property(e => e.GstCode)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("gst_code");
            entity.Property(e => e.GstNbr)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("gst_nbr");
            entity.Property(e => e.PanNbr)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("pan_nbr");
            entity.Property(e => e.State)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("state");
            entity.Property(e => e.ThisCompany).HasColumnName("this_company");
        });

        modelBuilder.Entity<ProductGroup>(entity =>
        {
            entity.HasKey(e => e.Gkey).HasName("PRODUCT_GROUP_PK");

            entity.ToTable("PRODUCT_GROUP");

            entity.HasIndex(e => e.Gkey, "PRODUCT_GROUP_PK_2").IsUnique();

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
            entity.Property(e => e.ActiveForSale).HasColumnName("ACTIVE_FOR_SALE");
            entity.Property(e => e.BaseUnit)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("BASE_UNIT");
            entity.Property(e => e.Brand)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("BRAND");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("CREATED_BY");
            entity.Property(e => e.CreatedOn)
                .HasPrecision(6)
                .HasColumnName("CREATED_ON");
            entity.Property(e => e.DeletedFlag).HasColumnName("DELETED_FLAG");
            entity.Property(e => e.GrossWeight)
                .HasColumnType("decimal(9, 2)")
                .HasColumnName("GROSS_WEIGHT");
            entity.Property(e => e.HsnCode)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("HSN_CODE");
            entity.Property(e => e.Metal)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("METAL");
            entity.Property(e => e.Model)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("MODEL");
            entity.Property(e => e.ModifiedBy)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("MODIFIED_BY");
            entity.Property(e => e.ModifiedOn)
                .HasPrecision(6)
                .HasColumnName("MODIFIED_ON");
            entity.Property(e => e.NetWeight)
                .HasColumnType("decimal(9, 2)")
                .HasColumnName("NET_WEIGHT");
            entity.Property(e => e.OtherWeight)
                .HasColumnType("decimal(9, 2)")
                .HasColumnName("OTHER_WEIGHT");
            entity.Property(e => e.ProductDesc)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("PRODUCT_DESC");
            entity.Property(e => e.ProductGkey).HasColumnName("PRODUCT_GKEY");
            entity.Property(e => e.ProductId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("PRODUCT_ID");
            entity.Property(e => e.ProductImageRef)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("PRODUCT_IMAGE_REF");
            entity.Property(e => e.ProductName)
                .HasMaxLength(200)
                .HasColumnName("PRODUCT_NAME");
            entity.Property(e => e.PurchaseRef)
                .HasMaxLength(50)
                .HasColumnName("PURCHASE_REF");
            entity.Property(e => e.Qty).HasColumnName("QTY");
            entity.Property(e => e.SetIdGkey)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("SET_ID_GKEY");
            entity.Property(e => e.Status)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("STATUS");
            entity.Property(e => e.StockId).HasColumnName("STOCK_ID");
            entity.Property(e => e.SupplierId).HasColumnName("SUPPLIER_ID");
            entity.Property(e => e.TaxRule)
                .HasMaxLength(50)
                .HasColumnName("TAX_RULE");
            entity.Property(e => e.Taxable).HasColumnName("TAXABLE");
            entity.Property(e => e.TenantGkey)
                .HasMaxLength(255)
                .HasColumnName("TENANT_GKEY");
            entity.Property(e => e.Uom)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("UOM");
            entity.Property(e => e.VaPercent)
                .HasColumnType("decimal(9, 2)")
                .HasColumnName("VA_PERCENT");
        });

        modelBuilder.Entity<ProductTransaction>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("PRODUCT_TRANSACTION");

            entity.Property(e => e.CbQty)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("CB_QTY");
            entity.Property(e => e.Column1)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("COLUMN1");
            entity.Property(e => e.DocRefNbr)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("DOC_REF_NBR");
            entity.Property(e => e.DocType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("DOC_TYPE");
            entity.Property(e => e.Gkey).HasColumnName("GKEY");
            entity.Property(e => e.ProductRefGkey).HasColumnName("PRODUCT_REF_GKEY");
            entity.Property(e => e.RefGkey).HasColumnName("REF_GKEY");
            entity.Property(e => e.TransDate)
                .HasColumnType("datetime")
                .HasColumnName("TRANS_DATE");
            entity.Property(e => e.TransNote)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("TRANS_NOTE");
            entity.Property(e => e.TransQty)
                .HasColumnType("decimal(10, 3)")
                .HasColumnName("TRANS_QTY");
            entity.Property(e => e.TransactionValue)
                .HasColumnType("decimal(19, 2)")
                .HasColumnName("TRANSACTION_VALUE");
            entity.Property(e => e.UnitTransPrice)
                .HasColumnType("decimal(19, 2)")
                .HasColumnName("UNIT_TRANS_PRICE");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
