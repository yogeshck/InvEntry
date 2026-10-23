/*
    InvEntry Database Migration
    File: 006_Sync_Dev_Columns_Views_Procedures.sql

    Purpose:
        Brings a production-restored mijms database (DB_Bkp_2026-10-20 schema) up to the
        definitions in the development mijms database for:

        Section 1  Missing columns on existing tables.
        Section 2  Views and procedures (CREATE OR ALTER from the development definitions).

    Run after: 005, 001 and 002, and after the DataAccess migrations 20261011 and 20261012.

    CONFIRMED: all historical invoices are final. Section 1b backfills them to FINALISED.
        INVOICE_HEADER.STATUS does not exist in the production schema. The development
        database has it as NOT NULL DEFAULT ('DRAFT'). Adding it with that default would
        make every existing invoice a DRAFT. This script backfills existing rows to
        'FINALISED'. Confirmed by the business owner: every historical invoice is final.

    Safe to rerun: Yes. Columns are added only when missing. Views and procedures are
    replaced with CREATE OR ALTER.

    Not covered here:
        ORG_BANK_DETAILS.GKEY (identity) and ORG_BANK_DETAILS.USE_BY_REF_GKEY (bigint to int)
        StockVerifyScan.Status (varbinary to varchar)
        These need a reviewed table rebuild or a data check. Not generated.
        Update_Stock_curday_Book exists in production but not in development. Left unchanged.
*/

SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* ---- Section 1a: columns that are nullable in development ---- */

IF OBJECT_ID(N'dbo.INVOICE_HEADER', N'U') IS NOT NULL AND COL_LENGTH('dbo.INVOICE_HEADER', 'FINALISED_ON') IS NULL
    ALTER TABLE dbo.INVOICE_HEADER ADD FINALISED_ON datetime2(7) NULL;
GO

IF OBJECT_ID(N'dbo.PRODUCT_STOCK', N'U') IS NOT NULL AND COL_LENGTH('dbo.PRODUCT_STOCK', 'GRN_LINE_SUMMARY_GKEY') IS NULL
    ALTER TABLE dbo.PRODUCT_STOCK ADD GRN_LINE_SUMMARY_GKEY int NULL;
GO

