using System;
using System.Collections.Generic;
using System.Linq;

namespace InvEntry.Contracts.Gst;

/// <summary>
/// GST Unit Quantity Codes (UQC) accepted by the GSTN portal
/// and the mapping from application UOM values to UQC.
/// </summary>
public static class GstUqc
{
    public static IReadOnlyList<Gstr1UqcOption> Options { get; } =
    [
        new("BAG", "Bags"),
        new("BAL", "Bale"),
        new("BDL", "Bundles"),
        new("BKL", "Buckles"),
        new("BOU", "Billion of Units"),
        new("BOX", "Box"),
        new("BTL", "Bottles"),
        new("BUN", "Bunches"),
        new("CAN", "Cans"),
        new("CBM", "Cubic Meters"),
        new("CCM", "Cubic Centimeters"),
        new("CMS", "Centimeters"),
        new("CTN", "Cartons"),
        new("DOZ", "Dozens"),
        new("DRM", "Drums"),
        new("GGK", "Great Gross"),
        new("GMS", "Grammes"),
        new("GRS", "Gross"),
        new("GYD", "Gross Yards"),
        new("KGS", "Kilograms"),
        new("KLR", "Kilolitre"),
        new("KME", "Kilometre"),
        new("LTR", "Litres"),
        new("MLT", "Millilitre"),
        new("MTR", "Meters"),
        new("MTS", "Metric Ton"),
        new("NOS", "Numbers"),
        new("OTH", "Others"),
        new("PAC", "Packs"),
        new("PCS", "Pieces"),
        new("PRS", "Pairs"),
        new("QTL", "Quintal"),
        new("ROL", "Rolls"),
        new("SET", "Sets"),
        new("SQF", "Square Feet"),
        new("SQM", "Square Meters"),
        new("SQY", "Square Yards"),
        new("TBS", "Tablets"),
        new("TGM", "Ten Gross"),
        new("THD", "Thousands"),
        new("TON", "Tonnes"),
        new("TUB", "Tubes"),
        new("UGS", "US Gallons"),
        new("UNT", "Units"),
        new("YDS", "Yards")
    ];

    private static readonly HashSet<string> Codes =
        Options.Select(x => x.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);

    // Only unambiguous application UOM spellings are mapped.
    // Anything else must be chosen explicitly by the operator.
    private static readonly Dictionary<string, string> UomToUqc =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Grams"] = "GMS",
            ["Gram"] = "GMS",
            ["Gms"] = "GMS",
            ["Gm"] = "GMS",
            ["Kilograms"] = "KGS",
            ["Kilogram"] = "KGS",
            ["Kgs"] = "KGS",
            ["Kg"] = "KGS",
            ["Pieces"] = "PCS",
            ["Piece"] = "PCS",
            ["Pcs"] = "PCS",
            ["Pc"] = "PCS",
            ["Numbers"] = "NOS",
            ["Number"] = "NOS",
            ["Nos"] = "NOS",
            ["No"] = "NOS",
            ["Sets"] = "SET",
            ["Set"] = "SET",
            ["Pairs"] = "PRS",
            ["Pair"] = "PRS",
            ["Prs"] = "PRS"
        };

    public static bool IsValid(string? uqc) =>
        !string.IsNullOrWhiteSpace(uqc) && Codes.Contains(uqc.Trim());

    /// <summary>
    /// Returns the UQC for a known application UOM, or null when the
    /// UOM has no unambiguous mapping.
    /// </summary>
    public static string? MapFromUom(string? uom) =>
        !string.IsNullOrWhiteSpace(uom) && UomToUqc.TryGetValue(uom.Trim(), out var uqc)
            ? uqc
            : null;

    /// <summary>
    /// GST quantity for a UQC: net weight for weight units (jewellery is
    /// valued on net weight), otherwise the business quantity.
    /// Returns null when no positive source value is available.
    /// </summary>
    public static decimal? SuggestGstQuantity(string? uqc, decimal quantity, decimal? netWeight)
    {
        var weight = netWeight is > 0M ? netWeight : null;

        return uqc?.Trim().ToUpperInvariant() switch
        {
            null or "" => null,
            "GMS" => weight,
            "KGS" => weight is null ? null : Math.Round(weight.Value / 1000M, 3),
            _ => quantity > 0M ? quantity : null
        };
    }

    /// <summary>
    /// True when the UOM maps to a known UQC that differs from the given UQC.
    /// </summary>
    public static bool IsMismatch(string? uom, string? uqc)
    {
        var expected = MapFromUom(uom);
        return expected is not null &&
            !string.IsNullOrWhiteSpace(uqc) &&
            !string.Equals(expected, uqc.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record Gstr1UqcOption(string Code, string Description)
{
    public string Display => $"{Code} - {Description}";
}
