using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class MtblReference
{
    public int Gkey { get; set; }

    public string RefName { get; set; } = null!;

    public string RefCode { get; set; } = null!;

    public string RefValue { get; set; } = null!;

    public int SortSeq { get; set; }

    public string? RefDesc { get; set; }

    public string? Module { get; set; }

    public bool IsActive { get; set; }
}
