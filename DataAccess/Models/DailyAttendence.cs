using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class DailyAttendence
{
    public int Gkey { get; set; }

    public int EmployeeGkey { get; set; }

    public string? WorkSite { get; set; }

    public DateOnly WorkDate { get; set; }

    public string WorkStatus { get; set; } = null!;

    public decimal? WorkHoursDay { get; set; }

    public DateTime? FirstIn { get; set; }

    public DateTime? LastOut { get; set; }

    public int PunchCount { get; set; }

    public int? LeaveRequestGkey { get; set; }

    public string? Notes { get; set; }

    public int? TenantGkey { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedOn { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedOn { get; set; }
}
