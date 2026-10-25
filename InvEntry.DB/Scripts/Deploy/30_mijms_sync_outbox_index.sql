/*
    mijms database: unique index that stops the same invoice event being queued twice.
    The InvEntry readiness check requires it. Run after the 20261011 migration.
    Requires sqlcmd variable MijmsDatabaseName. Safe to re-run.
*/
USE [$(MijmsDatabaseName)];
GO
IF DB_NAME() <> N'$(MijmsDatabaseName)'
    THROW 51000, 'Outbox index must run in the mijms database.', 1;
GO
IF OBJECT_ID(N'dbo.FINANCE_SYNC_OUTBOX', N'U') IS NULL
    THROW 51001, 'dbo.FINANCE_SYNC_OUTBOX does not exist. Install the InvEntry schema first.', 1;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id = OBJECT_ID(N'dbo.FINANCE_SYNC_OUTBOX')
                 AND name = N'UX_FINANCE_SYNC_OUTBOX_SOURCE_EVENT')
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.FINANCE_SYNC_OUTBOX GROUP BY SOURCE_EVENT_ID HAVING COUNT(*) > 1)
        THROW 51002, 'Duplicate SOURCE_EVENT_ID values exist. Resolve them before creating the unique index.', 1;
    CREATE UNIQUE INDEX UX_FINANCE_SYNC_OUTBOX_SOURCE_EVENT
        ON dbo.FINANCE_SYNC_OUTBOX (SOURCE_EVENT_ID);
END
GO
