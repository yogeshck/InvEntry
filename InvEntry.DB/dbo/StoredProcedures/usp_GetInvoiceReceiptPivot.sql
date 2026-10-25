CREATE   PROCEDURE usp_GetInvoiceReceiptPivot
(
    @StartDate DATE,
    @EndDate   DATE
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE 
        @cols NVARCHAR(MAX),
        @sql  NVARCHAR(MAX);

    ---------------------------------------------------------
    -- Step 1: Build dynamic list of transaction types
    ---------------------------------------------------------
    SELECT @cols = STRING_AGG(QUOTENAME(transaction_type), ',')
    FROM (
        SELECT DISTINCT transaction_type
        FROM dbo.INVOICE_AR_RECEIPTS
    ) AS t;

    ---------------------------------------------------------
    -- Step 2: Build dynamic SQL with date filter
    ---------------------------------------------------------
    SET @sql = '
    SELECT 
        InvNbr,
        InvDate,
        InvAmount,
        ' + @cols + '
    FROM
    (
        SELECT 
            InvAR.invoice_nbr AS InvNbr,
            InvHdr.INV_DATE AS InvDate,
            InvHdr.AMOUNT_PAYABLE AS InvAmount,
            InvAR.transaction_type,
            InvAR.adjusted_amount
        FROM dbo.INVOICE_AR_RECEIPTS InvAR
        INNER JOIN dbo.INVOICE_HEADER InvHdr
            ON InvAR.invoice_gkey = InvHdr.GKEY
        WHERE InvHdr.INV_DATE BETWEEN @StartDate AND @EndDate
    ) AS src
    PIVOT
    (
        MAX(adjusted_amount)
        FOR transaction_type IN (' + @cols + ')
    ) AS p
    ORDER BY InvNbr, InvDate;
    ';

    ---------------------------------------------------------
    -- Step 3: Execute dynamic SQL safely
    ---------------------------------------------------------
    EXEC sp_executesql 
        @sql,
        N'@StartDate DATE, @EndDate DATE',
        @StartDate = @StartDate,
        @EndDate   = @EndDate;
END;
