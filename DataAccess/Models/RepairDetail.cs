using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class RepairDetail
{
    public int RepairDetailId { get; set; }

    public int RepairHeaderId { get; set; }

    public string ItemDescription { get; set; } = null!;

    public decimal ReceivedWeight { get; set; }

    public decimal DeliveredWeight { get; set; }

    public string Purity { get; set; } = null!;

    public decimal Wastage { get; set; }

    public decimal ExtraGoldUsed { get; set; }

    public virtual RepairHeader RepairHeader { get; set; } = null!;
}
