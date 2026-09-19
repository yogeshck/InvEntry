using System;

namespace InvEntry.Gst.Core.Models
{
    public class GstClassificationRequest
    {
        // ---------------------------------------------------------
        // DOCUMENT
        // ---------------------------------------------------------

        public GstDocumentType DocumentType { get; set; }

        public string? DocumentNumber { get; set; }

        public DateTime DocumentDate { get; set; }

        // ---------------------------------------------------------
        // SUPPLIER / SELLER
        // ---------------------------------------------------------

        public string? SupplierGstin { get; set; }

        public string? SupplierStateCode { get; set; }

        // ---------------------------------------------------------
        // RECIPIENT / BUYER
        // ---------------------------------------------------------

        public string? RecipientGstin { get; set; }

        public string? RecipientStateCode { get; set; }

        public string? PlaceOfSupplyCode { get; set; }

        // ---------------------------------------------------------
        // VALUES
        // ---------------------------------------------------------

        public decimal TaxableValue { get; set; }

        public decimal InvoiceValue { get; set; }

        public decimal CgstAmount { get; set; }

        public decimal SgstAmount { get; set; }

        public decimal IgstAmount { get; set; }

        public decimal CessAmount { get; set; }

        // ---------------------------------------------------------
        // GST CHARACTERISTICS
        // ---------------------------------------------------------

        public GstTaxTreatment TaxTreatment { get; set; }
            = GstTaxTreatment.Taxable;

        public bool IsExport { get; set; }

        public bool IsSez { get; set; }

        public bool IsReverseCharge { get; set; }

        public bool IsDeemedExport { get; set; }

        public bool IsEcommerceSupply { get; set; }

        public string? EcommerceOperatorGstin { get; set; }

        // ---------------------------------------------------------
        // AMENDMENT
        // ---------------------------------------------------------

        public bool IsAmendment { get; set; }

        public string? OriginalDocumentNumber { get; set; }

        public DateTime? OriginalDocumentDate { get; set; }
    }
}