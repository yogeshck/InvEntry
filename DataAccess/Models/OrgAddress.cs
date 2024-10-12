using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class OrgAddress
{
    public int Gkey { get; set; }

    public string? AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    public string? AddressLine3 { get; set; }

    public string? City { get; set; }

    public string? District { get; set; }

    public string? State { get; set; }

    public string? Country { get; set; }

    public decimal? TenantGkey { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedOn { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedOn { get; set; }

    public string? Area { get; set; }

    public string? GstStateCode { get; set; }

    public virtual ICollection<OrgCompany> OrgCompanies { get; set; } = new List<OrgCompany>();
}
