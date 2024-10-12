using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class OrgContactEmail
{
    public int Gkey { get; set; }

    public int CustRefGkey { get; set; }

    public string? EmailId { get; set; }

    public string? ActiveFlag { get; set; }

    public string? Status { get; set; }
}
