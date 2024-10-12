using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class OrgContact
{
    public int Gkey { get; set; }

    public string MobileNbr { get; set; } = null!;

    public int CustRefGkey { get; set; }

    public string? Status { get; set; }
}
