using System;

namespace InvEntry.Contracts.Gst;

/// <summary>
/// Current GST reporting fields of a staged GSTR-1 line together with
/// the source invoice/product data and suggested corrections.
/// </summary>
public sealed class Gstr1LineCorrectionResponse
{
    public long DocumentGkey { get; set; }

    public string DocumentNbr { get; set; } = string.Empty;

    public DateOnly DocumentDate { get; set; }

    public int LineNbr { get; set; }

    public string? Description { get; set; }

    public decimal Quantity { get; set; }

    public decimal TaxableValue { get; set; }

    // ---------------------------------------------------------
    // CURRENT STAGED VALUES
    // ---------------------------------------------------------

    public string? HsnCode { get; set; }

    public string? Uom { get; set; }

    public string? Uqc { get; set; }

    public decimal? GstQuantity { get; set; }

    // ---------------------------------------------------------
    // SOURCE INVOICE / PRODUCT
    // ---------------------------------------------------------

    public string? ProductName { get; set; }

    public string? ProductUom { get; set; }

    public string? ProductHsnCode { get; set; }

    public string? InvoiceHsnCode { get; set; }

    public decimal? NetWeight { get; set; }

    public decimal? GrossWeight { get; set; }

    // ---------------------------------------------------------
    // SUGGESTED VALUES
    // ---------------------------------------------------------

    public string? SuggestedHsnCode { get; set; }

    public string? SuggestedUom { get; set; }

    public string? SuggestedUqc { get; set; }

    public decimal? SuggestedGstQuantity { get; set; }

    public string? SuggestionNote { get; set; }
}
