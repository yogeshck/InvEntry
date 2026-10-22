using DataAccess.Models;
using DataAccess.Models.FinanceIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
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
/// A COLLECTION document (money received against a finalised invoice later)
/// goes to <c>usp_SyncInvEntryCollection</c> with its voucher gkey instead.
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

        var isCollection = string.Equals(payload.DocumentType,
            FinanceInvoicePayloadBuilder.CollectionDocumentType, StringComparison.OrdinalIgnoreCase);

        var invoiceNumber = payload.SourceDocumentNo ?? payload.DocumentNo;
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            return new(FinancePostingOutcome.BusinessRejected,
                "The financial document has no invoice number to synchronize.");

        int voucherGkey = 0;
        if (isCollection &&
            !int.TryParse(payload.Movements.FirstOrDefault()?.SourceReference, out voucherGkey))
            return new(FinancePostingOutcome.BusinessRejected,
                "The collection has no InvEntry voucher to synchronize.");

        var procedure = isCollection
            ? "FinanceTracker.dbo.usp_SyncInvEntryCollection"
            : "FinanceTracker.dbo.usp_SyncInvEntryInvoice";

        var connection = context.Database.GetDbConnection();
        var closeConnection = connection.State != ConnectionState.Open;
        if (closeConnection)
            await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = procedure;
            SetCurrentTransaction(command);

            if (isCollection)
                Add(command, "@VoucherGkey", voucherGkey);
            else
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
                    $"Unexpected result from {procedure}: {result ?? "<none>"}.");
        }
        catch (DbException) when (isCollection)
        {
            // The invoice posting log is per invoice; a collection's error stays on its event.
            throw;
        }
        catch (DbException ex)
        {
            // The posting rolled back, so the error is written afterwards, on its
            // own, to be visible in FinanceTracker's invoice posting log.
            await RecordFailureAsync(invoiceNumber, payload.OrgGkey,
                payload.LocationGkey, ex is SqlException sqlError ? sqlError.Number : null,
                ex.Message, cancellationToken);
            throw;
        }
        finally
        {
            if (closeConnection)
                await connection.CloseAsync();
        }
    }

    private async Task RecordFailureAsync(string invoiceNumber, int orgGkey,
        int locationGkey, int? errorNumber, string message,
        CancellationToken cancellationToken)
    {
        try
        {
            var connection = context.Database.GetDbConnection();
            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "FinanceTracker.dbo.usp_SyncInvEntryRecordFailure";
            Add(command, "@InvoiceNumber", invoiceNumber);
            Add(command, "@OrgGkey", orgGkey);
            Add(command, "@LocationGkey", locationGkey);
            Add(command, "@ErrorNumber", (object?)errorNumber ?? DBNull.Value);
            Add(command, "@ErrorMessage", Truncate(message, 1000));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception)
        {
            // The original posting error is what the caller needs; a failure to
            // record it must not replace that error.
        }
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

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
