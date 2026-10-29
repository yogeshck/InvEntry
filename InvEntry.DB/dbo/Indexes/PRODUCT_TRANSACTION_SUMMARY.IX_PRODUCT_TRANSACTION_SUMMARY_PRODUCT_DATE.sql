-- Present on production (no script in the repos); added to the project from the production snapshot, 2026-10-26.
CREATE NONCLUSTERED INDEX [IX_PRODUCT_TRANSACTION_SUMMARY_PRODUCT_DATE]
    ON [dbo].[PRODUCT_TRANSACTION_SUMMARY]([PRODUCT_GKEY] ASC, [TRANSACTION_DATE] ASC);
GO
