using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class Department
{
    public int Gkey { get; set; }

    public string Name { get; set; } = null!;

    public string? Code { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedOn { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime UpdatedOn { get; set; }
}
