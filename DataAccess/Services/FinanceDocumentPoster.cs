using DataAccess.Models;
using DataAccess.Models.FinanceIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Data.Common;

namespace DataAccess.Services;

public enum FinancePostingOutcome
{
    Posted,
    BusinessRejected,
    Unavailable
}

public sealed record FinancePostingResult(
    FinancePostingOutcome Outcome, string? ErrorMessage);

public interface IFinanceDocumentPoster
{
    Task<FinancePostingResult> PostAsync(FinanceDocumentPayload payload,
        CancellationToken cancellationToken);
}

/// <summary>
/// Posts an InvEntry invoice into FinanceTracker by calling
/// <c>FinanceTracker.dbo.usp_SyncInvEntryInvoice</c> once per invoice, inside
/// the caller's ambient transaction. Both databases live on the same SQL
/// Server instance, so no HTTP client, base URL or API key is involved.
/// </summary>
/// <remarks>
/// The procedure derives the invoice's vouchers from <c>mijms</c> itself and
/// decides suspense: a day closed in FinanceTracker, or a manual entry that
/// already references the invoice, is recorded with <c>IS_SUSPENSE = 1</c>
/// instead of failing. It therefore never blocks invoice finalisation, and
/// this poster passes only the invoice identity.
/// </remarks>
internal sealed class FinanceDocumentPoster(MijmsContext context)
    : IFinanceDocumentPoster
{
    public async Task<FinancePostingResult> PostAsync(
        FinanceDocumentPayload payload, CancellationToken cancellationToken)
    {
        if (!context.Database.IsSqlServer())
            throw new InvalidOperationException(
                "FinanceTracker posting requires SQL Server.");

        var invoiceNumber = payload.SourceDocumentNo ?? payload.DocumentNo;
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            return new(FinancePostingOutcome.BusinessRejected,
                "The financial document has no invoice number to synchronize.");

        var connection = context.Database.GetDbConnection();
        var closeConnection = connection.State != ConnectionState.Open;
        if (closeConnection)
            await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "FinanceTracker.dbo.usp_SyncInvEntryInvoice";
            SetCurrentTransaction(command);

            Add(command, "@InvoiceNumber", invoiceNumber);
            Add(command, "@PreviewOnly", false);
            Add(command, "@OrgGkey", payload.OrgGkey);
            Add(command, "@LocationGkey", payload.LocationGkey);

            await using var reader =
                await command.ExecuteReaderAsync(cancellationToken);

            string? result = null;
            if (await reader.ReadAsync(cancellationToken))
            {
                var resultOrdinal = reader.GetOrdinal("Result");
                if (!await reader.IsDBNullAsync(resultOrdinal, cancellationToken))
                    result = reader.GetString(resultOrdinal);
            }

            // SYNCED and ALREADY_SYNCED are both successful outcomes: the
            // latter means an earlier attempt already completed the posting
            // (e.g. after a crash between claim and dispatch).
            return result is "SYNCED" or "ALREADY_SYNCED"
                ? new(FinancePostingOutcome.Posted, null)
                : new(FinancePostingOutcome.Unavailable,
                    $"Unexpected result from usp_SyncInvEntryInvoice: {result ?? "<none>"}.");
        }
        finally
        {
            if (closeConnection)
                await connection.CloseAsync();
        }
    }

    private void SetCurrentTransaction(DbCommand command)
    {
        var transaction = context.Database.CurrentTransaction;
        if (transaction != null)
            command.Transaction = transaction.GetDbTransaction();
    }

    private static void Add(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
