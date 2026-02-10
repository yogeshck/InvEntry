using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class RepairHeader
{
    public int RepairHeaderId { get; set; }

    public string InvoiceNo { get; set; } = null!;

    public DateOnly InvoiceDate { get; set; }

    public string JobNo { get; set; } = null!;

    public DateOnly DeliveryDate { get; set; }

    public int CustomerId { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal Advance { get; set; }

    public decimal NetPayable { get; set; }

    public string? PaymentMode { get; set; }

    public string? TransactionRef { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Charge> Charges { get; set; } = new List<Charge>();

    public virtual ICollection<GoldLedger> GoldLedgers { get; set; } = new List<GoldLedger>();

    public virtual ICollection<RepairDetail> RepairDetails { get; set; } = new List<RepairDetail>();
}
