using InvEntry.Contracts.Schemes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace InvEntry.Helpers;

/// <summary>
/// Reads the RD register pasted from Excel (tab separated) or a CSV file.
/// Columns are found by their header names, so their order does not matter;
/// blank rows are skipped.
/// </summary>
public static class SchemeImportSheetParser
{
    private static readonly string[] DateFormats =
    {
        "dd-MMM-yyyy", "d-MMM-yyyy", "dd-MMM-yy", "d-MMM-yy",
        "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy", "yyyy-MM-dd", "dd.MM.yyyy"
    };

    public sealed record ParseResult<T>(List<T> Rows, List<string> Problems);

    public static ParseResult<SchemeImportScheme> ParseSchemes(string? text)
    {
        var rows = new List<SchemeImportScheme>();
        var problems = new List<string>();
        var table = ReadTable(text, problems, "RD master");

        if (table is null)
            return new(rows, problems);

        var (header, lines) = table.Value;
        var id = Find(header, "rd id", "rdid", "rd no");
        var mobile = Find(header, "mobile", "customer");
        var tenure = Find(header, "duration", "tenure", "months");
        var monthly = Find(header, "monthly");
        var start = Find(header, "start");
        var maturityDate = Find(header, "maturity date");
        var bonus = Find(header, "bonus");
        var maturityAmount = Find(header, "maturity amount");
        var status = Find(header, "status");

        Require(problems, "RD master", ("RD ID", id), ("Monthly Amount", monthly), ("Start Date", start));
        if (id < 0 || monthly < 0 || start < 0)
            return new(rows, problems);

        foreach (var (number, cells) in lines)
        {
            var legacyId = Cell(cells, id);
            if (string.IsNullOrWhiteSpace(legacyId))
                continue;

            rows.Add(new SchemeImportScheme
            {
                RowNumber = number,
                LegacyId = legacyId.Trim(),
                MobileNumber = Clean(Cell(cells, mobile)),
                TenureMonths = (int)(Amount(Cell(cells, tenure), problems, number, "Scheme Duration") ?? 12M),
                MonthlyAmount = Amount(Cell(cells, monthly), problems, number, "Monthly Amount") ?? 0M,
                StartDate = Date(Cell(cells, start), problems, number, "Start Date"),
                MaturityDate = Date(Cell(cells, maturityDate), problems, number, "Maturity Date"),
                BonusAmount = Amount(Cell(cells, bonus), problems, number, "Bonus Amount"),
                MaturityAmount = Amount(Cell(cells, maturityAmount), problems, number, "Maturity Amount"),
                Status = Clean(Cell(cells, status))
            });
        }

        return new(rows, problems);
    }

    public static ParseResult<SchemeImportPayment> ParsePayments(string? text)
    {
        var rows = new List<SchemeImportPayment>();
        var problems = new List<string>();
        var table = ReadTable(text, problems, "payment ledger");

        if (table is null)
            return new(rows, problems);

        var (header, lines) = table.Value;
        var id = Find(header, "rd id", "rdid", "rd no");
        var mobile = Find(header, "customer", "mobile");
        var tranId = Find(header, "tran", "utr", "reference");
        var due = Find(header, "due");
        var paid = Find(header, "paid date", "receipt date", "date paid");
        var amount = Find(header, "amount");
        var mode = Find(header, "mode");
        var receipt = Find(header, "receipt n", "receipt", "rct");

        Require(problems, "payment ledger", ("RD ID", id), ("Paid Date", paid), ("Amount Paid", amount), ("Receipt Number", receipt));
        if (id < 0 || paid < 0 || amount < 0 || receipt < 0)
            return new(rows, problems);

        foreach (var (number, cells) in lines)
        {
            var legacyId = Cell(cells, id);
            if (string.IsNullOrWhiteSpace(legacyId))
                continue;

            rows.Add(new SchemeImportPayment
            {
                RowNumber = number,
                LegacyId = legacyId.Trim(),
                MobileNumber = Clean(Cell(cells, mobile)),
                TransactionId = Clean(Cell(cells, tranId)),
                DueDate = Date(Cell(cells, due), problems, number, "Due Date"),
                PaidDate = Date(Cell(cells, paid), problems, number, "Paid Date"),
                Amount = Amount(Cell(cells, amount), problems, number, "Amount Paid") ?? 0M,
                Mode = Clean(Cell(cells, mode)),
                ReceiptNumber = Clean(Cell(cells, receipt))
            });
        }

        return new(rows, problems);
    }

    private static (string[] Header, List<(int Number, string[] Cells)> Lines)? ReadTable(
        string? text,
        List<string> problems,
        string sheet)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            problems.Add($"Paste the {sheet}, including its header row.");
            return null;
        }

        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var separator = lines.Any(x => x.Contains('\t')) ? '\t' : ',';

        var headerIndex = Array.FindIndex(lines, x => !string.IsNullOrWhiteSpace(x));
        var header = lines[headerIndex].Split(separator).Select(x => x.Trim()).ToArray();

        var data = new List<(int, string[])>();
        for (var i = headerIndex + 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i].Replace(separator.ToString(), string.Empty)))
                continue;

            data.Add((i + 1, lines[i].Split(separator)));
        }

        return (header, data);
    }

    private static int Find(string[] header, params string[] names)
    {
        foreach (var name in names)
        {
            var index = Array.FindIndex(header, x =>
                x.Replace("_", " ").Contains(name, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
                return index;
        }

        return -1;
    }

    private static void Require(List<string> problems, string sheet, params (string Name, int Index)[] columns)
    {
        foreach (var (name, index) in columns.Where(x => x.Index < 0))
            problems.Add($"The {sheet} has no '{name}' column.");
    }

    private static string? Cell(string[] cells, int index) =>
        index >= 0 && index < cells.Length ? cells[index] : null;

    private static string? Clean(string? value)
    {
        var trimmed = value?.Trim().Trim('"');
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static decimal? Amount(string? value, List<string> problems, int row, string column)
    {
        var text = Clean(value)?.Replace(",", string.Empty).Replace("₹", string.Empty).Trim();
        if (text is null)
            return null;

        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            return amount;

        problems.Add($"Row {row}: {column} '{value!.Trim()}' is not a number.");
        return null;
    }

    private static DateOnly? Date(string? value, List<string> problems, int row, string column)
    {
        var text = Clean(value);
        if (text is null)
            return null;

        if (DateTime.TryParseExact(text, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return DateOnly.FromDateTime(date);

        // Excel can also paste a date as its serial number.
        if (double.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var serial) && serial > 20000 && serial < 80000)
            return DateOnly.FromDateTime(DateTime.FromOADate(serial));

        problems.Add($"Row {row}: {column} '{text}' is not a date (use 17-Jul-2024).");
        return null;
    }
}
