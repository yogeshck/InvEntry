

CREATE VIEW [dbo].[PRODUCT_VIEW] AS
	SELECT 
			pr.ID, 
			pr.NAME, 
			pr.DESCRIPTION, 
			pr.CATEGORY, 
			pr.PURITY, 
			pr.METAL, 
			pr.HSN_CODE, 
			pr.UOM, 
			pr.IS_TAXABLE, 
			pr.gkey,
			prstk.PRODUCT_SKU, 
			prstk.GROSS_WEIGHT, 
			ISNULL(prstk.STONE_WEIGHT, 0) AS STONE_WEIGHT, 
			prstk.NET_WEIGHT, prstk.VA_PERCENT, 
			prstk.WASTAGE_PERCENT, 
			prstk.WASTAGE_AMOUNT, 
			prstk.SOLD_WEIGHT, 
			prstk.BALANCE_WEIGHT, 
			prstk.SOLD_QTY, 
			prstk.STOCK_QTY
	FROM    dbo.PRODUCT AS pr LEFT OUTER JOIN
            dbo.PRODUCT_STOCK_SUMMARY AS prstk ON pr.GKEY = prstk.PRODUCT_GKEY