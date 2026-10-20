using InvEntry.Contracts.Schemes;
using System.Collections.Generic;
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
}
