using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class OrgContact
{
    public long Gkey { get; set; }

    public string MobileNbr { get; set; } = null!;

    public string CustRefGkey { get; set; } = null!;

    public string? Status { get; set; }
}
