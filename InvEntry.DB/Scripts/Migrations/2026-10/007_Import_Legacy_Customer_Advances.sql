/* =========================================================
   007_Import_Legacy_Customer_Advances.sql

   Moves advances received before customer advance accounts
   existed into CUSTOMER_ADVANCE_ACCOUNT, so billing can use
   them with "Advance Adj".

   Source: VOUCHER rows of type 'Advance' / 'Advance Receipt'
   dated on or after @CutoffDate that no ADVANCE_LEDGER row
   points to yet.

   Old "Advance Adj" lines on FINALISED invoices from the same
   date that were not posted through an advance account (i.e.
   advances already used up) are subtracted per customer when
   @NetLegacyAdjustments = 1. Only the remaining balance is
   brought in.

   For each customer with something left:
     - credits their ACTIVE OPEN advance account, or opens one
       (ADV-nnnnnn, IS_OPENING_BALANCE = 1);
     - adds one OPENING ledger row pointing at their latest
       legacy advance voucher.
   No VOUCHER is created: the cash was already booked when the
   original voucher was saved.

   Run with @Apply = 0 first and check the preview. Running it
   again skips customers already imported
   (SOURCE_REF = OPENING-LEGACY-ADV-<customer gkey>).
   ========================================================= */

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;   -- filtered indexes on the advance tables

DECLARE @CutoffDate            DATE        = '2025-04-01';  -- first voucher date to bring in
DECLARE @Apply                 BIT         = 0;             -- 0 = preview only, 1 = write
DECLARE @NetLegacyAdjustments  BIT         = 1;             -- subtract advances already adjusted on invoices
DECLARE @CreatedBy             VARCHAR(50) = 'LEGACY-ADVANCE-IMPORT';

DECLARE @AdvanceLedgerGkey INT =
    (SELECT TOP (1) GKEY FROM dbo.MTBL_LEDGERS WHERE LEDGER_ACCOUNT_CODE = 1000 ORDER BY GKEY);

IF @AdvanceLedgerGkey IS NULL
BEGIN
    RAISERROR('An MTBL_LEDGERS row with account code 1000 (Advance) is required.', 16, 1);
    RETURN;
END;


/* ---------------------------------------------------------
   Legacy advance receipts
   --------------------------------------------------------- */

IF OBJECT_ID('tempdb..#LegacyReceipt') IS NOT NULL DROP TABLE #LegacyReceipt;

SELECT  v.gkey            AS VOUCHER_GKEY,
        v.voucher_nbr     AS VOUCHER_NBR,
        v.voucher_date    AS VOUCHER_DATE,
        v.customer_gkey   AS CUST_GKEY,
        v.mode            AS MODE,
        v.trans_amount    AS AMOUNT,
        v.ref_doc_nbr     AS REF_DOC_NBR
INTO    #LegacyReceipt
FROM    dbo.VOUCHER v
WHERE   v.voucher_type IN ('Advance', 'Advance Receipt')
  AND   v.trans_type = 'Receipt'
  AND   v.customer_gkey IS NOT NULL
  AND   v.trans_amount > 0
  AND   v.voucher_date >= @CutoffDate
  AND   NOT EXISTS (SELECT 1 FROM dbo.ADVANCE_LEDGER l WHERE l.VOUCHER_GKEY = v.gkey);


/* ---------------------------------------------------------
   Advances already used on invoices outside advance accounts
   --------------------------------------------------------- */

IF OBJECT_ID('tempdb..#LegacyUsage') IS NOT NULL DROP TABLE #LegacyUsage;

SELECT  h.CUST_GKEY,
        h.GKEY            AS INVOICE_GKEY,
        h.INV_NBR,
        h.INV_DATE,
        r.adjusted_amount AS AMOUNT
