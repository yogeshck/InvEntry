/*
    InvEntry Database Migration
    File: 003_Migrate_Ledger_Opening_Balances.sql
    Purpose:
        Moves existing customer Advance (MTBL_LEDGERS account code 1000) and
        Recurring Deposit (account code 3000) balances into
        CUSTOMER_ADVANCE_ACCOUNT, each with one OPENING row in ADVANCE_LEDGER.

    Why there are two balances to choose from:
        LEDGERS_HEADER.CURRENT_BALANCE is updated by the Cash Receipt screen, but
        LedgerHelper.ProcessInvoiceAdvanceAsync (which reduces it when an
        "Advance Adj" / "RD Adj" is used on an invoice) is commented out. So the
        ledger balance is usually too high for customers who already used their
        advance. This script therefore shows, per customer and ledger:

          LEDGER_BALANCE    SUM(LEDGERS_HEADER.CURRENT_BALANCE)
          RECEIPTS          receipt vouchers into the ledger (VOUCHER)
          ADJUSTMENTS       INVOICE_HEADER.ADVANCE_ADJ / RD_AMOUNT_ADJ on finalised invoices
          COMPUTED_BALANCE  RECEIPTS - ADJUSTMENTS
          DIFFERENCE        LEDGER_BALANCE - COMPUTED_BALANCE

        Review the preview, settle the differences with the business, then run
        with @Apply = 1 and the chosen @BalanceSource. Refunds of advances made
        outside invoices are not visible here and must be checked manually.

    Customer / ledger pairs with an ISSUE are never migrated:
        NO_CUSTOMER         customer gkey not found in ORG_CUSTOMER
        NEGATIVE_BALANCE    chosen balance is below zero
        ZERO_BALANCE        nothing to migrate
        ALREADY_MIGRATED    an OPENING row already exists for this pair

    Run after 002_Add_Customer_Advance_And_Schemes.sql.
    Take a database backup before running with @Apply = 1.

      sqlcmd -S <server> -d mijms -E -I -b -i 003_Migrate_Ledger_Opening_Balances.sql

    Date: 2026-10-20

    Safe to rerun: Yes (pairs already migrated are skipped; preview changes nothing)
*/

SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

DECLARE
    @Apply          BIT          = 0,            -- 0 = preview only, 1 = create accounts and opening rows
    @BalanceSource  VARCHAR (10) = 'COMPUTED',   -- 'COMPUTED' or 'LEDGER'
    @OpeningDate    DATE         = CAST(SYSDATETIME() AS DATE),
    @MigratedBy     VARCHAR (50) = 'MIGRATION-003';

DECLARE
    @AdvanceLedgerGkey INT,
    @RdLedgerGkey      INT;

IF @BalanceSource NOT IN ('COMPUTED', 'LEDGER')
    THROW 52001, '@BalanceSource must be COMPUTED or LEDGER.', 1;

IF OBJECT_ID(N'dbo.CUSTOMER_ADVANCE_ACCOUNT', N'U') IS NULL OR OBJECT_ID(N'dbo.ADVANCE_LEDGER', N'U') IS NULL
    THROW 52002, 'Run 002_Add_Customer_Advance_And_Schemes.sql first.', 1;

IF (SELECT COUNT(*) FROM dbo.MTBL_LEDGERS WHERE LEDGER_ACCOUNT_CODE = 1000) <> 1
   OR (SELECT COUNT(*) FROM dbo.MTBL_LEDGERS WHERE LEDGER_ACCOUNT_CODE = 3000) <> 1
    THROW 52003, 'Exactly one MTBL_LEDGERS row is required for account codes 1000 (Advance) and 3000 (RD).', 1;

SELECT @AdvanceLedgerGkey = GKEY FROM dbo.MTBL_LEDGERS WHERE LEDGER_ACCOUNT_CODE = 1000;
SELECT @RdLedgerGkey      = GKEY FROM dbo.MTBL_LEDGERS WHERE LEDGER_ACCOUNT_CODE = 3000;


