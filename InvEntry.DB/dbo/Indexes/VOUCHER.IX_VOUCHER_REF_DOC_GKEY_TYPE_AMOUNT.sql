CREATE NONCLUSTERED INDEX [IX_VOUCHER_REF_DOC_GKEY_TYPE_AMOUNT]
    ON [dbo].[VOUCHER]([ref_doc_gkey] ASC, [voucher_type] ASC, [trans_amount] ASC);
GO
