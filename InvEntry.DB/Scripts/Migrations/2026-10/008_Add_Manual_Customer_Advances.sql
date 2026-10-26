/* =========================================================
   008_Add_Manual_Customer_Advances.sql

   Adds advances kept in the manual register as opening
   balances, so billing can use them with "Advance Adj".

   Fill @Advance with mobile number, amount and the date the
   advance was received, then run. For each row:
     - credits the customer's ACTIVE OPEN advance account, or
       opens one (ADV-nnnnnn, IS_OPENING_BALANCE = 1);
     - adds an OPENING row to ADVANCE_LEDGER on that date.
   No VOUCHER is created (no cash is booked today).

   A row is rejected if the mobile number matches no customer
   or more than one. Re-running the same mobile + date + amount
   is skipped (SOURCE_REF), so the script can be run again.
   ========================================================= */

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;   -- filtered indexes on the advance tables

DECLARE @Apply     BIT         = 0;   -- 0 = preview only, 1 = write
DECLARE @CreatedBy VARCHAR(50) = 'MANUAL-ADVANCE-ENTRY';

DECLARE @Advance TABLE (
    MOBILE_NBR   VARCHAR(15)    NOT NULL,
    AMOUNT       DECIMAL(18, 2) NOT NULL,
    RECEIVED_ON  DATE           NOT NULL,
    REMARKS      VARCHAR(150)   NULL
);

INSERT INTO @Advance (MOBILE_NBR, AMOUNT, RECEIVED_ON, REMARKS) VALUES
    ('9000000000', 25000.00, '2025-06-15', 'Manual register page 12');
--  ('9000000001', 10000.00, '2025-07-01', NULL),


DECLARE @AdvanceLedgerGkey INT =
    (SELECT TOP (1) GKEY FROM dbo.MTBL_LEDGERS WHERE LEDGER_ACCOUNT_CODE = 1000 ORDER BY GKEY);

IF @AdvanceLedgerGkey IS NULL
BEGIN
    RAISERROR('An MTBL_LEDGERS row with account code 1000 (Advance) is required.', 16, 1);
    RETURN;
END;


/* ---------------------------------------------------------
   Match customers and check each row
   --------------------------------------------------------- */

IF OBJECT_ID('tempdb..#Entry') IS NOT NULL DROP TABLE #Entry;

SELECT  a.MOBILE_NBR,
        a.AMOUNT,
        a.RECEIVED_ON,
        a.REMARKS,
        m.CUST_GKEY,
        m.CUSTOMER_NAME,
        acc.GKEY          AS ACCOUNT_GKEY,
        acc.ACCOUNT_NBR,
        CONCAT('OPENING-MANUAL-', LTRIM(RTRIM(a.MOBILE_NBR)), '-',
               CONVERT(VARCHAR(8), a.RECEIVED_ON, 112), '-', a.AMOUNT) AS SOURCE_REF,
        CASE
            WHEN a.AMOUNT <= 0                     THEN 'AMOUNT MUST BE > 0'
            WHEN a.RECEIVED_ON > CAST(GETDATE() AS DATE) THEN 'DATE IS IN THE FUTURE'
            WHEN m.MATCHES = 0 OR m.MATCHES IS NULL THEN 'NO CUSTOMER WITH THIS MOBILE'
            WHEN m.MATCHES > 1                     THEN 'MORE THAN ONE CUSTOMER WITH THIS MOBILE'
            ELSE 'ADD'
        END AS OUTCOME
INTO    #Entry
FROM    @Advance a
OUTER APPLY (
        SELECT  MIN(c.GKEY)          AS CUST_GKEY,
                MIN(c.CUSTOMER_NAME) AS CUSTOMER_NAME,
                COUNT(*)             AS MATCHES
        FROM    dbo.ORG_CUSTOMER c
        WHERE   LTRIM(RTRIM(c.MOBILE_NBR)) = LTRIM(RTRIM(a.MOBILE_NBR))
     ) m
LEFT JOIN dbo.CUSTOMER_ADVANCE_ACCOUNT acc
       ON acc.CUST_GKEY = m.CUST_GKEY AND m.MATCHES = 1
      AND acc.ACCOUNT_TYPE = 'OPEN' AND acc.STATUS = 'ACTIVE';

UPDATE e
SET    OUTCOME = 'ALREADY ADDED'
FROM   #Entry e
WHERE  e.OUTCOME = 'ADD'
  AND  EXISTS (SELECT 1 FROM dbo.ADVANCE_LEDGER l WHERE l.SOURCE_REF = e.SOURCE_REF);

