-- Present on production (no script in the repos); added to the project from the production snapshot, 2026-10-26.
CREATE NONCLUSTERED INDEX [IX_GST_GSTR1_LINE_HSN]
    ON [dbo].[GST_GSTR1_DOCUMENT_LINE]([HSN_CODE] ASC, [GST_RATE] ASC);
GO
