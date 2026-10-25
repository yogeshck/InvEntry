/* InvEntry deployment validation - READ ONLY.
   This script performs SELECT/metadata operations only. It does not create temp tables,
   mutate data, execute application procedures, or change transaction isolation. */
SET NOCOUNT ON;

SELECT DB_NAME() AS database_name, ORIGINAL_LOGIN() AS login_name,
       @@SERVERNAME AS server_name, CAST(SERVERPROPERTY('ProductVersion') AS nvarchar(128)) AS sql_version;

SELECT required_object,
       CASE WHEN OBJECT_ID(required_object) IS NULL THEN 'FAIL' ELSE 'PASS' END AS status
FROM (VALUES
 ('dbo.VOUCHER_TYPES'),('dbo.VOUCHER'),('dbo.MTBL_REFERENCES'),
 ('dbo.ORG_COMPANY'),('dbo.ORG_ADDRESS'),('dbo.ORG_THIS_COMPANY_VIEW'),
 ('dbo.ORG_CUSTOMER'),('dbo.ORG_BANK_DETAILS'),('dbo.PRODUCT'),
 ('dbo.INVOICE_HEADER'),('dbo.INVOICE_LINE'),('dbo.INVOICE_AR_RECEIPTS'),
 ('dbo.ESTIMATE_HEADER'),('dbo.ESTIMATE_LINE'),
 ('dbo.GRN_HEADER'),('dbo.GRN_LINE'),('dbo.GRN_LINE_SUMMARY'),
 ('dbo.CUSTOMER_ORDER'),('dbo.CUSTOMER_ORDER_LINES'),
 ('dbo.STOCK_TRANSFER_HEADER'),('dbo.STOCK_TRANSFER_LINE'),
 ('dbo.OLD_METAL_TRANSACTION'),('dbo.PRODUCT_STOCK'),('dbo.PRODUCT_TRANSACTION'),
 ('dbo.GST_GSTR1_DOCUMENT'),('dbo.GST_GSTR1_DOCUMENT_LINE'),
 ('dbo.DAILY_REP_STOCK_MOVEMENT'),('dbo.DAILY_REP_STOCK_SUMRY_MOVEMENT'),
 ('dbo.ITEM_RCT_GRN_DBVIEW'),('dbo.PRODUCT_STOCK_TMP'),
 ('dbo.PRODUCT_VIEW'),('dbo.ORG_CUSTOMER_ADDRESS_VIEW'),
 ('dbo.GRNDBVIEW'),('dbo.VOUCHER_DB_VIEW'),('dbo.REP_SALES_INVRCT_DB_VIEW')
) v(required_object)
ORDER BY required_object;

IF OBJECT_ID('dbo.VOUCHER_TYPES') IS NOT NULL
BEGIN
    SELECT DOCUMENT_TYPE, COUNT(*) AS row_count,
           CASE WHEN COUNT(*) = 1 THEN 'PASS' ELSE 'FAIL' END AS uniqueness_status
    FROM dbo.VOUCHER_TYPES
    GROUP BY DOCUMENT_TYPE
    HAVING DOCUMENT_TYPE IN ('Sale Invoice','Estimate','GRN','Customer Order','Stock Transfer','OM Transfer','OM Purchase')
        OR COUNT(*) > 1;

    SELECT required_document_type,
           CASE WHEN COUNT(v.GKEY)=0 THEN 'FAIL: missing'
                WHEN COUNT(v.GKEY)>1 THEN 'FAIL: duplicate'
                WHEN MAX(CASE WHEN v.LAST_USED_NUMBER IS NULL OR v.LAST_USED_NUMBER < 0 THEN 1 ELSE 0 END)=1 THEN 'FAIL: counter'
                WHEN MAX(CASE WHEN required_document_type IN ('Stock Transfer','OM Transfer') AND (v.IS_ACTIVE<>1 OR v.DOC_NBR_LENGTH IS NULL OR v.DOC_NBR_LENGTH<=0) THEN 1 ELSE 0 END)=1 THEN 'FAIL: active/length'
                ELSE 'PASS' END AS status
    FROM (VALUES ('Sale Invoice'),('Estimate'),('GRN'),('Customer Order'),('Stock Transfer'),('OM Transfer'),('OM Purchase')) r(required_document_type)
    LEFT JOIN dbo.VOUCHER_TYPES v ON v.DOCUMENT_TYPE=r.required_document_type
    GROUP BY required_document_type;
END;

IF OBJECT_ID('dbo.ORG_COMPANY') IS NOT NULL AND OBJECT_ID('dbo.ORG_ADDRESS') IS NOT NULL
BEGIN
    SELECT c.GKEY,c.NAME,c.GST_NBR,c.THIS_COMPANY,c.ADDRESS_GKEY,
           a.GKEY AS matched_address_gkey,a.GST_STATE_CODE,a.STATE,a.CITY,a.PINCODE,
           CASE WHEN NULLIF(LTRIM(RTRIM(c.NAME)),'') IS NULL THEN 'FAIL: company name'
                WHEN a.GKEY IS NULL THEN 'FAIL: address join'
                ELSE 'PASS' END AS core_status,
           CASE WHEN NULLIF(LTRIM(RTRIM(c.GST_NBR)),'') IS NULL OR NULLIF(LTRIM(RTRIM(a.GST_STATE_CODE)),'') IS NULL
                THEN 'INVESTIGATE: GST incomplete' ELSE 'PASS' END AS gst_status
    FROM dbo.ORG_COMPANY c LEFT JOIN dbo.ORG_ADDRESS a ON a.GKEY=c.ADDRESS_GKEY
    WHERE c.THIS_COMPANY=1;

    SELECT COUNT(*) AS this_company_count,
           CASE WHEN COUNT(*)=1 THEN 'PASS' ELSE 'FAIL: expected exactly one' END AS status
    FROM dbo.ORG_COMPANY WHERE THIS_COMPANY=1;
