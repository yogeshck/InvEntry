﻿using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class OrgCompany
{
    public int Gkey { get; set; }

    public string? AccountId { get; set; }

    public string? Name { get; set; }

    public long? DraftId { get; set; }

    public long? InvId { get; set; }

    public string? PanNbr { get; set; }

    public string? Notes { get; set; }

    public string? ServiceTaxNbr { get; set; }

    public string? Status { get; set; }

    public short? DeleteFlag { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedOn { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedOn { get; set; }

    public int? TenantGkey { get; set; }

    public string? Tagline { get; set; }

    public string? CinNbr { get; set; }

    public string? TinNbr { get; set; }

    public int? AddressGkey { get; set; }

    public string? GstNbr { get; set; }

    public bool? ThisCompany { get; set; }

    public string? ContactNbr1 { get; set; }

    public string? ContactNbr2 { get; set; }

    public string? EmailId { get; set; }
}
