namespace InvEntry.Contracts.CustomerOrders;

public sealed class SaveCustomerOrderRequest
{
    public CustomerOrderSaveModel Header { get; set; } = new();

    public List<CustomerOrderLineSaveModel> Lines { get; set; } = [];

    public List<OldMetalTransactionSaveModel> OldMetalTransactions { get; set; } = [];

    public List<CustomerOrderReceiptSaveModel> Receipts  { get; set; } = [];

}