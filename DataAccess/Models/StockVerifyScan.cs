using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class StockVerifyScan
{
    public int Gkey { get; set; }

    public int SessionId { get; set; }

    public string Barcode { get; set; } = null!;

    public DateTime ScanTime { get; set; }
}