/* ---------------------------------------------------------
   1. Customer / ledger pairs seen in LEDGERS_HEADER or in receipt vouchers
   --------------------------------------------------------- */

IF OBJECT_ID('tempdb..#ReceiptVouchers') IS NOT NULL DROP TABLE #ReceiptVouchers;
IF OBJECT_ID('tempdb..#Opening') IS NOT NULL DROP TABLE #Opening;

SELECT
    V.gkey          AS VOUCHER_GKEY,
    V.customer_gkey AS CUST_GKEY,
    CASE
        WHEN V.to_ledger_gkey IN (@AdvanceLedgerGkey, @RdLedgerGkey) THEN V.to_ledger_gkey
        WHEN V.to_ledger_gkey IS NULL AND V.voucher_type = 'Advance Receipt' THEN @AdvanceLedgerGkey
        WHEN V.to_ledger_gkey IS NULL AND V.voucher_type = 'Recurring Deposit' THEN @RdLedgerGkey
    END             AS MTBL_LEDGERS_GKEY,
    V.trans_amount  AS AMOUNT
INTO #ReceiptVouchers
FROM dbo.VOUCHER V
WHERE V.trans_type = 'Receipt'
  AND V.customer_gkey IS NOT NULL
  AND ISNULL(V.trans_amount, 0) > 0
  AND (   V.to_ledger_gkey IN (@AdvanceLedgerGkey, @RdLedgerGkey)
       OR (V.to_ledger_gkey IS NULL AND V.voucher_type IN ('Advance Receipt', 'Recurring Deposit')));

;WITH Pairs AS (
    SELECT H.CUST_GKEY, H.MTBL_LEDGERS_GKEY
    FROM dbo.LEDGERS_HEADER H
    WHERE H.CUST_GKEY IS NOT NULL
      AND H.MTBL_LEDGERS_GKEY IN (@AdvanceLedgerGkey, @RdLedgerGkey)
    UNION
    SELECT R.CUST_GKEY, R.MTBL_LEDGERS_GKEY
    FROM #ReceiptVouchers R
)
SELECT
    P.CUST_GKEY,
    C.CUSTOMER_NAME,
    C.MOBILE_NBR,
    C.TENANT_GKEY,
    C.LOCATION_GKEY,
    P.MTBL_LEDGERS_GKEY,
    CASE P.MTBL_LEDGERS_GKEY WHEN @AdvanceLedgerGkey THEN 'OPEN' ELSE 'SCHEME' END AS ACCOUNT_TYPE,
    CASE P.MTBL_LEDGERS_GKEY WHEN @AdvanceLedgerGkey THEN 1000 ELSE 3000 END       AS LEDGER_ACCOUNT_CODE,
    LH.LEDGER_BALANCE,
    LH.LEDGER_HEADER_COUNT,
    LH.LEGACY_LEDGERS_HEADER_GKEY,
    ISNULL(RV.RECEIPTS, 0)    AS RECEIPTS,
    ADJ.ADJUSTMENTS,
    ISNULL(RV.RECEIPTS, 0) - ADJ.ADJUSTMENTS AS COMPUTED_BALANCE,
    CONCAT('OB-', CASE P.MTBL_LEDGERS_GKEY WHEN @AdvanceLedgerGkey THEN 'ADV' ELSE 'RD' END, '-', P.CUST_GKEY) AS ACCOUNT_NBR,
    CONCAT('OPENING-', P.CUST_GKEY, '-', CASE P.MTBL_LEDGERS_GKEY WHEN @AdvanceLedgerGkey THEN 1000 ELSE 3000 END) AS SOURCE_REF,
    CAST(NULL AS DECIMAL (18, 2)) AS CHOSEN_BALANCE,
    CAST(NULL AS VARCHAR (20))    AS ISSUE
