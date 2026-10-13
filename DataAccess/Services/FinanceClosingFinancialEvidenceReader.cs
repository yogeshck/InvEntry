using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Data.Common;

namespace DataAccess.Services;

public sealed record FinanceClosingMovement(
    string Direction, string PaymentMode, decimal Amount);

public sealed record FinanceClosingInvoiceRequirement(
    int InvoiceGkey, string InvoiceNumber, DateOnly InvoiceDate,
    string SourceEventId, IReadOnlyList<FinanceClosingMovement> Movements);

public enum FinanceClosingEvidenceDecision
{
    Verified,
    Missing,
    Ambiguous,
    Mismatched
}

internal sealed record FinanceClosingDocumentEvidence(
    long Gkey, string Status);

internal sealed record FinanceClosingManualEvidence(
    long Gkey, DateOnly TransactionDate, string? DocumentNumber,
    string? Description, string Status, string Direction,
    string PaymentMode, decimal Amount);

public interface IFinanceClosingFinancialEvidenceReader
{
    Task<IReadOnlyDictionary<int, FinanceClosingEvidenceDecision>> ReadAsync(
        int orgGkey, int locationGkey,
        IReadOnlyList<FinanceClosingInvoiceRequirement> requirements,
        CancellationToken cancellationToken);
}

