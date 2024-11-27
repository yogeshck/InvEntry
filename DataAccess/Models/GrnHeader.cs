using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class GrnHeader
{
    public int Gkey { get; set; }

    public string? GrnNbr { get; set; }

    public DateTime? GrnDate { get; set; }

    public string? DocumentType { get; set; }

    public string? OrderNbr { get; set; }

    public DateTime? OrderDate { get; set; }

    public string? SupplierId { get; set; }

    public string? SupplierRefNbr { get; set; }

    public string? CustomerOrderNbr { get; set; }

    public string? DocumentRef { get; set; }

    public DateTime? DocumentDate { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedOn { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedOn { get; set; }

    public string? Status { get; set; }
}
