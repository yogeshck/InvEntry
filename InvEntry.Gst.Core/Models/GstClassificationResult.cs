using System.Collections.Generic;

namespace InvEntry.Gst.Core.Models
{
    public class GstClassificationResult
    {
        public GstSupplyType SupplyType { get; set; }

        public GstReturnCategory ReturnCategory { get; set; }

        public GstTaxType TaxType { get; set; }

        /// <summary>
        /// GSTR-1 table such as 4A, 5, 6A, 7, 8 etc.
        /// </summary>
        public string? Gstr1Table { get; set; }

        public bool IsReportable { get; set; }

        public bool IsValid { get; set; } = true;

        public bool IsRecipientRegistered { get; set; }

        public List<string> Errors { get; } = new();

        public List<string> Warnings { get; } = new();

        public bool HasErrors => Errors.Count > 0;

        public bool HasWarnings => Warnings.Count > 0;
    }
}