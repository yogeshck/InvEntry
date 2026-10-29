-- Present on production (no script in the repos); added to the project from the production snapshot, 2026-10-26.
CREATE NONCLUSTERED INDEX [IX_PRODUCT_TRANSACTION_SUMMARY_DOCUMENT]
    ON [dbo].[PRODUCT_TRANSACTION_SUMMARY]([DOCUMENT_TYPE] ASC, [REF_LINE_GKEY] ASC);
GO
