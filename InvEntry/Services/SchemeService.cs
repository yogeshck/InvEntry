using InvEntry.Contracts.Schemes;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InvEntry.Services;

/// <summary>
/// Savings scheme (RD) plans and enrollments. Installments are paid with
/// <see cref="IAdvanceAccountService.CreateReceipt"/> using the enrollment's
/// AdvanceAccountGkey.
/// </summary>
public interface ISchemeService
{
    Task<IReadOnlyList<SchemePlanDto>> GetPlans(bool activeOnly);

    Task<SchemePlanDto> SavePlan(SchemePlanDto plan);

    Task<IReadOnlyList<SchemeMetalOption>> GetMetalOptions();

    /// <summary>Reuse the same request (same ClientRequestId) when retrying after a failure.</summary>
    Task<SchemeEnrollmentDto> Enroll(SchemeEnrollmentRequest request);

    Task<IReadOnlyList<SchemeEnrollmentDto>> GetCustomerEnrollments(int customerGkey, bool includeClosed = false);

    /// <summary>Checks (Apply = false) or imports the shop's RD register.</summary>
    Task<SchemeImportResult> Import(SchemeImportRequest request);

    /// <summary>The scheme with this RD number (e.g. MJ-RD26-002), or null when unknown.</summary>
    Task<SchemeEnrollmentDto?> FindByAccountNumber(string accountNbr);

    /// <summary>RD passbook / statement data.</summary>
    Task<RdStatementDto> GetStatement(int enrollmentGkey);

    /// <summary>RD installments due or collected in the month (optionally with earlier arrears).</summary>
    Task<RdDueReportDto> GetMonthlyDues(int year, int month, bool includeArrears);
}

public class SchemeService : ISchemeService
{
    private readonly IMijmsApiService _mijmsApiService;

    public SchemeService(IMijmsApiService mijmsApiService)
    {
        _mijmsApiService = mijmsApiService;
    }

    public async Task<IReadOnlyList<SchemePlanDto>> GetPlans(bool activeOnly)
    {
        return await _mijmsApiService.GetResponse<List<SchemePlanDto>>(
            $"api/SchemePlan?activeOnly={activeOnly.ToString().ToLowerInvariant()}");
    }

    public Task<SchemePlanDto> SavePlan(SchemePlanDto plan)
    {
        return _mijmsApiService.Post<SchemePlanDto, SchemePlanDto>("api/SchemePlan", plan);
    }

    public async Task<IReadOnlyList<SchemeMetalOption>> GetMetalOptions()
    {
        return await _mijmsApiService.GetResponse<List<SchemeMetalOption>>("api/SchemePlan/metal-options");
    }

    public Task<SchemeEnrollmentDto> Enroll(SchemeEnrollmentRequest request)
    {
        return _mijmsApiService.Post<SchemeEnrollmentRequest, SchemeEnrollmentDto>(
            "api/SchemeEnrollment", request);
    }

    public async Task<IReadOnlyList<SchemeEnrollmentDto>> GetCustomerEnrollments(
        int customerGkey,
        bool includeClosed = false)
    {
        return await _mijmsApiService.GetResponse<List<SchemeEnrollmentDto>>(
            $"api/SchemeEnrollment/customer/{customerGkey}?includeClosed={includeClosed.ToString().ToLowerInvariant()}");
    }

    public Task<SchemeImportResult> Import(SchemeImportRequest request)
    {
        return _mijmsApiService.Post<SchemeImportRequest, SchemeImportResult>("api/SchemeImport", request);
    }

    public async Task<SchemeEnrollmentDto?> FindByAccountNumber(string accountNbr)
    {
        var found = await _mijmsApiService.GetResponse<List<SchemeEnrollmentDto>>(
            $"api/SchemeEnrollment/by-account?accountNbr={System.Uri.EscapeDataString(accountNbr.Trim())}");

        return found.FirstOrDefault();
    }

    public Task<RdStatementDto> GetStatement(int enrollmentGkey)
    {
        return _mijmsApiService.GetResponse<RdStatementDto>($"api/SchemeEnrollment/{enrollmentGkey}/statement");
    }

    public Task<RdDueReportDto> GetMonthlyDues(int year, int month, bool includeArrears)
    {
        return _mijmsApiService.GetResponse<RdDueReportDto>(
            $"api/SchemeEnrollment/dues?year={year}&month={month}&includeArrears={includeArrears.ToString().ToLowerInvariant()}");
    }
}
