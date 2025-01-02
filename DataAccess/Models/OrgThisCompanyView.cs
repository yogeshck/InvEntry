using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class OrgThisCompanyView
{
    public string? CompanyName { get; set; }

    public string? AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    public string? AddressLine3 { get; set; }

    public string? Area { get; set; }

    public string? State { get; set; }

    public int? Pincode { get; set; }

    public string? Country { get; set; }

    public string? District { get; set; }

    public string? City { get; set; }

    public string? ContactNbr1 { get; set; }

    public string? ContactNbr2 { get; set; }

    public string? EmailId { get; set; }

    public string? PanNbr { get; set; }

    public int? TenantGkey { get; set; }

    public string? ServiceTaxNbr { get; set; }

    public string? GstNbr { get; set; }

    public string? GstCode { get; set; }

    public bool? ThisCompany { get; set; }

    public string? Tagline { get; set; }
}