IF OBJECT_ID(N'dbo.PRODUCT_TRANSACTION_SUMMARY', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.PRODUCT_TRANSACTION_SUMMARY', 'DOCUMENT_NBR') IS NULL
        ALTER TABLE dbo.PRODUCT_TRANSACTION_SUMMARY ADD DOCUMENT_NBR varchar(50) NULL;
    IF COL_LENGTH('dbo.PRODUCT_TRANSACTION_SUMMARY', 'DOCUMENT_TYPE') IS NULL
        ALTER TABLE dbo.PRODUCT_TRANSACTION_SUMMARY ADD DOCUMENT_TYPE varchar(30) NULL;
    IF COL_LENGTH('dbo.PRODUCT_TRANSACTION_SUMMARY', 'NOTES') IS NULL
        ALTER TABLE dbo.PRODUCT_TRANSACTION_SUMMARY ADD NOTES varchar(250) NULL;
    IF COL_LENGTH('dbo.PRODUCT_TRANSACTION_SUMMARY', 'PRODUCT_GKEY') IS NULL
        ALTER TABLE dbo.PRODUCT_TRANSACTION_SUMMARY ADD PRODUCT_GKEY int NULL;
    IF COL_LENGTH('dbo.PRODUCT_TRANSACTION_SUMMARY', 'REF_GKEY') IS NULL
        ALTER TABLE dbo.PRODUCT_TRANSACTION_SUMMARY ADD REF_GKEY int NULL;
    IF COL_LENGTH('dbo.PRODUCT_TRANSACTION_SUMMARY', 'REF_LINE_GKEY') IS NULL
        ALTER TABLE dbo.PRODUCT_TRANSACTION_SUMMARY ADD REF_LINE_GKEY int NULL;
    IF COL_LENGTH('dbo.PRODUCT_TRANSACTION_SUMMARY', 'TRANSACTION_TYPE') IS NULL
        ALTER TABLE dbo.PRODUCT_TRANSACTION_SUMMARY ADD TRANSACTION_TYPE varchar(30) NULL;
END;
GO

/* ---- Section 1b: INVOICE_HEADER.STATUS (backfill confirmed, see header) ----
   Add nullable, backfill every existing invoice to FINALISED, then enforce NOT NULL and the
   development default. Development uses DEFAULT ('DRAFT') for new invoices. */

IF OBJECT_ID(N'dbo.INVOICE_HEADER', N'U') IS NOT NULL AND COL_LENGTH('dbo.INVOICE_HEADER', 'STATUS') IS NULL
BEGIN
    ALTER TABLE dbo.INVOICE_HEADER ADD STATUS varchar(20) NULL;
    EXEC (N'UPDATE dbo.INVOICE_HEADER SET STATUS = ''FINALISED'' WHERE STATUS IS NULL;');
    ALTER TABLE dbo.INVOICE_HEADER ALTER COLUMN STATUS varchar(20) NOT NULL;
    ALTER TABLE dbo.INVOICE_HEADER ADD CONSTRAINT DF_INVOICE_HEADER_STATUS DEFAULT ('DRAFT') FOR STATUS;
END;
GO

/* ---- Section 2: views and procedures (development definitions) ---- */
CREATE OR ALTER VIEW [dbo].[CUSTOMER_ORDER_DB_VIEW] AS
SELECT ORD.CUST_GKEY,
(SELECT CUSTOMER_NAME
FROM dbo.ORG_CUSTOMER AS OC
WHERE OC.GKEY = ORD.CUST_GKEY ) AS CUSTOMER_NAME,
ORD.CUST_MOBILE_NBR,
ORD.ORDER_NBR,
ORD.ORDER_DATE,
ORD.ORDER_REF_NBR,
ORD.ORDER_TYPE,
ORD.ORDER_DUE_DATE,
ORD.DELIVERY_DATE,
ORD.ORDER_STATUS_FLAG,
  (SELECT REF_VALUE
                       FROM      dbo.MTBL_REFERENCES AS MREF
                       WHERE   (REF_NAME = 'CUST_ORD_STATUS')
  AND REF_CODE = ORD.ORDER_STATUS_FLAG) AS ORDER_STATUS,
ORD.ORDER_BRANCH,
ORD.ADVANCE_PAID_AMOUNT,
ORD.BALANCE_AMOUNT,
OL.ORDER_LINE_NBR,
OL.PROD_CATEGORY,
OL.PROD_GROSS_WEIGHT,
OL.PROD_STONE_WEIGHT,
OL.PROD_NET_WEIGHT,
OL.PROD_QTY
FROM     dbo.CUSTOMER_ORDER AS ORD INNER JOIN
                  dbo.CUSTOMER_ORDER_LINES AS OL ON OL.ORDER_GKEY = ORD.GKEY;


GO

CREATE OR ALTER VIEW [dbo].[GRNDBVIEW]
AS
SELECT GH.GRN_NBR				AS	"GRN_NBR",
		GH.GRN_DATE				AS	"GRN_DATE",
		GH.DOCUMENT_TYPE		AS	"DOCUMENT_TYPE",
		GH.ORDER_NBR			AS	"ORDER_NBR",
		GH.ORDER_DATE			AS	"ORDER_DATE",
		GH.SUPPLIER_ID			AS	"SUPPLIER_ID",
		GH.SUPPLIER_REF_NBR		AS	"SUPPLIER_REF_NBR",
		GH.ITEM_RECEIVED_DATE	AS	"ITEM_RECEIVED_DATE",
		GH.GKEY					AS	"GKEY",
		GS.LINE_NBR				AS	"LINE_NBR",
		GS.PRODUCT_GKEY			AS	"PRODUCT_GKEY",
		GS.GROSS_WEIGHT			AS	"GROSS_WEIGHT",
		GS.STONE_WEIGHT			AS	"STONE_WEIGHT",
		GS.NET_WEIGHT			AS	"NET_WEIGHT",
		GS.SUPPLIED_QTY			AS	"SUPPLIED_QTY",
		GS.PRODUCT_CATEGORY		AS	"PRODUCT_CATEGORY",
		GS.PRODUCT_PURITY		AS	"PRODUCT_PURITY",
		GS.UOM					AS	"UOM"
FROM	GRN_HEADER GH, GRN_LINE_SUMMARY GS
WHERE	GH.GKEY = GS.GRN_HDR_GKEY


GO

CREATE OR ALTER VIEW [dbo].[ITEM_RCT_GRN_DBVIEW]
AS
SELECT GH.GRN_NBR				AS	"GRN_NBR",
		GH.GRN_DATE				AS	"GRN_DATE",
		GH.DOCUMENT_TYPE		AS	"DOCUMENT_TYPE",
		GH.SUPPLIER_ID			AS	"SUPPLIER_ID",
		GH.ITEM_RECEIVED_DATE	AS	"ITEM_RECEIVED_DATE",
		GH.GKEY					AS	"GKEY",
		GS.LINE_NBR				AS	"LINE_NBR",
		GS.PRODUCT_GKEY			AS	"PRODUCT_GKEY",
		GS.GROSS_WEIGHT			AS	"GROSS_WEIGHT",
		GS.STONE_WEIGHT			AS	"STONE_WEIGHT",
		GS.NET_WEIGHT			AS	"NET_WEIGHT",
		GS.SUPPLIED_QTY			AS	"SUPPLIED_QTY",
		GS.PRODUCT_CATEGORY		AS	"PRODUCT_CATEGORY",
		GS.PRODUCT_PURITY		AS	"PRODUCT_PURITY",
		GS.UOM					AS	"UOM"
FROM	GRN_HEADER GH, GRN_LINE_SUMMARY GS
WHERE	GH.GKEY = GS.GRN_HDR_GKEY
and grn_Date > '2026-04-01'


GO

CREATE OR ALTER view [dbo].[ORG_CUSTOMER_ADDRESS_VIEW] as
SELECT
		orgCustomer.CUSTOMER_NAME			AS "CUST_NAME",
		orgCustomer.PAN_NBR 				AS "PAN_NBR",
		orgCustomer.CREDIT_AVAILED			AS "CREDIT_AVAILED",
		orgCustomer.STATUS 					AS "CUST_STATUS",
		orgCustomer.CUSTOMER_SINCE			AS "CUST_SINCE",
		orgCustomer.SALUTATIONS				AS "SALUTATIONS",
        orgCustomer.CUSTOMER_TYPE 			AS "CUST_TYPE",
		orgAddress.ADDRESS_LINE1 			AS "ADD_LINE1",
		orgAddress.ADDRESS_LINE2 			AS "ADD_LINE2",
		orgAddress.ADDRESS_LINE3 			AS "ADD_LINE3",
		orgAddress.AREA						AS "AREA",
		orgAddress.CITY						AS "CITY",
		orgAddress.GST_STATE_CODE 			AS "CUST_GST_CODE",
		orgAddress.DISTRICT					AS "DISTRICT",
		orgAddress.STATE					AS "STATE",
		orgAddress.COUNTRY					AS "COUNTRY",
		orgCustomer.MOBILE_NBR				AS "MOBILE_NBR",
		orgCustomer.GKEY					AS "CUST_GKEY",
		orgCustomer.CLIENT_ID				AS "CLIENT_ID",
		orgCustomer.GSTIN_NBR				AS "GSTIN_NBR",
		orgCustomer.LOCATION_GKEY			AS "LOCATION_GKEY",
		orgCustomer.ADDRESS_GKEY			AS "ADDRESS_GKEY"
FROM    dbo.ORG_CUSTOMER AS orgCustomer INNER JOIN
        dbo.ORG_ADDRESS AS orgAddress ON orgCustomer.ADDRESS_GKEY = orgAddress.GKEY



GO

CREATE OR ALTER VIEW [dbo].[ORG_THIS_COMPANY_VIEW] AS
SELECT 	orgCompany.NAME AS company_name,
		orgAddress.ADDRESS_LINE1,
		orgAddress.ADDRESS_LINE2,
		orgAddress.ADDRESS_LINE3,
		orgAddress.AREA,
		orgAddress.STATE,
		orgAddress.PINCODE,
		orgAddress.COUNTRY,
		orgAddress.DISTRICT,
		orgAddress.CITY,
		orgCompany.CONTACT_NBR1,
		orgCompany.CONTACT_NBR2,
		orgCompany.EMAIL_ID,
		orgCompany.PAN_NBR,
		orgCompany.TENANT_GKEY,
		orgCompany.SERVICE_TAX_NBR,
		orgCompany.GST_NBR,
		orgAddress.GST_STATE_CODE AS gst_code,
		orgCompany.THIS_COMPANY,
		orgCompany.TAGLINE
FROM    dbo.ORG_COMPANY AS orgCompany
INNER JOIN dbo.ORG_ADDRESS AS orgAddress ON orgCompany.ADDRESS_GKEY = orgAddress.GKEY
WHERE  (orgCompany.THIS_COMPANY = '1')


GO

CREATE OR ALTER VIEW [dbo].[PRODUCT_VIEW] AS
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




GO

CREATE OR ALTER VIEW [dbo].[REP_SALES_INVRCT_DB_VIEW] AS

SELECT invoice_nbr as "InvNbr",
InvHdr.inv_date as "InvDate",
max(amount_payable) as "InvAmount",
max(case when transaction_type='Advance Adj' THEN adjusted_amount else 0 END) as "AdvanceAmt",
max(case when transaction_type='Discount' THEN adjusted_amount else 0 END) as "DiscountAmt",
max(case when transaction_type='Refund' THEN adjusted_amount else 0 END) as "Refund",
max(case when transaction_type='Recurring Deposit' THEN adjusted_amount else 0 END) as "RD",
max(case when transaction_type='Cash' THEN adjusted_amount else 0 END) as "Cash",
max(case when transaction_type='GPAY' THEN adjusted_amount else 0 END ) as "Gpay",
max(case when transaction_type='Credit Card' THEN adjusted_amount else 0 END ) as "CreditCard" ,
max(case when transaction_type='Debit Card' THEN adjusted_amount else 0 END) as "DebitCard",
max(case when transaction_type='Bank' THEN adjusted_amount else 0 END) as "Bank",
max(case when transaction_type='Credit' THEN adjusted_amount else 0 END) as "Credit",
max(case when transaction_type='Wire Transfer' THEN adjusted_amount else 0 END) as "Wire Transfer",
max(case when transaction_type='Bank Scanner' THEN adjusted_amount else 0 END) as "Bank Scanner",
max(case when transaction_type='mSwipe' THEN adjusted_amount else 0 END) as "mSwipe"
from dbo.INVOICE_AR_RECEIPTS as InvAR,
     dbo.INVOICE_HEADER as InvHdr
where InvHdr.gkey = InvAr.invoice_gkey
group by invoice_nbr, InvHdr.inv_date


GO

CREATE OR ALTER VIEW [dbo].[VOUCHER_DB_VIEW] AS
 SELECT [GKEY]    ,
[seq_nbr]
,[customer_gkey]
,[trans_type]
,[voucher_type]
,[mode]
,[trans_amount]
,[voucher_nbr]
,[voucher_date]
,[ref_doc_gkey]
,[ref_doc_nbr]
,[ref_doc_date]
,[trans_desc]
,[trans_date]
,CASE WHEN trans_type = 'Receipt' THEN trans_amount ELSE 0 END AS "Recd_Amount"
,CASE WHEN trans_type = 'Payment' THEN trans_amount ELSE 0 END AS "Paid_Amount"
,[from_ledger_gkey]
,FROM_LEDGER_NAME = (SELECT LEDGER_NAME FROM MTBL_LEDGERS MLF WHERE MLF.GKEY = VR.FROM_LEDGER_GKEY)
,[to_ledger_gkey]
,TO_LEDGER_NAME = (SELECT LEDGER_NAME FROM MTBL_LEDGERS MLT WHERE MLT.GKEY = VR.TO_LEDGER_GKEY)
,[ob_amount]
,[cb_amount]
,[fund_transfer_mode]
,[fund_transfer_ref_gkey]
,[fund_transfer_date]
  FROM [mijms].[dbo].[VOUCHER] VR


GO

CREATE OR ALTER VIEW dbo.CUSTOMER_ADVANCE_BALANCE_CHECK_VIEW
AS
SELECT
    A.GKEY            AS ADVANCE_ACCOUNT_GKEY,
    A.ACCOUNT_NBR,
    A.CUST_GKEY,
    A.BALANCE_AMOUNT  AS CACHED_BALANCE_AMOUNT,
    ISNULL(L.LEDGER_AMOUNT, 0) AS LEDGER_BALANCE_AMOUNT,
    A.BALANCE_WEIGHT  AS CACHED_BALANCE_WEIGHT,
    ISNULL(L.LEDGER_WEIGHT, 0) AS LEDGER_BALANCE_WEIGHT
FROM dbo.CUSTOMER_ADVANCE_ACCOUNT A
LEFT JOIN (
    SELECT ADVANCE_ACCOUNT_GKEY,
           SUM(AMOUNT) AS LEDGER_AMOUNT,
           SUM(WEIGHT) AS LEDGER_WEIGHT
    FROM dbo.ADVANCE_LEDGER
    GROUP BY ADVANCE_ACCOUNT_GKEY
) L ON L.ADVANCE_ACCOUNT_GKEY = A.GKEY
WHERE A.BALANCE_AMOUNT <> ISNULL(L.LEDGER_AMOUNT, 0)
   OR A.BALANCE_WEIGHT <> ISNULL(L.LEDGER_WEIGHT, 0);


GO

CREATE OR ALTER PROCEDURE  [dbo].[Get_Stock_Day_Book]
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
  FROM [mijms_prod_2812].[dbo].[REP_DAILY_STOCK_SUMMARY]
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

 		UPDATE [mijms_prod_2812].[dbo].[REP_DAILY_STOCK_SUMMARY]
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
				STOCK_TRNSFER_OUT_QTY	= @StkTrfrOutQty,
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


GO

-- sp_BackupDatabases: CREATE OR ALTER fails for sp_ names on SQL Server, so drop and recreate.
IF OBJECT_ID(N'dbo.sp_BackupDatabases', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_BackupDatabases;
GO
-- Create date: 2010-02-06
-- Description: Backup Databases for SQLExpress
-- Parameter1: databaseName
-- Parameter2: backupType F=full, D=differential, L=log
-- Parameter3: backup file location
-- =============================================
CREATE PROCEDURE [dbo].[sp_BackupDatabases]
            @databaseName sysname = null,
            @backupType CHAR(1),
            @backupLocation nvarchar(200)
AS
       SET NOCOUNT ON;
            DECLARE @DBs TABLE
            (
                  ID int IDENTITY PRIMARY KEY,
                  DBNAME nvarchar(500)
            )
             -- Pick out only databases which are online in case ALL databases are chosen to be backed up
             -- If specific database is chosen to be backed up only pick that out from @DBs
            INSERT INTO @DBs (DBNAME)
            SELECT Name FROM master.sys.databases
            where state=0
            AND name= ISNULL(@databaseName ,name)
            ORDER BY Name
            -- Filter out databases which do not need to backed up
            IF @backupType='F'
                  BEGIN
                  DELETE @DBs where DBNAME IN ('tempdb','Northwind','pubs','AdventureWorks')
                  END
            ELSE IF @backupType='D'
                  BEGIN
                  DELETE @DBs where DBNAME IN ('tempdb','Northwind','pubs','master','AdventureWorks')
                  END
            ELSE IF @backupType='L'
                  BEGIN
                  DELETE @DBs where DBNAME IN ('tempdb','Northwind','pubs','master','AdventureWorks')
                  END
            ELSE
                  BEGIN
                  RETURN
                  END
            -- Declare variables
            DECLARE @BackupName nvarchar(100)
            DECLARE @BackupFile nvarchar(300)
            DECLARE @DBNAME nvarchar(300)
            DECLARE @sqlCommand NVARCHAR(1000)
	        DECLARE @dateTime NVARCHAR(20)
            DECLARE @Loop int
            -- Loop through the databases one by one
            SELECT @Loop = min(ID) FROM @DBs
      WHILE @Loop IS NOT NULL
      BEGIN
-- Database Names have to be in [dbname] format since some have - or _ in their name
      SET @DBNAME = '['+(SELECT DBNAME FROM @DBs WHERE ID = @Loop)+']'
-- Set the current date and time n yyyyhhmmss format
      SET @dateTime = REPLACE(CONVERT(VARCHAR, GETDATE(),101),'/','') + '_' +  REPLACE(CONVERT(VARCHAR, GETDATE(),108),':','')
-- Create backup filename in path\filename.extension format for full,diff and log backups
      IF @backupType = 'F'
            SET @BackupFile = @backupLocation+REPLACE(REPLACE(@DBNAME, '[',''),']','')+ '_FULL_'+ @dateTime+ '.BAK'
      ELSE IF @backupType = 'D'
            SET @BackupFile = @backupLocation+REPLACE(REPLACE(@DBNAME, '[',''),']','')+ '_DIFF_'+ @dateTime+ '.BAK'
      ELSE IF @backupType = 'L'
            SET @BackupFile = @backupLocation+REPLACE(REPLACE(@DBNAME, '[',''),']','')+ '_LOG_'+ @dateTime+ '.TRN'
-- Provide the backup a name for storing in the media
      IF @backupType = 'F'
            SET @BackupName = REPLACE(REPLACE(@DBNAME,'[',''),']','') +' full backup for '+ @dateTime
      IF @backupType = 'D'
            SET @BackupName = REPLACE(REPLACE(@DBNAME,'[',''),']','') +' differential backup for '+ @dateTime
      IF @backupType = 'L'
            SET @BackupName = REPLACE(REPLACE(@DBNAME,'[',''),']','') +' log backup for '+ @dateTime
-- Generate the dynamic SQL command to be executed
       IF @backupType = 'F'
                  BEGIN
               SET @sqlCommand = 'BACKUP DATABASE ' +@DBNAME+  ' TO DISK = '''+@BackupFile+ ''' WITH INIT, NAME= ''' +@BackupName+''', NOSKIP, NOFORMAT'
                  END
       IF @backupType = 'D'
                  BEGIN
               SET @sqlCommand = 'BACKUP DATABASE ' +@DBNAME+  ' TO DISK = '''+@BackupFile+ ''' WITH DIFFERENTIAL, INIT, NAME= ''' +@BackupName+''', NOSKIP, NOFORMAT'
                  END
       IF @backupType = 'L'
                  BEGIN
               SET @sqlCommand = 'BACKUP LOG ' +@DBNAME+  ' TO DISK = '''+@BackupFile+ ''' WITH INIT, NAME= ''' +@BackupName+''', NOSKIP, NOFORMAT'
                  END
-- Execute the generated SQL command
       EXEC(@sqlCommand)
-- Goto the next database
SELECT @Loop = min(ID) FROM @DBs where ID>@Loop
END



GO

-- sp_generate_stock_summary: CREATE OR ALTER fails for sp_ names on SQL Server, so drop and recreate.
IF OBJECT_ID(N'dbo.sp_generate_stock_summary', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_generate_stock_summary;
GO
CREATE PROCEDURE [dbo].[sp_generate_stock_summary] (@report_date DATE)
AS
BEGIN
    SET NOCOUNT ON;

    -- Insert or update summary per product
-- Insert or update summary per product
    MERGE DAILY_REP_STOCK_SUMRY_MOVEMENT AS target
    USING (

    SELECT
        dm.product,
        dm.branch_id AS branch_id,
        dm.metal AS metal,
        ISNULL(prev.closing_qty,0) AS opening_qty,
        ISNULL(prev.closing_weight,0) AS opening_weight,
        SUM(dm.in_qty) AS in_qty,
        SUM(dm.in_gross_weight) AS in_weight,
        SUM(dm.out_qty) AS out_qty,
        SUM(dm.out_gross_weight) AS out_weight
    FROM DAILY_REP_STOCK_MOVEMENT dm
    LEFT JOIN DAILY_REP_STOCK_SUMRY_MOVEMENT prev
        ON prev.product = dm.product
       AND prev.report_date = DATEADD(DAY,-1,@report_date)
    WHERE dm.report_date = @report_date
    GROUP BY dm.product, dm.metal, dm.branch_id, prev.closing_qty, prev.closing_weight
) AS src

    ON target.report_date = @report_date AND target.product = src.product
    WHEN MATCHED THEN
        UPDATE SET
            opening_qty = src.opening_qty,
            opening_weight = src.opening_weight,
            in_qty = src.in_qty,
            in_gross_weight = src.in_weight,
            out_qty = src.out_qty,
            out_gross_weight = src.out_weight,
            closing_qty = src.opening_qty + src.in_qty - src.out_qty,
            closing_weight = src.opening_weight + src.in_weight - src.out_weight
    WHEN NOT MATCHED THEN
        INSERT (report_date, branch_id, product, metal, opening_qty, opening_weight,
                in_qty, in_gross_weight, out_qty, out_gross_weight, closing_qty, closing_weight)
        VALUES (@report_date, src.branch_id, src.product, src.metal, src.opening_qty, src.opening_weight,
                src.in_qty, src.in_weight, src.out_qty, src.out_weight,
                src.opening_qty + src.in_qty - src.out_qty,
                src.opening_weight + src.in_weight - src.out_weight);


    -- Products with no movement today: carry forward yesterday’s closing
        INSERT DAILY_REP_STOCK_SUMRY_MOVEMENT (report_date, branch_id, product, metal, opening_qty, opening_weight,
                in_qty, in_gross_weight, out_qty, out_gross_weight, closing_qty, closing_weight)
    SELECT
        @report_date,
        prev.branch_id AS branch_id,
        prev.product,
        prev.metal AS metal,
        prev.closing_qty AS opening_qty,
        prev.closing_weight AS opening_weight,
        0 AS in_qty,
        0 AS in_weight,
        0 AS out_qty,
        0 AS out_weight,
        prev.closing_qty AS closing_qty,       -- carry forward yesterday’s closing
        prev.closing_weight AS closing_weight
    FROM DAILY_REP_STOCK_SUMRY_MOVEMENT prev
    WHERE prev.report_date = DATEADD(DAY,-1, @report_date)
      AND NOT EXISTS (
          SELECT dm.product, dm.REPORT_DATE, dm.TYPE
          FROM dbo.DAILY_REP_STOCK_MOVEMENT dm
          WHERE dm.report_date = @report_date
            AND dm.product = prev.product
      )
END


GO

-- sp_rep_stock_trans_populate: CREATE OR ALTER fails for sp_ names on SQL Server, so drop and recreate.
IF OBJECT_ID(N'dbo.sp_rep_stock_trans_populate', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_rep_stock_trans_populate;
GO
CREATE PROCEDURE [dbo].[sp_rep_stock_trans_populate] (@report_date DATE)
AS
BEGIN
--GRN
	INSERT INTO DAILY_REP_STOCK_MOVEMENT ( REPORT_DATE, BRANCH_ID, TYPE, PRODUCT, METAL, REF_NBR,
											IN_QTY, IN_GROSS_WEIGHT, IN_STONE_WEIGHT, IN_NET_WEIGHT,
											OUT_QTY, OUT_GROSS_WEIGHT, OUT_STONE_WEIGHT, OUT_NET_WEIGHT )

		SELECT
			 CAST(h.ITEM_RECEIVED_DATE AS DATE) AS MOVEMENT_DATE,
			 1000,
			'GRN' AS TYPE,
			l.PRODUCT_CATEGORY,
			p.STOCK_GROUP,
			h.GRN_NBR,
			ISNULL(l.SUPPLIED_QTY, 0) AS STOCKIN_QTY,
			ISNULL(l.GROSS_WEIGHT, 0) AS STOCKIN_GROSS_WEIGHT,
			ISNULL(l.STONE_WEIGHT, 0) AS STOCKIN_STONE_WEIGHT,
			ISNULL(l.NET_WEIGHT, 0) AS STOCKIN_NET_WEIGHT,
			0,0,0,0
		FROM    grn_line_summary L, grn_header h, product p
		WHERE   L.grn_hdr_gkey = h.gkey
		  AND   p.gkey = l.product_gkey
		  AND   CAST(h.ITEM_RECEIVED_DATE AS DATE) = @report_date; --'2026-08-01';


	--SALES
	INSERT INTO DAILY_REP_STOCK_MOVEMENT ( REPORT_DATE, BRANCH_ID, TYPE, PRODUCT, METAL, REF_NBR,
											IN_QTY, IN_GROSS_WEIGHT, IN_STONE_WEIGHT, IN_NET_WEIGHT,
											OUT_QTY, OUT_GROSS_WEIGHT, OUT_STONE_WEIGHT, OUT_NET_WEIGHT )

		SELECT
			CAST(h.INV_DATE AS DATE) AS MOVEMENT_DATE,
			1000,
			'INVOICE' AS TYPE,
			l.PROD_CATEGORY,
			p.STOCK_GROUP,
			h.INV_NBR, 0,0,0,0,
			ISNULL(l.PROD_QTY, 0) AS STOCKOUT_QTY,
			ISNULL(l.PROD_GROSS_WEIGHT, 0) AS STOCKOUT_GROSS_WEIGHT,
			ISNULL(l.PROD_STONE_WEIGHT, 0) AS STOCKOUT_STONE_WEIGHT,
			ISNULL(l.PROD_NET_WEIGHT, 0) AS STOCKOUT_NET_WEIGHT
		FROM invoice_header h
		INNER JOIN invoice_line l   ON l.INVOICE_HDR_GKEY = h.GKEY
		INNER JOIN PRODUCT P        ON l.PRODUCT_GKEY = p.GKEY
		WHERE CAST(h.INV_DATE AS DATE) = @report_date;


	-- ESTIMATE
    INSERT INTO DAILY_REP_STOCK_MOVEMENT ( REPORT_DATE, BRANCH_ID, TYPE, PRODUCT, METAL, REF_NBR,
											IN_QTY, IN_GROSS_WEIGHT, IN_STONE_WEIGHT, IN_NET_WEIGHT,
											OUT_QTY, OUT_GROSS_WEIGHT, OUT_STONE_WEIGHT, OUT_NET_WEIGHT )

		SELECT
			CAST(h.EST_DATE AS DATE) AS MOVEMENT_DATE,
			1000,
			'ESTIMATE' AS TYPE,
			l.PROD_CATEGORY,
			p.STOCK_GROUP,
			h.EST_NBR, 0,0,0,0,
			ISNULL(l.PROD_QTY, 0) AS STOCKOUT_QTY,
			ISNULL(l.PROD_GROSS_WEIGHT, 0) AS STOCKOUT_GROSS_WEIGHT,
			ISNULL(l.PROD_STONE_WEIGHT, 0) AS STOCKOUT_STONE_WEIGHT,
			ISNULL(l.PROD_NET_WEIGHT, 0) AS STOCKOUT_NET_WEIGHT
		FROM estimate_header h
		INNER JOIN estimate_line l  ON l.ESTIMATE_HDR_GKEY = h.GKEY
		INNER JOIN PRODUCT P        ON l.PRODUCT_GKEY = p.GKEY
		WHERE CAST(h.EST_DATE AS DATE) = @report_date
		and l.PROD_CATEGORY not like 'OLD%'


	--INTRODUCE METAL - GOLD/SILVER ETC
    INSERT INTO DAILY_REP_STOCK_MOVEMENT ( REPORT_DATE, BRANCH_ID, TYPE, PRODUCT, METAL, REF_NBR,
											IN_QTY, IN_GROSS_WEIGHT, IN_STONE_WEIGHT, IN_NET_WEIGHT,
											OUT_QTY, OUT_GROSS_WEIGHT, OUT_STONE_WEIGHT, OUT_NET_WEIGHT )

		SELECT
			CAST(om.TRANS_DATE AS DATE) AS MOVEMENT_DATE,
			1000,
			TRANS_TYPE AS TYPE,
			PRODUCT_CATEGORY,
			p.STOCK_GROUP metal,
			TRANS_NBR, 0,0,0,0,
			0 AS STOCKOUT_QTY,
			ISNULL(GROSS_WEIGHT, 0) AS STOCKOUT_GROSS_WEIGHT,
			ISNULL(STONE_WEIGHT, 0) AS STOCKOUT_STONE_WEIGHT,
			ISNULL(NET_WEIGHT, 0) AS STOCKOUT_NET_WEIGHT
		FROM OLD_METAL_TRANSACTION om
		INNER JOIN PRODUCT P        ON om.PRODUCT_GKEY = p.GKEY
		WHERE TRANS_TYPE = 'OG Transfer'
		AND CAST(TRANS_DATE AS DATE) = @report_date


	-- PICK OLD METAL TRANSACTIONS - OG PURCHASE
	INSERT INTO DAILY_REP_STOCK_MOVEMENT ( REPORT_DATE, BRANCH_ID, TYPE, PRODUCT, METAL, REF_NBR,
											IN_QTY, IN_GROSS_WEIGHT, IN_STONE_WEIGHT, IN_NET_WEIGHT,
											OUT_QTY, OUT_GROSS_WEIGHT, OUT_STONE_WEIGHT, OUT_NET_WEIGHT )
		SELECT
			CAST(om.TRANS_DATE AS DATE) AS MOVEMENT_DATE,
			1000,
			TRANS_TYPE AS TYPE,
			PRODUCT_CATEGORY,
			p.STOCK_GROUP metal,
			TRANS_NBR,
			0 AS STOCKIN_QTY,
			ISNULL(GROSS_WEIGHT, 0) AS STOCKIN_GROSS_WEIGHT,
			ISNULL(STONE_WEIGHT, 0) AS STOCKIN_STONE_WEIGHT,
			ISNULL(NET_WEIGHT, 0) AS STOCKIN_NET_WEIGHT,
			0,0,0,0
		FROM OLD_METAL_TRANSACTION om
		INNER JOIN PRODUCT P        ON om.PRODUCT_GKEY = p.GKEY
		WHERE om.TRANS_TYPE = 'OG Purchase'
		AND CAST(om.TRANS_DATE AS DATE) = @report_date
		   AND PRODUCT_CATEGORY IS NOT NULL


	-- PICK OLD METAL TRANSACTIONS - OG ADVANCE
	INSERT INTO DAILY_REP_STOCK_MOVEMENT ( REPORT_DATE, BRANCH_ID, TYPE, PRODUCT, METAL, REF_NBR,
											IN_QTY, IN_GROSS_WEIGHT, IN_STONE_WEIGHT, IN_NET_WEIGHT,
											OUT_QTY, OUT_GROSS_WEIGHT, OUT_STONE_WEIGHT, OUT_NET_WEIGHT )
		SELECT
			CAST(om.TRANS_DATE AS DATE) AS MOVEMENT_DATE,
			1000,
			TRANS_TYPE AS TYPE,
			PRODUCT_CATEGORY,
			p.STOCK_GROUP metal,
			TRANS_NBR,
			0 AS STOCKIN_QTY,
			ISNULL(GROSS_WEIGHT, 0) AS STOCKIN_GROSS_WEIGHT,
			ISNULL(STONE_WEIGHT, 0) AS STOCKIN_STONE_WEIGHT,
			ISNULL(NET_WEIGHT, 0) AS STOCKIN_NET_WEIGHT,
			0,0,0,0
		FROM OLD_METAL_TRANSACTION om
		INNER JOIN PRODUCT P        ON om.PRODUCT_GKEY = p.GKEY
		WHERE om.TRANS_TYPE = 'OG Advance'
		AND CAST(om.TRANS_DATE AS DATE) = @report_date
		   AND PRODUCT_CATEGORY IS NOT NULL

END


GO

