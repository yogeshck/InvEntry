using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class GoldLedger
{
    public int GoldLedgerId { get; set; }

    public int RepairHeaderId { get; set; }

    public decimal GoldReceived { get; set; }

    public decimal GoldReturned { get; set; }

    public decimal NetWastage { get; set; }

    public decimal ExtraGoldCharged { get; set; }

    public string Status { get; set; } = null!;

    public virtual RepairHeader RepairHeader { get; set; } = null!;
}
