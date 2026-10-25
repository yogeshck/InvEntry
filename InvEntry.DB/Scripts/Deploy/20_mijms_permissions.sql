/*
    mijms database: read rights for the FinanceTracker API pool.
    The transactions and invoice-sync screens read VOUCHER, INVOICE_HEADER and FINANCE_SYNC_OUTBOX.
    Requires sqlcmd variable MijmsDatabaseName. Safe to re-run.
*/
USE [$(MijmsDatabaseName)];
GO
IF DB_NAME() <> N'$(MijmsDatabaseName)'
    THROW 51000, 'mijms permissions must run in the mijms database.', 1;
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'IIS APPPOOL\FinanceTrackerPool')
    CREATE USER [IIS APPPOOL\FinanceTrackerPool] FOR LOGIN [IIS APPPOOL\FinanceTrackerPool];
GO
GRANT SELECT ON SCHEMA::dbo TO [IIS APPPOOL\FinanceTrackerPool];
GO
