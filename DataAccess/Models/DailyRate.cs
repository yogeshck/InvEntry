using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class DailyRate
{
    public long Gkey { get; set; }

    public DateTime EffectiveDate { get; set; }

    public string? Metal { get; set; }

    public string? Purity { get; set; }

    public string? Carat { get; set; }

    public decimal? Price { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedOn { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedOn { get; set; }

    public bool IsDisplay { get; set; }
}
