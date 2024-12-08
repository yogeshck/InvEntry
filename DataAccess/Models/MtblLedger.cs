using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class MtblLedger
{
    public int Gkey { get; set; }

    public string? LedgerName { get; set; }

    public string? Description { get; set; }

    public string? AccountGroupName { get; set; }

    public bool? Status { get; set; }

    public int? SubGroup { get; set; }

    public int? PrimaryGroup { get; set; }

    public int? MemberGkey { get; set; }

    public string? MemberType { get; set; }

    public int? TenantGkey { get; set; }

    public int? LedgerAccountCode { get; set; }
}
