/*
    Normalize verified completed INVOICE_HEADER lifecycle aliases to FINALISED.

    Review and execute manually during a controlled deployment. This script does
    not touch invoice amounts, vouchers, stock, GST staging, filing state, or any
    other table. It is transactional and safe to rerun.
*/
USE [mijms];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

-- Pre-migration inventory.
SELECT
    COALESCE(NULLIF(UPPER(LTRIM(RTRIM(STATUS))), ''), '<NULL_OR_EMPTY>') AS STATUS,
    COUNT_BIG(*) AS RECORD_COUNT
FROM dbo.INVOICE_HEADER
GROUP BY COALESCE(NULLIF(UPPER(LTRIM(RTRIM(STATUS))), ''), '<NULL_OR_EMPTY>')
ORDER BY STATUS;

-- Operator-facing affected-record preview. Do not expose internal GKEY values.
SELECT
    INV_NBR AS INVOICE_NUMBER,
    CONVERT(date, INV_DATE) AS INVOICE_DATE,
    STATUS AS CURRENT_STATUS,
    CAST('FINALISED' AS varchar(20)) AS PROPOSED_STATUS
FROM dbo.INVOICE_HEADER
WHERE UPPER(LTRIM(RTRIM(STATUS))) IN ('FINAL', 'FINALIZED')
ORDER BY INV_DATE, INV_NBR;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @ExpectedRows bigint =
    (
        SELECT COUNT_BIG(*)
        FROM dbo.INVOICE_HEADER WITH (UPDLOCK, HOLDLOCK)
        WHERE UPPER(LTRIM(RTRIM(STATUS))) IN ('FINAL', 'FINALIZED')
    );

    UPDATE dbo.INVOICE_HEADER
    SET STATUS = 'FINALISED'
    WHERE UPPER(LTRIM(RTRIM(STATUS))) IN ('FINAL', 'FINALIZED');

    DECLARE @UpdatedRows bigint = @@ROWCOUNT;
    IF @UpdatedRows <> @ExpectedRows
        THROW 51000, 'Invoice status normalization row count changed unexpectedly.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.INVOICE_HEADER
        WHERE UPPER(LTRIM(RTRIM(STATUS))) IN ('FINAL', 'FINALIZED')
    )
        THROW 51001, 'Completed invoice status aliases remain after normalization.', 1;

    COMMIT TRANSACTION;

    SELECT @UpdatedRows AS UPDATED_INVOICE_COUNT;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;

-- Post-migration verification.
SELECT
    COALESCE(NULLIF(UPPER(LTRIM(RTRIM(STATUS))), ''), '<NULL_OR_EMPTY>') AS STATUS,
    COUNT_BIG(*) AS RECORD_COUNT
FROM dbo.INVOICE_HEADER
GROUP BY COALESCE(NULLIF(UPPER(LTRIM(RTRIM(STATUS))), ''), '<NULL_OR_EMPTY>')
ORDER BY STATUS;

SELECT
    INV_NBR AS INVOICE_NUMBER,
    CONVERT(date, INV_DATE) AS INVOICE_DATE,
    STATUS
FROM dbo.INVOICE_HEADER
WHERE UPPER(LTRIM(RTRIM(STATUS))) IN ('FINAL', 'FINALIZED')
ORDER BY INV_DATE, INV_NBR;
GO
