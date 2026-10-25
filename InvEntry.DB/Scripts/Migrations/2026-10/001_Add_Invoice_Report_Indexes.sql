/*
    InvEntry Database Migration
    File: 003_Add_Invoice_Report_Indexes.sql
    Purpose:
        Improve Invoice Print Preview / PDF generation performance.

    Date: 2026-10-05

    Safe to rerun: Yes
*/

SET NOCOUNT ON;
GO


/* =========================================================
   INVOICE_HEADER
   ========================================================= */

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_INVOICE_HEADER_INV_NBR'
      AND object_id = OBJECT_ID('dbo.INVOICE_HEADER')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_INVOICE_HEADER_INV_NBR
        ON dbo.INVOICE_HEADER (INV_NBR);
END;
GO


/* =========================================================
   INVOICE_LINE
   ========================================================= */

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_INVOICE_LINE_INVOICE_ID_METAL'
      AND object_id = OBJECT_ID('dbo.INVOICE_LINE')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_INVOICE_LINE_INVOICE_ID_METAL
        ON dbo.INVOICE_LINE (INVOICE_ID, METAL);
END;
GO


/* =========================================================
   OLD_METAL_TRANSACTION
   ========================================================= */

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_OLD_METAL_TRANSACTION_DOC_REF_GKEY'
      AND object_id = OBJECT_ID('dbo.OLD_METAL_TRANSACTION')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_OLD_METAL_TRANSACTION_DOC_REF_GKEY
        ON dbo.OLD_METAL_TRANSACTION (DOC_REF_GKEY);
END;
GO


/* =========================================================
   INVOICE_AR_RECEIPTS
   ========================================================= */

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_INVOICE_AR_RECEIPTS_INVOICE_NBR'
      AND object_id = OBJECT_ID('dbo.INVOICE_AR_RECEIPTS')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_INVOICE_AR_RECEIPTS_INVOICE_NBR
        ON dbo.INVOICE_AR_RECEIPTS (INVOICE_NBR);
END;
GO


/* =========================================================
   VOUCHER
   ========================================================= */

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_VOUCHER_REF_DOC_GKEY_TYPE_AMOUNT'
      AND object_id = OBJECT_ID('dbo.VOUCHER')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_VOUCHER_REF_DOC_GKEY_TYPE_AMOUNT
        ON dbo.VOUCHER
        (
            REF_DOC_GKEY,
            VOUCHER_TYPE,
            TRANS_AMOUNT
        );
END;
GO