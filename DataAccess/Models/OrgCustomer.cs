﻿using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class OrgCustomer
{
    public int Gkey { get; set; }

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

    public int? LocationGkey { get; set; }

    public bool? DeleteFlag { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedOn { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedOn { get; set; }

    public int? TenantGkey { get; set; }

    public int? AddressGkey { get; set; }

    public DateTime? CustomerSince { get; set; }

    public string? Salutations { get; set; }

    public string? GstStateCode { get; set; }
}
