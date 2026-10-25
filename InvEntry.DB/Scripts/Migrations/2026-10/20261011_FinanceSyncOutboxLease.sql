/*
    Phase 1 finance synchronization reliability.
    Review and run against the InvEntry database before deploying DataAccess.
    This script does not update existing records or statuses.
*/
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.FINANCE_SYNC_OUTBOX', 'CLAIM_TOKEN') IS NULL
    ALTER TABLE dbo.FINANCE_SYNC_OUTBOX
        ADD CLAIM_TOKEN varchar(64) NULL;

IF COL_LENGTH('dbo.FINANCE_SYNC_OUTBOX', 'CLAIM_EXPIRES_ON') IS NULL
    ALTER TABLE dbo.FINANCE_SYNC_OUTBOX
        ADD CLAIM_EXPIRES_ON datetime2(7) NULL;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.FINANCE_SYNC_OUTBOX')
      AND name = 'IX_FINANCE_SYNC_OUTBOX_DISPATCH_CLAIM')
BEGIN
    CREATE INDEX IX_FINANCE_SYNC_OUTBOX_DISPATCH_CLAIM
        ON dbo.FINANCE_SYNC_OUTBOX
            (STATUS, CLAIM_EXPIRES_ON, CREATED_ON)
        INCLUDE (GKEY, CLAIM_TOKEN);
END;

COMMIT TRANSACTION;
