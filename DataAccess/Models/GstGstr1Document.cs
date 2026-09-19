using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class GstGstr1Document
{
    public long Gkey { get; set; }

    public int SourceGkey { get; set; }

    public string DocumentType { get; set; } = null!;

    public string DocumentNbr { get; set; } = null!;

    public DateOnly DocumentDate { get; set; }

    public string SupplierGstin { get; set; } = null!;

    public string ReturnPeriod { get; set; } = null!;

    public string? RecipientGstin { get; set; }

    public string? RecipientStateCode { get; set; }

    public bool IsRecipientRegistered { get; set; }

    public string PlaceOfSupplyCode { get; set; } = null!;

    public string SupplyType { get; set; } = null!;

    public string TaxType { get; set; } = null!;

    public string ReturnCategory { get; set; } = null!;

    public string? Gstr1Table { get; set; }

    public bool IsReportable { get; set; }

    public decimal InvoiceValue { get; set; }

    public decimal TaxableValue { get; set; }

    public decimal CgstAmount { get; set; }

    public decimal SgstAmount { get; set; }

    public decimal IgstAmount { get; set; }

    public decimal CessAmount { get; set; }

    public bool IsReverseCharge { get; set; }

    public bool IsSez { get; set; }

    public bool IsDeemedExport { get; set; }

    public bool IsEcommerceSupply { get; set; }

    public string? EcommerceOperatorGstin { get; set; }

    public bool IsAmendment { get; set; }

    public string? OriginalDocumentNbr { get; set; }

    public DateOnly? OriginalDocumentDate { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedOn { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public virtual ICollection<GstGstr1DocumentLine> GstGstr1DocumentLines { get; set; } = new List<GstGstr1DocumentLine>();
}
