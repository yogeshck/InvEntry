using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class OrgContactEmail
{
    public long Gkey { get; set; }

    public double CustRefGkey { get; set; }

    public string? EmailId { get; set; }

    public string? ActiveFlag { get; set; }

    public string? Status { get; set; }
}
