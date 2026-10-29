/*
Production (mijms): let the finance sync outbox store every status the sync code writes.

CK_FINANCE_SYNC_OUTBOX_STATUS allowed only PENDING / PROCESSING / SENT / ERROR, but
FinanceSyncService writes RETRYABLE_ERROR, BUSINESS_ERROR and UNCERTAIN when an event
fails. Saving that status broke the constraint, so the whole background cycle failed:
no attempt or error was recorded, the 10 claimed events stayed claimed, and after the
2-minute lease the same 10 were claimed again. One failing event blocked every event
behind it (DataAccess log: "The UPDATE statement conflicted with the CHECK constraint
CK_FINANCE_SYNC_OUTBOX_STATUS").

After this runs, the background job picks the stuck events up within about 2-3 minutes
(once the current claims expire). Failing events are then recorded with their error
instead of blocking the rest. Safe to re-run.
*/

USE mijms;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF EXISTS (SELECT 1 FROM sys.check_constraints
           WHERE name = 'CK_FINANCE_SYNC_OUTBOX_STATUS'
             AND parent_object_id = OBJECT_ID('dbo.FINANCE_SYNC_OUTBOX'))
    ALTER TABLE dbo.FINANCE_SYNC_OUTBOX DROP CONSTRAINT CK_FINANCE_SYNC_OUTBOX_STATUS;

ALTER TABLE dbo.FINANCE_SYNC_OUTBOX WITH CHECK
    ADD CONSTRAINT CK_FINANCE_SYNC_OUTBOX_STATUS CHECK (
        [STATUS] = 'PENDING' OR [STATUS] = 'PROCESSING' OR [STATUS] = 'SENT' OR [STATUS] = 'ERROR'
        OR [STATUS] = 'RETRYABLE_ERROR' OR [STATUS] = 'BUSINESS_ERROR' OR [STATUS] = 'UNCERTAIN');

COMMIT TRANSACTION;
GO

SELECT name, definition, is_not_trusted
FROM sys.check_constraints
WHERE name = 'CK_FINANCE_SYNC_OUTBOX_STATUS';
GO
