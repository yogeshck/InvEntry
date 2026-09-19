using System.Globalization;
using System.Text.RegularExpressions;
using DataAccess.Models;
using InvEntry.Contracts.Gst;
using InvEntry.Contracts.Invoices;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services;

public sealed class Gstr1DocumentsIssuedService : IGstr1DocumentsIssuedService
{
    private static readonly Regex TrailingNumber =
        new(@"^(?<series>.*?)(?<number>\d+)$", RegexOptions.Compiled);

    private readonly MijmsContext _context;

    public Gstr1DocumentsIssuedService(MijmsContext context)
    {
        _context = context;
    }

    public async Task<Gstr1DocumentsIssuedResponse> GetAsync(
        string supplierGstin,
        string returnPeriod,
        CancellationToken cancellationToken = default)
    {
        var normalizedGstin = ValidateSupplierGstin(supplierGstin);
        var (start, end) = ValidateReturnPeriod(returnPeriod);

        var currentCompanyGstin = await _context.OrgThisCompanyViews
            .AsNoTracking()
            .Where(x => x.ThisCompany == true)
            .Select(x => x.GstNbr)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.Equals(
                normalizedGstin,
                currentCompanyGstin?.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "SupplierGstin does not match the current company GSTIN.",
                nameof(supplierGstin));
        }

        var invoices = await _context.InvoiceHeaders
            .AsNoTracking()
            .Where(x =>
                x.InvDate >= start &&
                x.InvDate < end &&
                x.InvNbr != null &&
                x.InvNbr != string.Empty)
            .Select(x => new IssuedInvoice(
                x.InvNbr!,
                x.Status))
            .ToListAsync(cancellationToken);

        var rows = invoices
            .Where(x => InvoiceStatus.IsFinal(x.Status) || InvoiceStatus.IsCancelled(x.Status))
            .Select(x =>
            {
                var parsed = ParseNumber(x.Number);
                return new { Invoice = x, Parsed = parsed };
            })
            .GroupBy(x => x.Parsed.Series, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var ordered = group
                    .OrderBy(x => x.Parsed.NumericValue.HasValue ? 0 : 1)
                    .ThenBy(x => x.Parsed.NumericValue)
                    .ThenBy(x => x.Invoice.Number, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var cancelled = ordered.Count(x =>
                    InvoiceStatus.IsCancelled(x.Invoice.Status));

                return new Gstr1DocumentSeriesResponse
                {
                    DocumentType = "Sales Invoice",
                    NatureOfDocument = "Invoices for outward supply",
                    Series = group.Key,
                    FromNumber = ordered[0].Invoice.Number,
                    ToNumber = ordered[^1].Invoice.Number,
                    TotalIssued = ordered.Count,
                    Cancelled = cancelled
                };
            })
            .ToList();

        return new Gstr1DocumentsIssuedResponse
        {
            SupplierGstin = normalizedGstin,
            ReturnPeriod = returnPeriod,
            Series = rows
        };
    }

    private static string ValidateSupplierGstin(string supplierGstin)
    {
        if (string.IsNullOrWhiteSpace(supplierGstin))
        {
            throw new ArgumentException(
                "SupplierGstin is required.",
                nameof(supplierGstin));
        }

        return supplierGstin.Trim().ToUpperInvariant();
    }

    private static (DateTime Start, DateTime End) ValidateReturnPeriod(
        string returnPeriod)
    {
        if (string.IsNullOrWhiteSpace(returnPeriod) ||
            returnPeriod.Length != 6 ||
            !int.TryParse(
                returnPeriod,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var numericPeriod))
        {
            throw new ArgumentException(
                "ReturnPeriod must be a valid yyyyMM value.",
                nameof(returnPeriod));
        }

        var year = numericPeriod / 100;
        var month = numericPeriod % 100;

        if (year < 1 || month is < 1 or > 12)
        {
            throw new ArgumentException(
                "ReturnPeriod must be a valid yyyyMM value.",
                nameof(returnPeriod));
        }

        var start = new DateTime(year, month, 1);
        return (start, start.AddMonths(1));
    }

    private static ParsedNumber ParseNumber(string number)
    {
        var match = TrailingNumber.Match(number.Trim());

        if (!match.Success ||
            !long.TryParse(
                match.Groups["number"].Value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var numericValue))
        {
            return new ParsedNumber(number.Trim(), null);
        }

        return new ParsedNumber(
            match.Groups["series"].Value,
            numericValue);
    }

    private sealed record IssuedInvoice(string Number, string? Status);

    private sealed record ParsedNumber(string Series, long? NumericValue);
}
