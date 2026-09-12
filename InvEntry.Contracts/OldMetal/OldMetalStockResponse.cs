namespace InvEntry.Contracts.OldMetal;

public sealed class OldMetalStockResponse
{
    public int ProductGkey { get; set; }

    public string? ProductId { get; set; }

    public string? Metal { get; set; }

    public string? Purity { get; set; }

    public decimal CurrentNetWeight { get; set; }
}
