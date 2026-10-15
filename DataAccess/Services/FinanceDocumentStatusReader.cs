using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Data.Common;

namespace DataAccess.Services;

/// <summary>
/// Reads whether a financial document has already been posted to
/// FinanceTracker. Replaces the previous HTTP status endpoint with a direct
/// read of the same FIN_DOCUMENT table already used by
/// <see cref="FinanceClosingFinancialEvidenceReader"/>, since both databases
/// share the same SQL Server instance.
/// </summary>
public interface IFinanceDocumentStatusReader
{
    Task<FinanceRemoteEventState> GetStatusAsync(int orgGkey,
        string sourceSystem, string sourceEventId,
        CancellationToken cancellationToken);
}

internal sealed class FinanceDocumentStatusReader(MijmsContext context)
    : IFinanceDocumentStatusReader
{
    public async Task<FinanceRemoteEventState> GetStatusAsync(int orgGkey,
        string sourceSystem, string sourceEventId,
        CancellationToken cancellationToken)
    {
        if (!context.Database.IsSqlServer())
            throw new InvalidOperationException(
                "FinanceTracker status lookup requires SQL Server.");

        var connection = context.Database.GetDbConnection();
        var closeConnection = connection.State != ConnectionState.Open;
        if (closeConnection)
            await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT STATUS
                FROM FinanceTracker.dbo.FIN_DOCUMENT
                WHERE ORG_GKEY = @org
                  AND SOURCE_SYSTEM = @sourceSystem
                  AND SOURCE_EVENT_ID = @sourceEventId;
                """;
            SetCurrentTransaction(command);
            Add(command, "@org", orgGkey);
            Add(command, "@sourceSystem", sourceSystem);
            Add(command, "@sourceEventId", sourceEventId);

            var statuses = new List<string>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                statuses.Add(reader.GetString(0));

            if (statuses.Count == 0)
                return FinanceRemoteEventState.NotPosted;
            // More than one matching document is an unexpected data
            // condition; treat it the same as "cannot determine" rather
            // than guessing which row is authoritative.
            if (statuses.Count > 1)
                return FinanceRemoteEventState.Unavailable;

            return string.Equals(statuses[0], "POSTED", StringComparison.OrdinalIgnoreCase)
                ? FinanceRemoteEventState.Posted
                : FinanceRemoteEventState.NotPosted;
        }
        catch (DbException)
        {
            return FinanceRemoteEventState.Unavailable;
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