END;

IF OBJECT_ID('dbo.MTBL_REFERENCES') IS NOT NULL
BEGIN
    SELECT REF_NAME,REF_CODE,REF_VALUE,SORT_SEQ,IS_ACTIVE,
           CASE WHEN NULLIF(LTRIM(RTRIM(REF_CODE)),'') IS NULL OR NULLIF(LTRIM(RTRIM(REF_VALUE)),'') IS NULL
                THEN 'FAIL' ELSE 'PASS' END AS status
    FROM dbo.MTBL_REFERENCES
    WHERE REF_NAME IN ('STOCK_TRANSFER','DAILY_RATE','SUPPLIERS','STOCK_ADJUSTMENTS')
    ORDER BY REF_NAME,SORT_SEQ;
END;

IF OBJECT_ID('dbo.GRN_HEADER') IS NOT NULL
BEGIN
    SELECT STATUS, COUNT(*) AS row_count
    FROM dbo.GRN_HEADER
    GROUP BY STATUS
    ORDER BY STATUS;
END;

IF OBJECT_ID('dbo.PRODUCT_TRANSACTION') IS NOT NULL
BEGIN
    SELECT DOCUMENT_NBR,DOCUMENT_TYPE,VOUCHER_TYPE,COUNT(*) AS row_count,
           CASE WHEN DOCUMENT_TYPE='SAN' AND COUNT(*)>1
                THEN 'INVESTIGATE: shared adjustment document number' ELSE 'PASS/INFORMATION' END AS status
    FROM dbo.PRODUCT_TRANSACTION
    WHERE DOCUMENT_TYPE IN ('GRN','SAN')
       OR TRANSACTION_TYPE IN ('WORKSHOP_ISSUE','WORKSHOP_RECEIPT')
    GROUP BY DOCUMENT_NBR,DOCUMENT_TYPE,VOUCHER_TYPE;
END;

IF OBJECT_ID('dbo.VOUCHER') IS NOT NULL
BEGIN
    SELECT mode,trans_type,voucher_type,COUNT(*) AS row_count
    FROM dbo.VOUCHER
    GROUP BY mode,trans_type,voucher_type
    ORDER BY trans_type,voucher_type,mode;
END;

IF OBJECT_ID('dbo.INVOICE_AR_RECEIPTS') IS NOT NULL
BEGIN
    SELECT mode_of_receipt,transaction_type,status,COUNT(*) AS row_count
    FROM dbo.INVOICE_AR_RECEIPTS
    GROUP BY mode_of_receipt,transaction_type,status
    ORDER BY transaction_type,mode_of_receipt,status;

    SELECT invoice_receipt_nbr,COUNT(*) AS row_count,
           'INVESTIGATE: duplicate derived receipt reference' AS status
    FROM dbo.INVOICE_AR_RECEIPTS
    WHERE invoice_receipt_nbr IS NOT NULL
    GROUP BY invoice_receipt_nbr
    HAVING COUNT(*)>1;
END;

IF OBJECT_ID('dbo.MTBL_REFERENCES') IS NOT NULL AND OBJECT_ID('dbo.ORG_CUSTOMER') IS NOT NULL
BEGIN
    SELECT r.GKEY,r.REF_CODE,r.REF_VALUE,
           CASE WHEN c.GKEY IS NULL THEN 'FAIL: no customer mobile match' ELSE 'PASS' END AS old_metal_destination_status
    FROM dbo.MTBL_REFERENCES r
    LEFT JOIN dbo.ORG_CUSTOMER c ON c.MOBILE_NBR=r.REF_VALUE
    WHERE r.REF_NAME='STOCK_TRANSFER' AND r.IS_ACTIVE=1;
END;

IF OBJECT_ID('dbo.PRODUCT') IS NOT NULL
BEGIN
    SELECT GKEY,ID,NAME,METAL,PURITY,UOM,HSN_CODE,IS_TAXABLE,
           CASE WHEN NULLIF(LTRIM(RTRIM(ID)),'') IS NULL OR NULLIF(LTRIM(RTRIM(METAL)),'') IS NULL
                  OR NULLIF(LTRIM(RTRIM(PURITY)),'') IS NULL OR NULLIF(LTRIM(RTRIM(UOM)),'') IS NULL
                THEN 'FAIL: inventory attributes'
                WHEN NULLIF(LTRIM(RTRIM(HSN_CODE)),'') IS NULL THEN 'INVESTIGATE: HSN missing'
                WHEN UOM <> 'Grams' THEN 'INVESTIGATE: no proven GST UQC mapping'
                ELSE 'PASS' END AS status
    FROM dbo.PRODUCT WHERE IS_ACTIVE=1
      AND (NULLIF(LTRIM(RTRIM(ID)),'') IS NULL OR NULLIF(LTRIM(RTRIM(METAL)),'') IS NULL
        OR NULLIF(LTRIM(RTRIM(PURITY)),'') IS NULL OR NULLIF(LTRIM(RTRIM(UOM)),'') IS NULL
        OR NULLIF(LTRIM(RTRIM(HSN_CODE)),'') IS NULL OR UOM<>'Grams');
END;

SELECT 'FINANCIAL_YEAR numbering policy' AS check_name,
       CASE WHEN COL_LENGTH('dbo.VOUCHER_TYPES','FINANCIAL_YEAR') IS NULL
            THEN 'INVESTIGATE: no column in verified schema' ELSE 'INVESTIGATE: column exists; verify behavior' END AS status;
