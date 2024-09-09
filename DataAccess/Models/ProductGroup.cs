using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class ProductGroup
{
    public int Gkey { get; set; }

    public string? GroupName { get; set; }

    public string? Purity { get; set; }

    public string? Category { get; set; }
}
