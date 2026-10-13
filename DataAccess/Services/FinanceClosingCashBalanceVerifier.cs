using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace DataAccess.Services;

public enum FinanceClosingCashBalanceState
{
    Open,
    Missing,
    ClosedOrFinalised,
    Ambiguous
}

public interface IFinanceClosingCashBalanceVerifier
{
    Task<FinanceClosingCashBalanceState> VerifyAsync(int orgGkey, int locationGkey,
        DateOnly businessDate, CancellationToken cancellationToken);
}

internal sealed class FinanceClosingCashBalanceVerifier(MijmsContext context)
    : IFinanceClosingCashBalanceVerifier
{
    public async Task<FinanceClosingCashBalanceState> VerifyAsync(int orgGkey,
        int locationGkey, DateOnly businessDate, CancellationToken cancellationToken)
    {
        if (!context.Database.IsSqlServer())
            throw new InvalidOperationException(
                "FinanceTracker cash-balance verification requires SQL Server.");

        var connection = context.Database.GetDbConnection();
        var closeConnection = connection.State != ConnectionState.Open;
        if (closeConnection)
            await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT STATUS, IS_FINALISED
                FROM FinanceTracker.dbo.FIN_CASH_BALANCE
                WHERE ORG_GKEY = @orgGkey
                  AND LOCATION_GKEY = @locationGkey
                  AND BALANCE_DATE = @businessDate;
                """;
            command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();

            var org = command.CreateParameter();
            org.ParameterName = "@orgGkey";
            org.Value = orgGkey;
            command.Parameters.Add(org);
            var location = command.CreateParameter();
            location.ParameterName = "@locationGkey";
            location.Value = locationGkey;
            command.Parameters.Add(location);
            var date = command.CreateParameter();
            date.ParameterName = "@businessDate";
            date.DbType = DbType.Date;
            date.Value = businessDate.ToDateTime(TimeOnly.MinValue);
            command.Parameters.Add(date);

            var rows = new List<(string Status, bool IsFinalised)>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                rows.Add((reader.GetString(0), reader.GetBoolean(1)));

            if (rows.Count == 0) return FinanceClosingCashBalanceState.Missing;
            if (rows.Count != 1) return FinanceClosingCashBalanceState.Ambiguous;
            return string.Equals(rows[0].Status, "OPEN", StringComparison.OrdinalIgnoreCase)
                && !rows[0].IsFinalised
                ? FinanceClosingCashBalanceState.Open
                : FinanceClosingCashBalanceState.ClosedOrFinalised;
        }
        finally
        {
            if (closeConnection)
                await connection.CloseAsync();
        }
    }
}
