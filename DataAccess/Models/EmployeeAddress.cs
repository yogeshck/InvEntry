using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class EmployeeAddress
{
    public int Gkey { get; set; }

    public int? EmployeeGkey { get; set; }

    public string? AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    public string? AddressLine3 { get; set; }

    public string? City { get; set; }

    public string? District { get; set; }

    public string? State { get; set; }

    public string? Pincode { get; set; }

    public string Country { get; set; } = null!;

    public bool IsPrimary { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedOn { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime UpdatedOn { get; set; }
}
