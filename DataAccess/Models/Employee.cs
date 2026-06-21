using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class Employee
{
    public int Gkey { get; set; }

    public string EmployeeCode { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string? MiddleName { get; set; }

    public string LastName { get; set; } = null!;

    public string? Gender { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public DateOnly? DateOfJoin { get; set; }

    public string? BloodGroupId { get; set; }

    public int? MaritalStatus { get; set; }

    public string? MobileNbr { get; set; }

    public string? WorkMobileNbr { get; set; }

    public string? EmergencyContactNbr { get; set; }

    public string? PersonalEmail { get; set; }

    public string? WorkEmail { get; set; }

    public bool IsActive { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedOn { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime UpdatedOn { get; set; }
}
