using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class Designation
{
    public int Gkey { get; set; }

    public string? Code { get; set; }

    public string? Name { get; set; }

    public int? WorkLevel { get; set; }

    public int? DepartmentGkey { get; set; }

    public bool IsActive { get; set; }
}