SELECT  OUTCOME, MOBILE_NBR, CUSTOMER_NAME, CUST_GKEY, AMOUNT, RECEIVED_ON, REMARKS,
        ISNULL(ACCOUNT_NBR, CASE WHEN OUTCOME = 'ADD' THEN '(new ADV account)' END) AS ACCOUNT
FROM    #Entry
ORDER BY OUTCOME, CUSTOMER_NAME;

IF @Apply = 0
BEGIN
    PRINT 'Preview only. Set @Apply = 1 to add the rows marked ADD.';
    RETURN;
END;


/* ---------------------------------------------------------
   Write
   --------------------------------------------------------- */

BEGIN TRANSACTION;

DECLARE @Now DATETIME2(7) = SYSDATETIME();
DECLARE @NewAccount TABLE (GKEY INT NOT NULL, CUST_GKEY INT NOT NULL);

-- One new OPEN account per customer that has none. The number needs the
-- identity, so a temporary unique number is replaced after insert.
INSERT INTO dbo.CUSTOMER_ADVANCE_ACCOUNT
       (ACCOUNT_NBR, ACCOUNT_TYPE, CUST_GKEY, MTBL_LEDGERS_GKEY, STATUS, OPENED_ON,
        IS_OPENING_BALANCE, REMARKS, CREATED_BY, CREATED_ON)
OUTPUT inserted.GKEY, inserted.CUST_GKEY INTO @NewAccount (GKEY, CUST_GKEY)
SELECT CONCAT('TMP-', CONVERT(VARCHAR(36), NEWID())), 'OPEN', e.CUST_GKEY, @AdvanceLedgerGkey,
       'ACTIVE', MIN(e.RECEIVED_ON), 1, 'Manual advance register', @CreatedBy, @Now
FROM   #Entry e
WHERE  e.OUTCOME = 'ADD' AND e.ACCOUNT_GKEY IS NULL
GROUP BY e.CUST_GKEY;

UPDATE a
SET    ACCOUNT_NBR = CONCAT('ADV-', RIGHT(CONCAT('000000', a.GKEY), 6))
FROM   dbo.CUSTOMER_ADVANCE_ACCOUNT a
JOIN   @NewAccount n ON n.GKEY = a.GKEY;

UPDATE e
SET    ACCOUNT_GKEY = n.GKEY
FROM   #Entry e
JOIN   @NewAccount n ON n.CUST_GKEY = e.CUST_GKEY
WHERE  e.OUTCOME = 'ADD';

INSERT INTO dbo.ADVANCE_LEDGER
       (ADVANCE_ACCOUNT_GKEY, ENTRY_TYPE, ENTRY_DATE, AMOUNT, WEIGHT,
        SOURCE_REF, NARRATION, CREATED_BY, CREATED_ON)
SELECT e.ACCOUNT_GKEY, 'OPENING', e.RECEIVED_ON, e.AMOUNT, 0,
       e.SOURCE_REF,
       LEFT(CONCAT('Manual advance received ', CONVERT(VARCHAR(10), e.RECEIVED_ON, 103),
                   CASE WHEN e.REMARKS IS NOT NULL THEN CONCAT(' - ', e.REMARKS) ELSE '' END), 250),
       @CreatedBy, @Now
FROM   #Entry e
WHERE  e.OUTCOME = 'ADD';

UPDATE a
SET    BALANCE_AMOUNT        = a.BALANCE_AMOUNT + t.AMOUNT,
       TOTAL_RECEIVED_AMOUNT = a.TOTAL_RECEIVED_AMOUNT + t.AMOUNT,
       MODIFIED_BY           = @CreatedBy,
       MODIFIED_ON           = @Now
FROM   dbo.CUSTOMER_ADVANCE_ACCOUNT a
JOIN  (SELECT ACCOUNT_GKEY, SUM(AMOUNT) AS AMOUNT
       FROM   #Entry WHERE OUTCOME = 'ADD'
       GROUP BY ACCOUNT_GKEY) t ON t.ACCOUNT_GKEY = a.GKEY;

COMMIT TRANSACTION;

SELECT a.ACCOUNT_NBR, cu.CUSTOMER_NAME, cu.MOBILE_NBR, a.BALANCE_AMOUNT
FROM   dbo.CUSTOMER_ADVANCE_ACCOUNT a
JOIN   dbo.ORG_CUSTOMER cu ON cu.GKEY = a.CUST_GKEY
WHERE  a.GKEY IN (SELECT ACCOUNT_GKEY FROM #Entry WHERE OUTCOME = 'ADD')
ORDER BY cu.CUSTOMER_NAME;