internal sealed class FinanceClosingFinancialEvidenceReader(
    MijmsContext context) : IFinanceClosingFinancialEvidenceReader
{
    public async Task<IReadOnlyDictionary<int, FinanceClosingEvidenceDecision>> ReadAsync(
        int orgGkey, int locationGkey,
        IReadOnlyList<FinanceClosingInvoiceRequirement> requirements,
        CancellationToken cancellationToken)
    {
        if (requirements.Count == 0)
            return new Dictionary<int, FinanceClosingEvidenceDecision>();
        if (!context.Database.IsSqlServer())
            throw new InvalidOperationException(
                "FinanceTracker financial evidence requires SQL Server.");

        var connection = context.Database.GetDbConnection();
        var closeConnection = connection.State != ConnectionState.Open;
        if (closeConnection)
            await connection.OpenAsync(cancellationToken);

        try
        {
            var documents = await ReadDocumentsAsync(connection, orgGkey,
                locationGkey, requirements, cancellationToken);
            var manual = await ReadManualTransactionsAsync(connection, orgGkey,
                locationGkey, requirements, cancellationToken);

            return requirements.ToDictionary(x => x.InvoiceGkey, x =>
                EvaluateInvoice(x,
                    documents.GetValueOrDefault(x.SourceEventId) ?? [],
                    manual.GetValueOrDefault(x.InvoiceNumber) ?? []));
        }
        finally
        {
            if (closeConnection)
                await connection.CloseAsync();
        }
    }

    internal static FinanceClosingEvidenceDecision EvaluateInvoice(
        FinanceClosingInvoiceRequirement requirement,
        IReadOnlyList<FinanceClosingDocumentEvidence> documents,
        IReadOnlyList<FinanceClosingManualEvidence> manualTransactions)
    {
        if (documents.Count > 1 || documents.Count == 1 && manualTransactions.Count > 0)
            return FinanceClosingEvidenceDecision.Ambiguous;

        if (documents.Count == 1)
            return string.Equals(documents[0].Status, "POSTED",
                StringComparison.OrdinalIgnoreCase)
                ? FinanceClosingEvidenceDecision.Verified
                : FinanceClosingEvidenceDecision.Mismatched;

        if (manualTransactions.Count == 0)
            return FinanceClosingEvidenceDecision.Missing;

        if (manualTransactions.Any(x =>
                x.TransactionDate != requirement.InvoiceDate ||
                !string.Equals(x.Status, "POSTED", StringComparison.OrdinalIgnoreCase)))
            return FinanceClosingEvidenceDecision.Mismatched;

        static string Key(string direction, string paymentMode, decimal amount) =>
            $"{direction.Trim().ToUpperInvariant()}|" +
            $"{paymentMode.Trim().ToUpperInvariant()}|{amount:0.00}";

        var required = requirement.Movements
            .GroupBy(x => Key(x.Direction, x.PaymentMode, x.Amount))
            .ToDictionary(x => x.Key, x => x.Count(), StringComparer.Ordinal);
        var actual = manualTransactions
            .GroupBy(x => Key(x.Direction, x.PaymentMode, x.Amount))
            .ToDictionary(x => x.Key, x => x.Count(), StringComparer.Ordinal);

        if (actual.Any(x => !required.ContainsKey(x.Key)))
            return FinanceClosingEvidenceDecision.Mismatched;
        if (actual.Any(x => x.Value > required[x.Key]))
            return FinanceClosingEvidenceDecision.Ambiguous;

        return required.Count == actual.Count && required.All(x =>
            actual.TryGetValue(x.Key, out var count) && count == x.Value)
            ? FinanceClosingEvidenceDecision.Verified
            : FinanceClosingEvidenceDecision.Mismatched;
    }

    private async Task<Dictionary<string, List<FinanceClosingDocumentEvidence>>>
        ReadDocumentsAsync(DbConnection connection, int orgGkey, int locationGkey,
            IReadOnlyList<FinanceClosingInvoiceRequirement> requirements,
            CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        SetCurrentTransaction(command);
        Add(command, "@org", orgGkey);
        Add(command, "@location", locationGkey);
        var names = AddList(command, "event", requirements.Select(x => x.SourceEventId));
        command.CommandText = $"""
            SELECT SOURCE_EVENT_ID, GKEY, STATUS
            FROM FinanceTracker.dbo.FIN_DOCUMENT
            WHERE ORG_GKEY = @org AND LOCATION_GKEY = @location
              AND SOURCE_EVENT_ID IN ({names});
            """;

        var result = new Dictionary<string, List<FinanceClosingDocumentEvidence>>(
            StringComparer.Ordinal);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var eventId = reader.GetString(0);
            if (!result.TryGetValue(eventId, out var rows))
                result[eventId] = rows = [];
            rows.Add(new(Convert.ToInt64(reader.GetValue(1)), reader.GetString(2)));
        }
        return result;
    }

    private async Task<Dictionary<string, List<FinanceClosingManualEvidence>>>
        ReadManualTransactionsAsync(DbConnection connection, int orgGkey,
            int locationGkey,
            IReadOnlyList<FinanceClosingInvoiceRequirement> requirements,
            CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        SetCurrentTransaction(command);
        Add(command, "@org", orgGkey);
        Add(command, "@location", locationGkey);
        Add(command, "@from", requirements.Min(x => x.InvoiceDate).ToDateTime(TimeOnly.MinValue));
        Add(command, "@through", requirements.Max(x => x.InvoiceDate).AddDays(1)
            .ToDateTime(TimeOnly.MinValue));
        var names = AddList(command, "invoice", requirements.Select(x => x.InvoiceNumber));
        command.CommandText = $"""
            SELECT T.GKEY, T.TRANSACTION_DATE, T.DOCUMENT_NO, T.DESCRIPTION,
                   T.STATUS, C.DIRECTION, P.MODE_CODE, T.AMOUNT
            FROM FinanceTracker.dbo.FIN_TRANSACTION T
            JOIN FinanceTracker.dbo.TRANSACTION_CATEGORY C
              ON C.GKEY = T.CATEGORY_GKEY
            JOIN FinanceTracker.dbo.PAYMENT_MODE P
              ON P.GKEY = T.PAYMENT_MODE_GKEY
            WHERE T.ORG_GKEY = @org AND T.LOCATION_GKEY = @location
              AND T.IS_MANUAL_ENTRY = 1
              AND T.TRANSACTION_DATE >= @from AND T.TRANSACTION_DATE < @through
              AND (LTRIM(RTRIM(ISNULL(T.DOCUMENT_NO, ''))) IN ({names})
                   OR LTRIM(RTRIM(ISNULL(T.DESCRIPTION, ''))) IN ({names}));
            """;

        var requirementsByNumber = requirements.ToDictionary(x => x.InvoiceNumber,
            StringComparer.OrdinalIgnoreCase);
        var result = new Dictionary<string, List<FinanceClosingManualEvidence>>(
            StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var document = reader.IsDBNull(2) ? null : reader.GetString(2).Trim();
            var description = reader.IsDBNull(3) ? null : reader.GetString(3).Trim();
            var matchedInvoices = new[] { document, description }
                .Where(x => x != null && requirementsByNumber.ContainsKey(x))
                .Select(x => x!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var status = matchedInvoices.Count > 1
                ? "REFERENCE_CONFLICT"
                : reader.GetString(4);
            foreach (var invoiceNumber in matchedInvoices)
            {
                if (!result.TryGetValue(invoiceNumber, out var rows))
                    result[invoiceNumber] = rows = [];
                rows.Add(new(Convert.ToInt64(reader.GetValue(0)),
                    DateOnly.FromDateTime(reader.GetDateTime(1)), document, description,
                    status, reader.GetString(5), reader.GetString(6),
                    reader.GetDecimal(7)));
            }
        }
        return result;
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

    private static string AddList(DbCommand command, string prefix,
        IEnumerable<string> values)
    {
        var names = new List<string>();
        var index = 0;
        foreach (var value in values.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var name = $"@{prefix}{index++}";
            Add(command, name, value);
            names.Add(name);
        }
        return string.Join(",", names);
    }
}
