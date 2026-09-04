using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class StockTransferHeader
{
    public int Gkey { get; set; }
    public string TransferNbr { get; set; } = null!;
    public DateTime TransferDate { get; set; }
    public string TransferType { get; set; } = null!;
    public string FromBranch { get; set; } = null!;
    public int? FromTenantGkey { get; set; }
    public int ToReferenceGkey { get; set; }
    public string ToReferenceCode { get; set; } = null!;
    public string ToReferenceValue { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? Remarks { get; set; }
    public int TotalQty { get; set; }
    public decimal TotalGrossWeight { get; set; }
    public decimal TotalStoneWeight { get; set; }
    public decimal TotalNetWeight { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public virtual ICollection<StockTransferLine> Lines { get; set; } = new List<StockTransferLine>();
}