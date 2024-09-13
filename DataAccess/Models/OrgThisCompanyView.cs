using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class OrgThisCompanyView
{
    public string? CompanyName { get; set; }

    public string? AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    public string? Area { get; set; }

    public string? State { get; set; }

    public string? GstCode { get; set; }

    public string? Country { get; set; }

    public string? District { get; set; }

    public string? City { get; set; }

    public string? PanNbr { get; set; }

    public string? GstNbr { get; set; }

    public bool? ThisCompany { get; set; }
}
