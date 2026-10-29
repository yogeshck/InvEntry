using InvEntry.Contracts.FinanceSync;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InvEntry.Services;

/// <summary>Finance sync status from the InvEntry server, and sending entries again.</summary>
public interface IFinanceSyncStatusClient
{
    Task<FinanceSyncStatusDto> GetStatus();

    /// <summary>Sends the given entries again; every entry that can be sent when none are given.</summary>
    Task<FinanceSyncSendResultDto> Send(IReadOnlyCollection<long>? eventIds);
}

public class FinanceSyncStatusClient : IFinanceSyncStatusClient
{
    private readonly IMijmsApiService _mijmsApiService;

    public FinanceSyncStatusClient(IMijmsApiService mijmsApiService)
    {
        _mijmsApiService = mijmsApiService;
    }

    public Task<FinanceSyncStatusDto> GetStatus() =>
        _mijmsApiService.GetResponse<FinanceSyncStatusDto>("api/FinanceSyncStatus");

    public Task<FinanceSyncSendResultDto> Send(IReadOnlyCollection<long>? eventIds) =>
        _mijmsApiService.Post<FinanceSyncSendRequestDto, FinanceSyncSendResultDto>(
            "api/FinanceSyncStatus/send",
            new FinanceSyncSendRequestDto
            {
                EventIds = eventIds is null ? null : new List<long>(eventIds),
                Actor = Environment.UserName
            });
}
