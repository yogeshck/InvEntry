CREATE PROCEDURE  [dbo].[Get_Stock_Day_Book]
@currentDate  varchar(10)   --Date
AS

DECLARE @productCategory  VARCHAR(50)
DECLARE @BalanceWeight decimal(10,3)
DECLARE @OpeningStock decimal(10,3)
DECLARE @SuppIn decimal(10,3)
DECLARE @Sales decimal(10,3)
DECLARE @StkTrfrIn decimal(10,3)
DECLARE @StkTrfrOut decimal(10,3)

BEGIN
--SELECT @currentDate = CONVERT(VARCHAR(20),@currentDate,112)  --2

DECLARE stock_cursor CURSOR FOR
SELECT CATEGORY, BALANCE_WEIGHT
FROM dbo.PRODUCT_STOCK_SUMMARY;

OPEN stock_cursor  

SELECT @SuppIn=0,
@StkTrfrIn=0,
@Sales=0,
@StkTrfrOut=0,
@BalanceWeight=0;

FETCH NEXT
FROM stock_cursor
INTO @productCategory,@BalanceWeight;

WHILE @@FETCH_STATUS = 0  

BEGIN  

BEGIN

BEGIN TRAN

SELECT
@Sales =(
ISNULL((
SELECT sum(il.PROD_GROSS_WEIGHT)  
FROM [dbo].[INVOICE_LINE] il,
[dbo].[INVOICE_HEADER] ih
WHERE il.invoice_id = ih.inv_nbr
AND il.PROD_CATEGORY=  @productCategory
AND convert(varchar(10), IH.INV_DATE, 105) = @currentDate --'21-12-2024'
GROUP BY prod_category, convert(varchar(10), ih.inv_date, 105)),0)
),

@StkTrfrOut =(
ISNULL((
SELECT sum(il.PROD_GROSS_WEIGHT)  
FROM [dbo].ESTIMATE_LINE il,
[dbo].ESTIMATE_HEADER ih
WHERE il.estimate_id = ih.est_nbr
AND il.PROD_CATEGORY=  @productCategory
AND convert(varchar(10), IH.est_DATE, 105) = @currentDate
AND ih.CUST_MOBILE = '9446419916'
GROUP BY prod_category, convert(varchar(10), ih.est_date, 105)),0)
),

@SuppIn =(
ISNULL((
SELECT sum(gl.GROSS_WEIGHT)  
FROM [dbo].GRN_LINE_SUMMARY gl,
[dbo].GRN_HEADER gh
WHERE gl.grn_hdr_gkey = gh.gkey
AND gl.PRODUCT_CATEGORY=  @productCategory
AND convert(varchar(10), gh.grn_DATE, 105) = @currentDate  
GROUP by product_category, convert(varchar(10), gh.grn_date, 105)),0)
)

PRINT 'CATEGORY : '+@productCategory

INSERT INTO [mijms].[dbo].[REP_DAILY_STOCK_SUMMARY]
(   TRANSACTION_DATE,
PRODUCT_CATEGORY,
OPENING_STOCK,
STOCK_IN,
STOCK_TRANSFER_IN,
STOCK_OUT,
STOCK_TRANSFER_OUT,
CLOSING_STOCK
)
SELECT  Convert(DATETIME, @currentDate, 105),
@productCategory,
((@BalanceWeight + @Sales + @StkTrfrOut) - ( @SuppIn + @StkTrfrIn) ),
@SuppIn,
@StkTrfrIn,
@Sales,
@StkTrfrOut,
@BalanceWeight

FETCH NEXT FROM stock_cursor
INTO @productCategory ,@BalanceWeight

Commit TRAN

SELECT @SuppIn=0,
@StkTrfrIn=0,
@Sales=0,
@StkTrfrOut=0,
@BalanceWeight=0;

END
 
END
 
END

CLOSE stock_cursor  
DEALLOCATE stock_cursor