using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class OrgCustomer
{
    public long Gkey { get; set; }

    public string? ClientId { get; set; }

    public string? CustomerName { get; set; }

    public string? CustomerType { get; set; }

    public string? MobileNbr { get; set; }

    public string? GstinNbr { get; set; }

    public string? LedgerName { get; set; }

    public string? Notes { get; set; }

    public string? PanNbr { get; set; }

    public string? Status { get; set; }

    public string? CreditAvailed { get; set; }

    public long? LocationGkey { get; set; }

    public bool? DeleteFlag { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedOn { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedOn { get; set; }

    public long? TenantGkey { get; set; }

    public long? AddressGkey { get; set; }

    public DateTime? CustomerSince { get; set; }

    public string? Salutations { get; set; }

    public virtual OrgAddress GkeyNavigation { get; set; } = null!;
}
