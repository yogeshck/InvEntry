namespace InvEntry.Contracts.CustomerOrders;

public sealed class SaveCustomerOrderResponse
{
    public int Gkey { get; set; }

    public string OrderNbr { get; set; } = string.Empty;

    public bool IsNew { get; set; }
}