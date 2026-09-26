using InvEntry.Models.UI;
using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InvEntry.Services;

public interface IDailyRateDefinitionService
{
    Task<IReadOnlyList<DailyRateDefinition>>
        GetDefinitionsAsync();
}


public sealed class DailyRateDefinitionService
    : IDailyRateDefinitionService
{
    private const string ReferenceName = "DAILY_RATE";

    private readonly IMijmsApiService
        _mijmsApiService;


    public DailyRateDefinitionService(
        IMijmsApiService mijmsApiService)
    {
        _mijmsApiService = mijmsApiService;
    }


    public async Task<IReadOnlyList<DailyRateDefinition>>
        GetDefinitionsAsync()
    {
        var references =
            await _mijmsApiService
                .GetResponse<List<MtblReference>>(
                    $"api/MtblReference/{ReferenceName}");


        if (references is null)
            return Array.Empty<DailyRateDefinition>();


        return references
            .OrderBy(x => x.SortSeq ?? short.MaxValue)
            .Select(x =>
                new DailyRateDefinition
                {
                    GKey = x.GKey,

                    Metal =
                        x.RefCode?.Trim()
                        ?? string.Empty,

                    Purity =
                        string.IsNullOrWhiteSpace(x.RefValue)
                            ? null
                            : x.RefValue.Trim(),

                    Carat =
                        string.IsNullOrWhiteSpace(x.RefDesc)
                            ? null
                            : x.RefDesc.Trim(),

                    DisplayOrder =
                        x.SortSeq ?? 0,

                    TrackDailyRate =
                        x.IsActive,

                    ShowInHeader =
                        string.Equals(
                            x.Module,
                            "HEADER",
                            StringComparison.OrdinalIgnoreCase)
                })
            .Where(x =>
                !string.IsNullOrWhiteSpace(x.Metal))
            .ToList();
    }
}
