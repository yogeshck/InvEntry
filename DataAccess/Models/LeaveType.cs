using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class LeaveType
{
    public int Gkey { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool? IsPaid { get; set; }

    public bool? IsCarryForward { get; set; }

    public decimal? MaxCarryForward { get; set; }

    public bool? IsEncashable { get; set; }

    public decimal? MaxEncashment { get; set; }

    public int? MinServiceDays { get; set; }

    public decimal? MaxPerMonth { get; set; }

    public decimal? MaxPerYear { get; set; }

    public string? Gender { get; set; }

    public bool? RequiresProof { get; set; }

    public int? ApprovalLevels { get; set; }

    public bool? IsActive { get; set; }
}