INTO #Opening
FROM Pairs P
LEFT JOIN dbo.ORG_CUSTOMER C ON C.GKEY = P.CUST_GKEY
OUTER APPLY (
    SELECT SUM(ISNULL(H.CURRENT_BALANCE, 0)) AS LEDGER_BALANCE,
           COUNT(*)                          AS LEDGER_HEADER_COUNT,
           -- Only kept when there is a single header, so the unique legacy index holds.
           CASE WHEN COUNT(*) = 1 THEN MIN(H.GKEY) END AS LEGACY_LEDGERS_HEADER_GKEY
    FROM dbo.LEDGERS_HEADER H
    WHERE H.CUST_GKEY = P.CUST_GKEY AND H.MTBL_LEDGERS_GKEY = P.MTBL_LEDGERS_GKEY
) LH
OUTER APPLY (
    SELECT SUM(R.AMOUNT) AS RECEIPTS
    FROM #ReceiptVouchers R
    WHERE R.CUST_GKEY = P.CUST_GKEY AND R.MTBL_LEDGERS_GKEY = P.MTBL_LEDGERS_GKEY
) RV
OUTER APPLY (
    SELECT ISNULL(SUM(ISNULL(I.ADVANCE_ADJ, 0)), 0)   AS ADVANCE_ADJUSTMENTS,
           ISNULL(SUM(ISNULL(I.RD_AMOUNT_ADJ, 0)), 0) AS RD_ADJUSTMENTS
    FROM dbo.INVOICE_HEADER I
    WHERE I.CUST_GKEY = P.CUST_GKEY
      AND I.STATUS IN ('FINAL', 'FINALISED')
) INV
CROSS APPLY (
    SELECT CASE P.MTBL_LEDGERS_GKEY
               WHEN @AdvanceLedgerGkey THEN INV.ADVANCE_ADJUSTMENTS
               ELSE INV.RD_ADJUSTMENTS
           END AS ADJUSTMENTS
) ADJ;

UPDATE #Opening
SET CHOSEN_BALANCE = CASE @BalanceSource WHEN 'LEDGER' THEN ISNULL(LEDGER_BALANCE, 0) ELSE COMPUTED_BALANCE END;

UPDATE O
SET ISSUE = CASE
        WHEN O.CUSTOMER_NAME IS NULL AND NOT EXISTS (SELECT 1 FROM dbo.ORG_CUSTOMER C WHERE C.GKEY = O.CUST_GKEY)
            THEN 'NO_CUSTOMER'
        WHEN EXISTS (SELECT 1 FROM dbo.ADVANCE_LEDGER L WHERE L.SOURCE_REF = O.SOURCE_REF)
            THEN 'ALREADY_MIGRATED'
        WHEN O.CHOSEN_BALANCE < 0 THEN 'NEGATIVE_BALANCE'
        WHEN O.CHOSEN_BALANCE = 0 THEN 'ZERO_BALANCE'
    END
FROM #Opening O;


/* ---------------------------------------------------------
   2. Preview
   --------------------------------------------------------- */

SELECT
    @BalanceSource AS BALANCE_SOURCE,
    LEDGER_ACCOUNT_CODE,
    COUNT(*)                                                 AS CUSTOMERS,
    SUM(CASE WHEN ISSUE IS NULL THEN 1 ELSE 0 END)            AS TO_MIGRATE,
    SUM(CASE WHEN ISSUE IS NULL THEN CHOSEN_BALANCE ELSE 0 END) AS AMOUNT_TO_MIGRATE,
    SUM(ISNULL(LEDGER_BALANCE, 0))                           AS TOTAL_LEDGER_BALANCE,
    SUM(COMPUTED_BALANCE)                                    AS TOTAL_COMPUTED_BALANCE,
    SUM(CASE WHEN ISNULL(LEDGER_BALANCE, 0) <> COMPUTED_BALANCE THEN 1 ELSE 0 END) AS CUSTOMERS_WITH_DIFFERENCE
FROM #Opening
GROUP BY LEDGER_ACCOUNT_CODE
ORDER BY LEDGER_ACCOUNT_CODE;

SELECT
    CUST_GKEY, CUSTOMER_NAME, MOBILE_NBR, LEDGER_ACCOUNT_CODE, ACCOUNT_NBR,
    LEDGER_BALANCE, LEDGER_HEADER_COUNT, RECEIPTS, ADJUSTMENTS, COMPUTED_BALANCE,
    ISNULL(LEDGER_BALANCE, 0) - COMPUTED_BALANCE AS DIFFERENCE,
    CHOSEN_BALANCE, ISSUE
