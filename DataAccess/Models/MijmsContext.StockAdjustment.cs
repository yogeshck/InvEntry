using Microsoft.EntityFrameworkCore;

namespace DataAccess.Models;

// Kept separate from the scaffolded MijmsContext.cs so future re-scaffolding does
// not overwrite the Stock Adjustment mapping.
public partial class MijmsContext
{
    public virtual DbSet<StockAdjustmentHeader> StockAdjustmentHeaders { get; set; }
    public virtual DbSet<StockAdjustmentLine> StockAdjustmentLines { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StockAdjustmentHeader>(entity =>
        {
            entity.HasKey(e => e.Gkey);
            entity.ToTable("STOCK_ADJUSTMENT_HEADER");

            entity.Property(e => e.Gkey).HasColumnName("GKEY");
            entity.Property(e => e.AdjustmentNbr).HasMaxLength(50).IsUnicode(false).HasColumnName("ADJUSTMENT_NBR");
            entity.Property(e => e.AdjustmentDate).HasPrecision(6).HasColumnName("ADJUSTMENT_DATE");
            entity.Property(e => e.AdjustmentType).HasMaxLength(20).IsUnicode(false).HasColumnName("ADJUSTMENT_TYPE");
            entity.Property(e => e.ReasonCode).HasMaxLength(50).IsUnicode(false).HasColumnName("REASON_CODE");
            entity.Property(e => e.Status).HasMaxLength(20).IsUnicode(false).HasColumnName("STATUS");
            entity.Property(e => e.Remarks).HasMaxLength(500).IsUnicode(false).HasColumnName("REMARKS");
            entity.Property(e => e.TotalQty).HasColumnName("TOTAL_QTY");
            entity.Property(e => e.TotalGrossWeight).HasColumnType("decimal(18, 3)").HasColumnName("TOTAL_GROSS_WEIGHT");
            entity.Property(e => e.TotalStoneWeight).HasColumnType("decimal(18, 3)").HasColumnName("TOTAL_STONE_WEIGHT");
            entity.Property(e => e.TotalNetWeight).HasColumnType("decimal(18, 3)").HasColumnName("TOTAL_NET_WEIGHT");
            entity.Property(e => e.CreatedBy).HasMaxLength(50).IsUnicode(false).HasColumnName("CREATED_BY");
            entity.Property(e => e.CreatedOn).HasPrecision(6).HasColumnName("CREATED_ON");
            entity.Property(e => e.ModifiedBy).HasMaxLength(50).IsUnicode(false).HasColumnName("MODIFIED_BY");
            entity.Property(e => e.ModifiedOn).HasPrecision(6).HasColumnName("MODIFIED_ON");
            entity.Property(e => e.FinalisedOn).HasPrecision(6).HasColumnName("FINALISED_ON");
        });

        modelBuilder.Entity<StockAdjustmentLine>(entity =>
        {
            entity.HasKey(e => e.Gkey);
            entity.ToTable("STOCK_ADJUSTMENT_LINE");

            entity.Property(e => e.Gkey).HasColumnName("GKEY");
            entity.Property(e => e.AdjustmentHdrGkey).HasColumnName("ADJUSTMENT_HDR_GKEY");
            entity.Property(e => e.LineNbr).HasColumnName("LINE_NBR");
            entity.Property(e => e.Direction).HasMaxLength(10).IsUnicode(false).HasColumnName("DIRECTION");
            entity.Property(e => e.StockLevel).HasMaxLength(20).IsUnicode(false).HasColumnName("STOCK_LEVEL");
            entity.Property(e => e.MovementKind).HasMaxLength(20).IsUnicode(false).HasColumnName("MOVEMENT_KIND");
            entity.Property(e => e.ProductGkey).HasColumnName("PRODUCT_GKEY");
            entity.Property(e => e.ProductStockGkey).HasColumnName("PRODUCT_STOCK_GKEY");
            entity.Property(e => e.ProductSku).HasMaxLength(50).IsUnicode(false).HasColumnName("PRODUCT_SKU");
            entity.Property(e => e.ProductCategory).HasMaxLength(255).IsUnicode(false).HasColumnName("PRODUCT_CATEGORY");
            entity.Property(e => e.Metal).HasMaxLength(50).IsUnicode(false).HasColumnName("METAL");
            entity.Property(e => e.Purity).HasMaxLength(255).IsUnicode(false).HasColumnName("PURITY");
            entity.Property(e => e.Uom).HasMaxLength(50).IsUnicode(false).HasColumnName("UOM");
            entity.Property(e => e.Qty).HasColumnName("QTY");
            entity.Property(e => e.GrossWeight).HasColumnType("decimal(18, 3)").HasColumnName("GROSS_WEIGHT");
            entity.Property(e => e.StoneWeight).HasColumnType("decimal(18, 3)").HasColumnName("STONE_WEIGHT");
            entity.Property(e => e.NetWeight).HasColumnType("decimal(18, 3)").HasColumnName("NET_WEIGHT");
            entity.Property(e => e.PairNbr).HasColumnName("PAIR_NBR");
            entity.Property(e => e.Notes).HasMaxLength(500).IsUnicode(false).HasColumnName("NOTES");

            entity.HasOne(d => d.AdjustmentHdrGkeyNavigation)
                .WithMany(p => p.StockAdjustmentLines)
                .HasForeignKey(d => d.AdjustmentHdrGkey)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_STOCK_ADJUSTMENT_LINE_HEADER");

            entity.HasOne(d => d.ProductStockGkeyNavigation)
                .WithMany()
                .HasForeignKey(d => d.ProductStockGkey)
                .HasConstraintName("FK_STOCK_ADJUSTMENT_LINE_PRODUCT_STOCK");
        });
    }
}
