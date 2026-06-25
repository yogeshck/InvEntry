using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class Supplier
{
    public int Gkey { get; set; }

    public string SupplierCode { get; set; } = null!;

    public string ShortName { get; set; } = null!;

    public string SupplierName { get; set; } = null!;

    public string? Phone { get; set; }

    public int? AddressGkey { get; set; }

    public string? Gstin { get; set; }

    public string? Email { get; set; }

    public string? Notes { get; set; }

    public string? ContactName { get; set; }

    public bool IsActive { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedOn { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime ModifiedOn { get; set; }
}
