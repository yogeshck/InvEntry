-- Present on production (no script in the repos); added to the project from the production snapshot, 2026-10-26.
CREATE NONCLUSTERED INDEX [IX_GST_GSTR1_DOCUMENT_DATE]
    ON [dbo].[GST_GSTR1_DOCUMENT]([DOCUMENT_DATE] ASC);
GO
