namespace InvEntry.Contracts.CustomerOrders;

public sealed class SaveCustomerOrderRequest
{
    public CustomerOrderSaveModel Header { get; set; } = new();

    public List<CustomerOrderLineSaveModel> Lines { get; set; } = [];
/*
    public List<OldMetalSaveModel> OldMetalTransactions { get; set; } = [];

    public List<ReceiptSaveModel> AdvanceReceipts { get; set; } = [];

*/
}