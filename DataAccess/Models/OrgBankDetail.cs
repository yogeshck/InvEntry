using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class OrgBankDetail
{
    public int Gkey { get; set; }

    public string? BankName { get; set; }

    public string? BranchName { get; set; }

    public string IfscCode { get; set; } = null!;

    public string? BankAccountNbr { get; set; }

    public string? BankCustRefNbr { get; set; }

    public string? IsPrimary { get; set; }

    public string? MobileEnabled { get; set; }

    public long? UseByRefGkey { get; set; }

    public string? UseByRefName { get; set; }

    /// <summary>
    /// True/  False
    /// </summary>
    public string? OnlineEnabled { get; set; }

    public long? LocationGkey { get; set; }
}
