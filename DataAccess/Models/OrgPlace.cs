using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class OrgPlace
{
    public int Gkey { get; set; }

    public int Pincode { get; set; }

    public string? LocalityVillageName { get; set; }

    public string? PostOfficeName { get; set; }

    public string? SubDistrictName { get; set; }

    public string? DistrictName { get; set; }

    public string? StateName { get; set; }
}
