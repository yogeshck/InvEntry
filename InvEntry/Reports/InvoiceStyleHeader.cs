using DevExpress.Drawing;
using DevExpress.XtraPrinting;
using DevExpress.XtraPrinting.Drawing;
using DevExpress.XtraReports.UI;
using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

namespace InvEntry.Reports;

/// <summary>
/// The company header of the invoice (<see cref="InvPrint25"/>): name, tagline,
/// address, phones and email on the left, the invoice's own logo and GSTIN on
/// the right, and the gold rule underneath. Used by code-built reports so
/// receipts and statements look like the invoice.
/// </summary>
public static class InvoiceStyleHeader
{
    private const float LogoWidth = 100F;
    private const float LogoHeight = 70F;

    private static readonly Lazy<ImageSource?> InvoiceLogo = new(LoadInvoiceLogo);

    /// <summary>Adds the header at (x, y) across the given width; returns the y below it.</summary>
    public static float Add(Band band, float x, float y, float width, OrgThisCompanyView? company)
    {
        if (company is null)
            return y;

        var top = y;
        var blockWidth = width - LogoWidth - 10F;

        y = AddLine(band, company.CompanyName, "Segoe UI", 12F, DXFontStyle.Bold, Color.RoyalBlue, x, y, blockWidth, 22F);
        y = AddLine(band, company.Tagline, "Times New Roman", 8F, DXFontStyle.Bold, Color.Blue, x, y, blockWidth, 15F);

        var address = Join(", ",
            company.AddressLine1, company.AddressLine2, company.AddressLine3,
            company.Area, company.District,
            Join("-", company.GstCode, company.State));
        y = AddLine(band, address, "Times New Roman", 8F, DXFontStyle.Regular, Color.Black, x, y, blockWidth, 15F, multiline: true);

        var phones = Join(", ", company.ContactNbr1, company.ContactNbr2);
        y = AddLine(band, phones.Length > 0 ? $"Ph: {phones}" : null, "Times New Roman", 8F, DXFontStyle.Regular, Color.Black, x, y, blockWidth, 15F);
        y = AddLine(band, string.IsNullOrWhiteSpace(company.EmailId) ? null : $"Email: {company.EmailId}",
            "Times New Roman", 8F, DXFontStyle.Regular, Color.Black, x, y, blockWidth, 15F);

        if (InvoiceLogo.Value is ImageSource logo)
        {
            band.Controls.Add(new XRPictureBox
            {
                ImageSource = logo,
                LocationF = new PointF(x + width - LogoWidth, top),
                SizeF = new SizeF(LogoWidth, LogoHeight),
                Sizing = ImageSizeMode.ZoomImage
            });
        }

        var gstTop = top + LogoHeight + 2F;
        if (!string.IsNullOrWhiteSpace(company.GstNbr))
        {
            band.Controls.Add(new XRLabel
            {
                LocationF = new PointF(x + width - 220F, gstTop),
                SizeF = new SizeF(220F, 15F),
                Text = $"GSTIN - {company.GstNbr}",
                Font = new DXFont("Arial", 8F, DXFontStyle.Bold),
                TextAlignment = TextAlignment.MiddleRight,
                Padding = new PaddingInfo(2, 2, 0, 0)
            });
        }

        y = Math.Max(y, gstTop + 15F);

        // Gold rule under the header, as on the invoice.
        band.Controls.Add(new XRLine
        {
            LocationF = new PointF(x, y + 2F),
            SizeF = new SizeF(width, 4F),
            LineWidth = 2F,
            ForeColor = Color.Gold
        });

        return y + 10F;
    }

    private static float AddLine(
        Band band,
        string? text,
        string fontName,
        float size,
        DXFontStyle style,
        Color color,
        float x,
        float y,
        float width,
        float height,
        bool multiline = false)
    {
        if (string.IsNullOrWhiteSpace(text))
            return y;

        var labelHeight = multiline ? height * 2F : height;

        band.Controls.Add(new XRLabel
        {
            LocationF = new PointF(x, y),
            SizeF = new SizeF(width, labelHeight),
            Text = text,
            Font = new DXFont(fontName, size, style),
            ForeColor = color,
            TextAlignment = multiline ? TextAlignment.TopLeft : TextAlignment.MiddleLeft,
            Multiline = multiline,
            WordWrap = multiline,
            CanShrink = true,
            Padding = new PaddingInfo(2, 2, 0, 0)
        });

        return y + labelHeight;
    }

    internal static string Join(string separator, params string?[] parts)
    {
        var list = new List<string>();
        foreach (var part in parts)
        {
            if (!string.IsNullOrWhiteSpace(part))
                list.Add(part.Trim());
        }

        return string.Join(separator, list);
    }

    /// <summary>The logo of the invoice report (InvPrint25's picture box).</summary>
    private static ImageSource? LoadInvoiceLogo()
    {
        try
        {
            var resources = new ComponentResourceManager(typeof(InvPrint25));
            var image = resources.GetString("xrPictureBox2.ImageSource");

            return string.IsNullOrWhiteSpace(image) ? null : new ImageSource("img", image);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Invoice logo could not be loaded for a code-built report");
            return null;
        }
    }
}