FROM #Opening
ORDER BY CASE WHEN ISNULL(LEDGER_BALANCE, 0) <> COMPUTED_BALANCE THEN 0 ELSE 1 END,
         LEDGER_ACCOUNT_CODE, CUSTOMER_NAME;

IF @Apply = 0
BEGIN
    PRINT 'Preview only. Set @Apply = 1 to migrate the rows without an ISSUE.';
    RETURN;
END;


/* ---------------------------------------------------------
   3. Apply
   --------------------------------------------------------- */

BEGIN TRY
    BEGIN TRANSACTION;

    INSERT dbo.CUSTOMER_ADVANCE_ACCOUNT (
        TENANT_GKEY, LOCATION_GKEY, ACCOUNT_NBR, ACCOUNT_TYPE, CUST_GKEY, MTBL_LEDGERS_GKEY,
        STATUS, OPENED_ON, BALANCE_AMOUNT, BALANCE_WEIGHT, TOTAL_RECEIVED_AMOUNT, TOTAL_RECEIVED_WEIGHT,
        IS_OPENING_BALANCE, LEGACY_LEDGERS_HEADER_GKEY, REMARKS, CREATED_BY)
    SELECT
        O.TENANT_GKEY, O.LOCATION_GKEY, O.ACCOUNT_NBR, O.ACCOUNT_TYPE, O.CUST_GKEY, O.MTBL_LEDGERS_GKEY,
        'ACTIVE', @OpeningDate, O.CHOSEN_BALANCE, 0, O.CHOSEN_BALANCE, 0,
        1, O.LEGACY_LEDGERS_HEADER_GKEY,
        CONCAT('Opening balance (', @BalanceSource, '); ledger ', ISNULL(O.LEDGER_BALANCE, 0),
               ', computed ', O.COMPUTED_BALANCE),
        @MigratedBy
    FROM #Opening O
    WHERE O.ISSUE IS NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.CUSTOMER_ADVANCE_ACCOUNT A
                      WHERE A.ACCOUNT_NBR = O.ACCOUNT_NBR
                        AND (A.TENANT_GKEY = O.TENANT_GKEY OR (A.TENANT_GKEY IS NULL AND O.TENANT_GKEY IS NULL)));

    INSERT dbo.ADVANCE_LEDGER (
        ADVANCE_ACCOUNT_GKEY, ENTRY_TYPE, ENTRY_DATE, AMOUNT, WEIGHT, SOURCE_REF, NARRATION, CREATED_BY)
    SELECT
        A.GKEY, 'OPENING', @OpeningDate, O.CHOSEN_BALANCE, 0, O.SOURCE_REF,
        CONCAT('Opening balance migrated from ledger account ', O.LEDGER_ACCOUNT_CODE),
        @MigratedBy
    FROM #Opening O
    JOIN dbo.CUSTOMER_ADVANCE_ACCOUNT A
      ON A.ACCOUNT_NBR = O.ACCOUNT_NBR
     AND (A.TENANT_GKEY = O.TENANT_GKEY OR (A.TENANT_GKEY IS NULL AND O.TENANT_GKEY IS NULL))
    WHERE O.ISSUE IS NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.ADVANCE_LEDGER L WHERE L.SOURCE_REF = O.SOURCE_REF);

    IF EXISTS (SELECT 1 FROM dbo.CUSTOMER_ADVANCE_BALANCE_CHECK_VIEW)
        THROW 52004, 'Cached balance does not match the ledger after migration.', 1;

    COMMIT TRANSACTION;

    SELECT
        LEDGER_ACCOUNT_CODE,
        COUNT(*)            AS ACCOUNTS_CREATED,
        SUM(CHOSEN_BALANCE) AS AMOUNT_MIGRATED
    FROM #Opening
    WHERE ISSUE IS NULL
    GROUP BY LEDGER_ACCOUNT_CODE;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
