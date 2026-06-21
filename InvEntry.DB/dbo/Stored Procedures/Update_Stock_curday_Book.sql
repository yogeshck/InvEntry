CREATE PROCEDURE  [dbo].[Update_Stock_curday_Book]
@currentDate  varchar(10)   --Date
AS

DECLARE @productCategory  VARCHAR(50)
DECLARE @GrossWeight decimal(10,3)
DECLARE @NetWeight decimal(10,3)
DECLARE @BalanceWeight decimal(10,3)
DECLARE @ClosingStockQty int

DECLARE @SuppIn decimal(10,3)
DECLARE @Sales decimal(10,3)
DECLARE @StkTrfrIn decimal(10,3)
DECLARE @StkTrfrOut decimal(10,3)
DECLARE @StkQty int
DECLARE @SuppInQty int
DECLARE @SalesQty int
DECLARE @StkTrfrInQty int
DECLARE @StkTrfrOutQty int

DECLARE daily_stock_cursor CURSOR FOR
SELECT CATEGORY, GROSS_WEIGHT, NET_WEIGHT, BALANCE_WEIGHT, STOCK_QTY
FROM [dbo].[PRODUCT_STOCK_SUMMARY];

OPEN daily_stock_cursor  

SELECT @SuppIn=0,
@StkTrfrIn=0,
@Sales=0,
@StkTrfrOut=0,
@StkQty=0,
@SuppInQty=0,
@SalesQty=0,
@StkTrfrInQty=0,
@StkTrfrOutQty=0;


FETCH NEXT
FROM daily_stock_cursor
INTO @productCategory,@GrossWeight,@NetWeight,@BalanceWeight,@StkQty;

WHILE @@FETCH_STATUS = 0  

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

@SalesQty =(
ISNULL((
SELECT sum(il.prod_qty)  
FROM [dbo].[INVOICE_LINE] il,
[dbo].[INVOICE_HEADER] ih
WHERE il.invoice_id = ih.inv_nbr
AND il.PROD_CATEGORY=  @productCategory
AND convert(varchar(10), IH.INV_DATE, 105) = @currentDate --'21-12-2024'
GROUP BY prod_category, convert(varchar(10), ih.inv_date, 105)),0)
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
),


@SuppInQTY =(
ISNULL((
SELECT sum(gl.supplied_qty)  
FROM [dbo].GRN_LINE_SUMMARY gl,
[dbo].GRN_HEADER gh
WHERE gl.grn_hdr_gkey = gh.gkey
AND gl.PRODUCT_CATEGORY=  @productCategory
AND convert(varchar(10), gh.grn_DATE, 105) = @currentDate  
GROUP by product_category, convert(varchar(10), gh.grn_date, 105)),0)
);

PRINT ' CATEGORY  : ' + @productCategory ;


UPDATE [mijms].[dbo].PRODUCT_STOCK_SUMMARY
SET    GROSS_WEIGHT = ( ( ISNULL(@GrossWeight,0) + ISNULL(@Sales,0) + ISNULL(@StkTrfrOut,0) ) - ( ISNULL(@SuppIn,0) + ISNULL(@StkTrfrIn,0) ) ),
  NET_WEIGHT = ( ( ISNULL(@NetWeight,0) + ISNULL(@Sales,0) + ISNULL(@StkTrfrOut,0) ) - ( ISNULL(@SuppIn,0) + ISNULL(@StkTrfrIn,0) ) ),
  BALANCE_WEIGHT = ( ( ISNULL(@BalanceWeight,0) + ISNULL(@Sales,0) + ISNULL(@StkTrfrOut,0) ) - ( ISNULL(@SuppIn,0) + ISNULL(@StkTrfrIn,0) ) ),
  STOCK_QTY = (( ISNULL(@StkQty,0) + ISNULL(@SalesQty,0) + ISNULL(@StkTrfrOutQty,0) ) - ( ISNULL(@SuppInQty,0) + ISNULL(@StkTrfrInQty,0) ) )
  WHERE CATEGORY = @productCategory;


FETCH NEXT FROM daily_stock_cursor
INTO @productCategory,@GrossWeight,@NetWeight,@BalanceWeight,@StkQty;

Commit TRAN

SELECT @SuppIn=0,
@StkTrfrIn=0,
@Sales=0,
@StkTrfrOut=0,
@SuppInQty=0,
@SalesQty=0,
@StkTrfrInQty=0,
@StkTrfrOutQty=0;
END


CLOSE daily_stock_cursor  
DEALLOCATE daily_stock_cursor
