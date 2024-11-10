using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class VoucherType
{
    public int Gkey { get; set; }

    public string? DocumentType { get; set; }

    public string? Abbreviation { get; set; }

    public int? MtblVoucherTypeGkey { get; set; }

    public int? LastUsedNumber { get; set; }

    public bool? IsTaxable { get; set; }

    public string? Narration { get; set; }

    public string? DocNbrMethod { get; set; }

    public int? DocNbrLength { get; set; }

    public string? DocNbrPrefill { get; set; }

    public string? DocNbrPrefix { get; set; }

    public string? DocNbrSuffix { get; set; }

    public bool? IsActive { get; set; }

    public string? UsedFor { get; set; }
}
