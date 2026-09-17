namespace InvEntry.GST.Models
{
    public enum GstSupplyType
    {
        IntraState,
        InterState,
        Export
    }

    public enum GstReturnCategory
    {
        NotApplicable,

        B2B,
        B2CL,
        B2CS,

        Export,

        NilRated,
        Exempt,
        NonGst,

        CreditDebitNote
    }

    public enum GstTaxType
    {
        None,
        CgstSgst,
        Igst
    }

    public enum GstDocumentType
    {
        SalesInvoice,
        Estimate,
        BranchTransfer,
        CreditNote,
        DebitNote
    }

    public enum GstTaxTreatment
    {
        Taxable,
        NilRated,
        Exempt,
        NonGst
    }
}