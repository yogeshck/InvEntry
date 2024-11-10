using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class OrgCustomerAddressView
{
    public string? CustName { get; set; }

    public string? PanNbr { get; set; }

    public string? CreditAvailed { get; set; }

    public string? CustStatus { get; set; }

    public DateTime? CustSince { get; set; }

    public string? Salutations { get; set; }

    public string? CustType { get; set; }

    public string? AddLine1 { get; set; }

    public string? AddLine2 { get; set; }

    public string? AddLine3 { get; set; }

    public string? Area { get; set; }

    public string? City { get; set; }

    public string? CustGstCode { get; set; }

    public string? District { get; set; }

    public string? State { get; set; }

    public string? Country { get; set; }

    public string? MobileNbr { get; set; }

    public int CustGkey { get; set; }

    public string? ClientId { get; set; }

    public string? GstinNbr { get; set; }

    public int? LocationGkey { get; set; }

    public int? AddressGkey { get; set; }
}
