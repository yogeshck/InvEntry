CREATE PROCEDURE  [dbo].[Get_Stock_Day_Book]
	@currentDate  varchar(10)   --Date
AS

DECLARE @productCategory  VARCHAR(50)
DECLARE @BalanceWeight decimal(10,3)
DECLARE @OpeningStockWt decimal(10,3)
DECLARE @SuppInWt decimal(10,3)
DECLARE @SalesWt decimal(10,3)
DECLARE @StkTrfrInWt decimal(10,3)
DECLARE @StkTrfrOutWt decimal(10,3)
DECLARE @StkQty int
DECLARE @OpeningStockQty int
DECLARE @ClosingStockQty int
DECLARE @SuppInQty int
DECLARE @SalesQty int
DECLARE @StkTrfrInQty int
DECLARE @StkTrfrOutQty int
DECLARE @Gkey int

BEGIN
--SELECT @currentDate = CONVERT(VARCHAR(20),@currentDate,112)  --2

--	SELECT 	CATEGORY, BALANCE_WEIGHT, STOCK_QTY
--	FROM	dbo.PRODUCT_STOCK_SUMMARY;

DECLARE dailyStkSumry_cursor CURSOR FOR 
SELECT 
       [PRODUCT_CATEGORY]
      ,[GKEY]
      ,[OPENING_STOCK]
      ,[OPENING_STOCK_QTY]
  FROM [mijms].[dbo].[REP_DAILY_STOCK_SUMMARY]
  WHERE convert(varchar(10), TRANSACTION_DATE, 105) = @currentDate --'21-12-2024'

OPEN dailyStkSumry_cursor  

	SELECT	@SuppInWt=0,
			@StkTrfrInWt=0,		
			@SalesWt=0,
			@StkTrfrOutWt=0, 
			@BalanceWeight=0,
			@StkQty=0,
			@SuppInQty=0,
			@SalesQty=0,
			@StkTrfrInQty=0,
			@StkTrfrOutQty=0,
			@OpeningStockQty=0,
			@ClosingStockQty=0,
			@Gkey=0;

FETCH NEXT 
	FROM dailyStkSumry_cursor 
	INTO @productCategory,@Gkey,@OpeningStockWt,@OpeningStockQty; 

WHILE @@FETCH_STATUS = 0  

BEGIN  

	BEGIN

		BEGIN TRAN

		SELECT
			@SalesWt 		=(
							ISNULL((
									SELECT 	sum(il.PROD_GROSS_WEIGHT)   
									FROM 	[dbo].[INVOICE_LINE] il,
											[dbo].[INVOICE_HEADER] ih
									WHERE 	il.invoice_id = ih.inv_nbr
									AND 	il.PROD_CATEGORY=  @productCategory
									AND 	convert(varchar(10), ih.INV_DATE, 105) = @currentDate --'21-12-2024' 
									GROUP BY prod_category, convert(varchar(10), ih.inv_date, 105)),0)
						),

			@SalesQty 		=(
							ISNULL((
									SELECT 	sum(il.prod_qty)   
									FROM 	[dbo].[INVOICE_LINE] il,
											[dbo].[INVOICE_HEADER] ih
									WHERE 	il.invoice_id = ih.inv_nbr
									AND 	il.PROD_CATEGORY=  @productCategory
									AND 	convert(varchar(10), IH.INV_DATE, 105) = @currentDate --'21-12-2024' 
									GROUP BY prod_category, convert(varchar(10), ih.inv_date, 105)),0)
						),

			@StkTrfrOutWt =(
							ISNULL((
							SELECT 	sum(il.PROD_GROSS_WEIGHT)  
							FROM 	[dbo].ESTIMATE_LINE il,
									[dbo].ESTIMATE_HEADER ih
							WHERE 	il.estimate_id = ih.est_nbr
							AND 	il.PROD_CATEGORY=  @productCategory
							AND 	convert(varchar(10), IH.est_DATE, 105) = @currentDate 
							AND		ih.CUST_MOBILE in ( '9446419916','7373088913')
							GROUP BY prod_category, convert(varchar(10), ih.est_date, 105)),0)
						),
						
			@StkTrfrOutQty =(
							ISNULL((
							SELECT 	sum(il.PROD_QTY)  
							FROM 	[dbo].ESTIMATE_LINE il,
									[dbo].ESTIMATE_HEADER ih
							WHERE 	il.estimate_id = ih.est_nbr
							AND 	il.PROD_CATEGORY=  @productCategory
							AND 	convert(varchar(10), IH.est_DATE, 105) = @currentDate 
							AND		ih.CUST_MOBILE in ( '9446419916','7373088913')
							GROUP BY prod_category, convert(varchar(10), ih.est_date, 105)),0)
						),
							
			@SuppInWt 	=(
							ISNULL((
							SELECT 	sum(gl.GROSS_WEIGHT)  
							FROM 	[dbo].GRN_LINE_SUMMARY gl, 
									[dbo].GRN_HEADER gh
							WHERE 	gl.grn_hdr_gkey = gh.gkey
							AND 	gl.PRODUCT_CATEGORY=  @productCategory
							AND 	convert(varchar(10), gh.grn_DATE, 105) = @currentDate  
							GROUP by product_category, convert(varchar(10), gh.grn_date, 105)),0)
						),
						
						
			@SuppInQty 	=(
							ISNULL((
							SELECT 	sum(gl.supplied_qty)  
							FROM 	[dbo].GRN_LINE_SUMMARY gl, 
									[dbo].GRN_HEADER gh
							WHERE 	gl.grn_hdr_gkey = gh.gkey
							AND 	gl.PRODUCT_CATEGORY=  @productCategory
							AND 	convert(varchar(10), gh.grn_DATE, 105) = @currentDate  
							GROUP by product_category, convert(varchar(10), gh.grn_date, 105)),0)
						)
						
		PRINT 'CATEGORY : '+@productCategory
	
 		UPDATE [mijms].[dbo].[REP_DAILY_STOCK_SUMMARY] 
		SET
				OPENING_STOCK			= @OpeningStockWt,
				STOCK_IN				= @SuppInWt,
				STOCK_TRANSFER_IN		= @StkTrfrInWt,
				STOCK_OUT				= @SalesWt,
				STOCK_TRANSFER_OUT		= @StkTrfrOutWt,
				CLOSING_STOCK			= ((@OpeningStockWt + @SuppInWt + @StkTrfrInWt) - ( @SalesWt + @StkTrfrOutWt ) ),
				OPENING_STOCK_QTY		= @OpeningStockQty,
				STOCK_IN_QTY			= @SuppInQty,
				STOCK_OUT_QTY			= @SalesQty,
				STOCK_TRANSFER_OUT_QTY	= @StkTrfrOutQty,
				CLOSING_STOCK_QTY		= ((@OpeningStockQty + @SuppInQty + @StkTrfrInQty) - ( @SalesQty + @StkTrfrOutQty) )
		WHERE	gkey = @Gkey;

		FETCH NEXT FROM dailyStkSumry_cursor 
				INTO @productCategory,@Gkey,@OpeningStockWt,@OpeningStockQty; 
		
				Commit TRAN 

		SELECT	@SuppInWt=0,
				@StkTrfrInWt=0,		
				@SalesWt=0,
				@StkTrfrOutWt=0, 
				@SuppInQty=0,
				@SalesQty=0,
				@StkTrfrInQty=0,
				@StkTrfrOutQty=0;
					  
					   

	END
	  
END 
  
END

