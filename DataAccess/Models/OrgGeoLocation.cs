using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class OrgGeoLocation
{
    public string Gkey { get; set; } = null!;

    public string? ExternalId { get; set; }

    public string? LocationType { get; set; }

    public string? Name { get; set; }

    public string? ParentId { get; set; }

    public string? Pincode { get; set; }
}
