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
/// Posts an InvEntry financial document into FinanceTracker by calling the
/// existing <c>FinanceTracker.dbo.usp_SyncInvEntryInvoice</c> stored
/// procedure once per invoice, inside the caller's ambient transaction.
/// This replaces the previous HTTP integration entirely: no base URL, API
/// key, or network timeout handling is required because both databases
/// live on the same SQL Server instance.
/// </summary>
/// <remarks>
/// Unlike an earlier per-movement design, <c>usp_SyncInvEntryInvoice</c>
/// re-derives the invoice's vouchers directly from
/// <c>mijms.dbo.INVOICE_HEADER</c>/<c>VOUCHER</c> and performs its own
/// idempotency check, application locking, mapping validation and closed
/// period guard, inserting the <c>FIN_DOCUMENT</c> row and every eligible
/// <c>FIN_TRANSACTION</c> row in a single call. It must therefore be
/// invoked exactly once per document, not once per movement - calling it
/// per movement would make it re-derive and attempt to re-insert the
/// invoice's full movement set on every call.
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

        }
        catch (Exception)
        {

            throw;
        }
        {
            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "FinanceTracker.dbo.usp_SyncInvEntryInvoice";
            SetCurrentTransaction(command);

            Add(command, "@InvoiceNumber", invoiceNumber);
            Add(command, "@PreviewOnly", false);
            Add(command, "@OrgGkey", payload.OrgGkey);  
            Add(command, "@LocationGkey", payload.LocationGkey);
            Add(command, "@PostAsSuspense", payload.RequiresSuspense);

            // usp_SyncInvEntryInvoice re-derives and classifies the
            // invoice's vouchers itself and performs its own idempotency
            // check, so it is called exactly once per document, not once
            // per movement.
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
            // latter means a previous attempt already completed the
            // posting (e.g. after a crash between claim and dispatch), and
            // the invoice must still be marked SENT in the outbox so it is
            // not retried indefinitely.
            return result is "SYNCED" or "ALREADY_SYNCED"
                ? new(FinancePostingOutcome.Posted, null)
                : new(FinancePostingOutcome.Unavailable,
                    $"Unexpected result from usp_SyncInvEntryInvoice: {result ?? "<none>"}.");
        }

    }

    // usp_SyncInvEntryInvoice raises errors in the custom 51000-51999
    // range (THROW 51001.. for invoice-not-found, invalid amount, missing
    // mappings, closed period, etc.) to signal a permanent business
    // validation failure. Any other failure - a deadlock, a timeout, a
    // connectivity blip - is treated as transient and retried.
    private static bool IsBusinessRejection(DbException ex) =>
        ex is Microsoft.Data.SqlClient.SqlException sqlEx &&
        sqlEx.Errors.Cast<Microsoft.Data.SqlClient.SqlError>()
            .Any(e => e.Number is >= 51000 and < 52000);

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