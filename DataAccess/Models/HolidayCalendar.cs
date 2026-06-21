using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class HolidayCalendar
{
    public int Gkey { get; set; }

    public DateTime HolidayDate { get; set; }

    public string HolidayName { get; set; } = null!;

    public string? HolidayDescription { get; set; }

    public string? HolidayType { get; set; }

    public bool? IsRecurring { get; set; }

    public bool? IsOptional { get; set; }

    public string? Location { get; set; }

    public bool? IsActive { get; set; }
}