INTO    #LegacyUsage
FROM    dbo.INVOICE_AR_RECEIPTS r
JOIN    dbo.INVOICE_HEADER h ON h.GKEY = r.invoice_gkey
WHERE   @NetLegacyAdjustments = 1
  AND   r.transaction_type = 'Advance Adj'
  AND   r.adjusted_amount > 0
  AND   h.STATUS = 'FINALISED'
  AND   h.INV_DATE >= @CutoffDate
  AND   h.CUST_GKEY IN (SELECT CUST_GKEY FROM #LegacyReceipt)
  AND   NOT EXISTS (SELECT 1 FROM dbo.ADVANCE_LEDGER l
                    WHERE l.INVOICE_GKEY = h.GKEY AND l.ENTRY_TYPE = 'ADJUSTMENT');


/* ---------------------------------------------------------
   Balance per customer
   --------------------------------------------------------- */

IF OBJECT_ID('tempdb..#Opening') IS NOT NULL DROP TABLE #Opening;

SELECT  c.CUST_GKEY,
        c.RECEIVED,
        ISNULL(u.USED, 0)                          AS USED,
        c.RECEIVED - ISNULL(u.USED, 0)             AS OPENING,
        c.FIRST_DATE,
        c.LATEST_VOUCHER_GKEY,
        c.LATEST_DATE,
        c.VOUCHER_NBRS,
        acc.GKEY                                   AS EXISTING_ACCOUNT_GKEY,
        CASE
            WHEN EXISTS (SELECT 1 FROM dbo.ADVANCE_LEDGER l
                         WHERE l.SOURCE_REF = CONCAT('OPENING-LEGACY-ADV-', c.CUST_GKEY))
                THEN 'ALREADY IMPORTED'
            WHEN c.RECEIVED - ISNULL(u.USED, 0) <= 0
                THEN 'NOTHING LEFT'
            ELSE 'IMPORT'
        END                                        AS OUTCOME
INTO    #Opening
FROM (
        SELECT  CUST_GKEY,
                SUM(AMOUNT)                         AS RECEIVED,
                CAST(MIN(VOUCHER_DATE) AS DATE)     AS FIRST_DATE,
                MAX(VOUCHER_GKEY)                   AS LATEST_VOUCHER_GKEY,
                CAST(MAX(VOUCHER_DATE) AS DATE)     AS LATEST_DATE,
                STRING_AGG(VOUCHER_NBR, ', ')       AS VOUCHER_NBRS
        FROM    #LegacyReceipt
        GROUP BY CUST_GKEY
     ) c
LEFT JOIN (SELECT CUST_GKEY, SUM(AMOUNT) AS USED FROM #LegacyUsage GROUP BY CUST_GKEY) u
       ON u.CUST_GKEY = c.CUST_GKEY
LEFT JOIN dbo.CUSTOMER_ADVANCE_ACCOUNT acc
       ON acc.CUST_GKEY = c.CUST_GKEY AND acc.ACCOUNT_TYPE = 'OPEN' AND acc.STATUS = 'ACTIVE';


/* ---------------------------------------------------------
   Preview
   --------------------------------------------------------- */

SELECT  o.OUTCOME, o.CUST_GKEY, cu.CUSTOMER_NAME, cu.MOBILE_NBR,
        o.RECEIVED, o.USED, o.OPENING, o.VOUCHER_NBRS,
        CASE WHEN o.EXISTING_ACCOUNT_GKEY IS NULL THEN 'new ADV account' ELSE 'credit existing OPEN account' END AS TARGET
FROM    #Opening o
LEFT JOIN dbo.ORG_CUSTOMER cu ON cu.GKEY = o.CUST_GKEY
ORDER BY o.OUTCOME, cu.CUSTOMER_NAME;

SELECT  'Legacy receipts' AS SECTION, VOUCHER_NBR AS DOC_NBR, CAST(VOUCHER_DATE AS DATE) AS DOC_DATE,
        CUST_GKEY, AMOUNT, REF_DOC_NBR AS REFERENCE
FROM    #LegacyReceipt
UNION ALL
SELECT  'Already adjusted', INV_NBR, CAST(INV_DATE AS DATE), CUST_GKEY, AMOUNT, NULL
FROM    #LegacyUsage
ORDER BY CUST_GKEY, DOC_DATE;

SELECT  COUNT(*)                                             AS CUSTOMERS_TO_IMPORT,
        ISNULL(SUM(OPENING), 0)                              AS AMOUNT_TO_IMPORT,
        (SELECT COUNT(*) FROM #Opening WHERE OUTCOME = 'NOTHING LEFT')     AS NOTHING_LEFT,
        (SELECT COUNT(*) FROM #Opening WHERE OUTCOME = 'ALREADY IMPORTED') AS ALREADY_IMPORTED
FROM    #Opening
WHERE   OUTCOME = 'IMPORT';

IF @Apply = 0
BEGIN
    PRINT 'Preview only. Set @Apply = 1 to import.';
    RETURN;
END;


/* ---------------------------------------------------------
   Import
   --------------------------------------------------------- */

BEGIN TRANSACTION;

DECLARE @Now DATETIME2(7) = SYSDATETIME();
DECLARE @NewAccount TABLE (GKEY INT NOT NULL, CUST_GKEY INT NOT NULL);

-- Customers without an active OPEN account get one. The number needs the
-- identity, so a temporary unique number is replaced after insert (as the
-- application does).
INSERT INTO dbo.CUSTOMER_ADVANCE_ACCOUNT
       (ACCOUNT_NBR, ACCOUNT_TYPE, CUST_GKEY, MTBL_LEDGERS_GKEY, STATUS, OPENED_ON,
        IS_OPENING_BALANCE, REMARKS, CREATED_BY, CREATED_ON)
OUTPUT inserted.GKEY, inserted.CUST_GKEY INTO @NewAccount (GKEY, CUST_GKEY)
SELECT CONCAT('TMP-', CONVERT(VARCHAR(36), NEWID())), 'OPEN', o.CUST_GKEY, @AdvanceLedgerGkey,
       'ACTIVE', o.FIRST_DATE, 1,
       LEFT(CONCAT('Legacy advances from ', CONVERT(VARCHAR(10), @CutoffDate, 120), ': ', o.VOUCHER_NBRS), 250),
       @CreatedBy, @Now
FROM   #Opening o
WHERE  o.OUTCOME = 'IMPORT'
  AND  o.EXISTING_ACCOUNT_GKEY IS NULL;

UPDATE a
SET    ACCOUNT_NBR = CONCAT('ADV-', RIGHT(CONCAT('000000', a.GKEY), 6))
FROM   dbo.CUSTOMER_ADVANCE_ACCOUNT a
JOIN   @NewAccount n ON n.GKEY = a.GKEY;

UPDATE o
SET    EXISTING_ACCOUNT_GKEY = n.GKEY
FROM   #Opening o
JOIN   @NewAccount n ON n.CUST_GKEY = o.CUST_GKEY;

INSERT INTO dbo.ADVANCE_LEDGER
       (ADVANCE_ACCOUNT_GKEY, ENTRY_TYPE, ENTRY_DATE, AMOUNT, WEIGHT, VOUCHER_GKEY,
        SOURCE_REF, NARRATION, CREATED_BY, CREATED_ON)
SELECT o.EXISTING_ACCOUNT_GKEY, 'OPENING', o.LATEST_DATE, o.OPENING, 0, o.LATEST_VOUCHER_GKEY,
       CONCAT('OPENING-LEGACY-ADV-', o.CUST_GKEY),
       LEFT(CONCAT('Legacy advance ', o.VOUCHER_NBRS,
                   ' Rs.', FORMAT(o.RECEIVED, 'N2'),
                   CASE WHEN o.USED > 0 THEN CONCAT(' less adjusted Rs.', FORMAT(o.USED, 'N2')) ELSE '' END), 250),
       @CreatedBy, @Now
FROM   #Opening o
WHERE  o.OUTCOME = 'IMPORT';

UPDATE a
SET    BALANCE_AMOUNT        = a.BALANCE_AMOUNT + o.OPENING,
       TOTAL_RECEIVED_AMOUNT = a.TOTAL_RECEIVED_AMOUNT + o.OPENING,
       MODIFIED_BY           = @CreatedBy,
       MODIFIED_ON           = @Now
FROM   dbo.CUSTOMER_ADVANCE_ACCOUNT a
JOIN   #Opening o ON o.EXISTING_ACCOUNT_GKEY = a.GKEY
WHERE  o.OUTCOME = 'IMPORT';

COMMIT TRANSACTION;

SELECT a.ACCOUNT_NBR, a.CUST_GKEY, cu.CUSTOMER_NAME, a.BALANCE_AMOUNT, a.IS_OPENING_BALANCE
FROM   dbo.CUSTOMER_ADVANCE_ACCOUNT a
JOIN   #Opening o ON o.EXISTING_ACCOUNT_GKEY = a.GKEY AND o.OUTCOME = 'IMPORT'
LEFT JOIN dbo.ORG_CUSTOMER cu ON cu.GKEY = a.CUST_GKEY
ORDER BY cu.CUSTOMER_NAME;
