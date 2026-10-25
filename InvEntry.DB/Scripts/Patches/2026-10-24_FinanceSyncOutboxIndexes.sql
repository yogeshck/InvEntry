/*
    Patch 2026-10-24: FINANCE_SYNC_OUTBOX indexes missing on databases restored from production.

    InvEntry fails at startup with "Finance synchronization schema is incompatible. Missing: index
    dbo.FINANCE_SYNC_OUTBOX.UX_FINANCE_SYNC_OUTBOX_SOURCE_EVENT, ...IX_FINANCE_SYNC_OUTBOX_DISPATCH_CLAIM"
    when the table exists without its indexes (005_Gap_Tables_And_Indexes skips existing tables).

    Changes data: no. Safe to re-run: yes. Applied to dev mijms on 2026-10-24.
    Run in the mijms database; the unique index fails if SOURCE_EVENT_ID has duplicates.
*/
SET NOCOUNT ON;

IF EXISTS (SELECT 1 FROM dbo.FINANCE_SYNC_OUTBOX GROUP BY SOURCE_EVENT_ID HAVING COUNT(*) > 1)
    THROW 51002, 'Duplicate SOURCE_EVENT_ID values exist. Resolve them before creating the unique index.', 1;

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id = OBJECT_ID('dbo.FINANCE_SYNC_OUTBOX')
                 AND name = 'UX_FINANCE_SYNC_OUTBOX_SOURCE_EVENT')
    CREATE UNIQUE NONCLUSTERED INDEX UX_FINANCE_SYNC_OUTBOX_SOURCE_EVENT
        ON dbo.FINANCE_SYNC_OUTBOX (SOURCE_EVENT_ID);

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id = OBJECT_ID('dbo.FINANCE_SYNC_OUTBOX')
                 AND name = 'IX_FINANCE_SYNC_OUTBOX_PENDING')
    CREATE NONCLUSTERED INDEX IX_FINANCE_SYNC_OUTBOX_PENDING
        ON dbo.FINANCE_SYNC_OUTBOX (STATUS, CREATED_ON)
        INCLUDE (EVENT_TYPE, RETRY_COUNT, SOURCE_GKEY, SOURCE_TYPE);

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id = OBJECT_ID('dbo.FINANCE_SYNC_OUTBOX')
                 AND name = 'IX_FINANCE_SYNC_OUTBOX_DISPATCH_CLAIM')
    CREATE NONCLUSTERED INDEX IX_FINANCE_SYNC_OUTBOX_DISPATCH_CLAIM
        ON dbo.FINANCE_SYNC_OUTBOX (STATUS, CLAIM_EXPIRES_ON, CREATED_ON)
        INCLUDE (GKEY, CLAIM_TOKEN);

SELECT name FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.FINANCE_SYNC_OUTBOX');
