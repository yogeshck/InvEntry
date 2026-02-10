using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class Charge
{
    public int ChargeId { get; set; }

    public int RepairHeaderId { get; set; }

    public string ChargeName { get; set; } = null!;

    public decimal Amount { get; set; }

    public virtual RepairHeader RepairHeader { get; set; } = null!;
}
