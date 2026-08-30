using InvEntry.Contracts.Invoices;
using System;

namespace InvEntry.Store;

public sealed class InvoiceEditSession
{
    public InvoiceEditResponse? Draft { get; private set; }

    public bool HasDraft =>
        Draft is not null;

    public void SetDraft(
        InvoiceEditResponse draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        Draft = draft;
    }

    public InvoiceEditResponse? TakeDraft()
    {
        var draft = Draft;

        Draft = null;

        return draft;
    }

    public void Clear()
    {
        Draft = null;
    }
}