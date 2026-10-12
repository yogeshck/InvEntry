namespace DataAccess.Models;

public sealed class FinanceClosingFence
{
    public long Gkey { get; set; }
    public int OrgGkey { get; set; }
    public int LocationGkey { get; set; }
    public DateOnly BusinessDate { get; set; }
    public Guid ReservationId { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedOnUtc { get; set; }
    public DateTime ModifiedOnUtc { get; set; }
}
