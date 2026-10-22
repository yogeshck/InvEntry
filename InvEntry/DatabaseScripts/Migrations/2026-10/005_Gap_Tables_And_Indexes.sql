/*
    InvEntry Database Migration
    File: 005_Gap_Tables_And_Indexes.sql

    Purpose:
        Brings a branch database up to the schema the current code expects, for the
        objects that older production backups do not have:

        Section 1  Stock adjustment and stock transfer tables (with their indexes).
        Section 2  Stock report tables (DAILY_REP_STOCK_MOVEMENT, DAILY_REP_STOCK_SUMRY_MOVEMENT).
        Section 3  PRODUCT_STOCK_TMP.
        Section 4  GST_GSTR1_DOCUMENT and GST_GSTR1_DOCUMENT_LINE (GSTR-1 staging).
        Section 5  FINANCE_SYNC_OUTBOX (finance sync queue, with its indexes).

    Not included (run separately, see the runbook order in the header below):
        001_Add_Invoice_Report_Indexes.sql
        002_Add_Customer_Advance_And_Schemes.sql   (advance and scheme tables, CUSTOMER_ADVANCE_BALANCE_CHECK_VIEW)
        DataAccess/Sql/Migrations/20261011_FinanceSyncOutboxLease.sql
        DataAccess/Sql/Migrations/20261012_FinanceClosingFence.sql
        004_Add_Lookup_Indexes_And_Primary_Keys.sql
        DataAccess/Sql/Migrations/20261014_NormalizeInvoiceStatusToFinalised.sql (changes data; manual)

    Run order on a branch database:
        1. 005_Gap_Tables_And_Indexes.sql          (this file)
        2. 001_Add_Invoice_Report_Indexes.sql
        3. 002_Add_Customer_Advance_And_Schemes.sql
        4. 20261011_FinanceSyncOutboxLease.sql
        5. 20261012_FinanceClosingFence.sql
        6. 004_Add_Lookup_Indexes_And_Primary_Keys.sql
        7. 20261014_NormalizeInvoiceStatusToFinalised.sql (only after review and a backup)

    Safe to rerun: Yes. Every table is created only when missing. An existing table
    is left unchanged, so its indexes are not added here.
*/

SET NOCOUNT ON;
GO

/* ---- Section 1: STOCK_ADJUSTMENT_HEADER ---- */
IF OBJECT_ID(N'dbo.STOCK_ADJUSTMENT_HEADER', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[STOCK_ADJUSTMENT_HEADER] (
        [GKEY]                 INT             IDENTITY (1, 1) NOT NULL,
        [ADJUSTMENT_NBR]       VARCHAR (50)    NOT NULL,
        [ADJUSTMENT_DATE]      DATETIME2 (6)   NOT NULL,
        [ADJUSTMENT_TYPE]      VARCHAR (20)    NOT NULL,
        [REASON_CODE]          VARCHAR (50)    NOT NULL,
        [STATUS]               VARCHAR (20)    NOT NULL,
        [REMARKS]              VARCHAR (500)   NULL,
        [TOTAL_QTY]            INT             NOT NULL,
        [TOTAL_GROSS_WEIGHT]   DECIMAL (18, 3) NOT NULL,
        [TOTAL_STONE_WEIGHT]   DECIMAL (18, 3) NOT NULL,
        [TOTAL_NET_WEIGHT]     DECIMAL (18, 3) NOT NULL,
        [CREATED_BY]           VARCHAR (50)    NULL,
        [CREATED_ON]           DATETIME2 (6)   NOT NULL,
        [MODIFIED_BY]          VARCHAR (50)    NULL,
        [MODIFIED_ON]          DATETIME2 (6)   NULL,
        [FINALISED_ON]         DATETIME2 (6)   NULL,
        CONSTRAINT [PK_STOCK_ADJUSTMENT_HEADER] PRIMARY KEY CLUSTERED ([GKEY] ASC),
        CONSTRAINT [CK_STOCK_ADJUSTMENT_HEADER_TYPE]
            CHECK ([ADJUSTMENT_TYPE] IN ('INCREASE', 'DECREASE', 'REALLOCATION')),
        CONSTRAINT [CK_STOCK_ADJUSTMENT_HEADER_STATUS]
            CHECK ([STATUS] IN ('DRAFT', 'POSTED')),
        CONSTRAINT [CK_STOCK_ADJUSTMENT_HEADER_TOTALS]
            CHECK ([TOTAL_QTY] >= 0 AND [TOTAL_GROSS_WEIGHT] >= 0 AND [TOTAL_STONE_WEIGHT] >= 0 AND [TOTAL_NET_WEIGHT] >= 0)
    );
    
    CREATE UNIQUE NONCLUSTERED INDEX [UX_STOCK_ADJUSTMENT_HEADER_ADJUSTMENT_NBR]
        ON [dbo].[STOCK_ADJUSTMENT_HEADER] ([ADJUSTMENT_NBR] ASC);
    
    CREATE NONCLUSTERED INDEX [IX_STOCK_ADJUSTMENT_HEADER_DATE]
        ON [dbo].[STOCK_ADJUSTMENT_HEADER] ([ADJUSTMENT_DATE] DESC);
    
    CREATE NONCLUSTERED INDEX [IX_STOCK_ADJUSTMENT_HEADER_STATUS_TYPE]
        ON [dbo].[STOCK_ADJUSTMENT_HEADER] ([STATUS] ASC, [ADJUSTMENT_TYPE] ASC);
END;
GO

/* ---- Section 1: STOCK_ADJUSTMENT_LINE ---- */
IF OBJECT_ID(N'dbo.STOCK_ADJUSTMENT_LINE', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[STOCK_ADJUSTMENT_LINE] (
        [GKEY]                  INT             IDENTITY (1, 1) NOT NULL,
        [ADJUSTMENT_HDR_GKEY]   INT             NOT NULL,
        [LINE_NBR]              INT             NOT NULL,
        [DIRECTION]             VARCHAR (10)    NOT NULL,
        [STOCK_LEVEL]           VARCHAR (20)    NOT NULL,
        [MOVEMENT_KIND]         VARCHAR (20)    NOT NULL,
        [PRODUCT_GKEY]          INT             NOT NULL,
        [PRODUCT_STOCK_GKEY]    INT             NULL,
        [PRODUCT_SKU]           VARCHAR (50)    NULL,
        [PRODUCT_CATEGORY]      VARCHAR (255)   NOT NULL,
        [METAL]                 VARCHAR (50)    NULL,
        [PURITY]                VARCHAR (255)   NULL,
        [UOM]                   VARCHAR (50)    NULL,
        [QTY]                   INT             NOT NULL,
        [GROSS_WEIGHT]          DECIMAL (18, 3) NOT NULL,
        [STONE_WEIGHT]          DECIMAL (18, 3) NOT NULL,
        [NET_WEIGHT]            DECIMAL (18, 3) NOT NULL,
        [PAIR_NBR]              INT             NULL,
        [NOTES]                 VARCHAR (500)   NULL,
        CONSTRAINT [PK_STOCK_ADJUSTMENT_LINE] PRIMARY KEY CLUSTERED ([GKEY] ASC),
        CONSTRAINT [FK_STOCK_ADJUSTMENT_LINE_HEADER]
            FOREIGN KEY ([ADJUSTMENT_HDR_GKEY]) REFERENCES [dbo].[STOCK_ADJUSTMENT_HEADER] ([GKEY]),
        CONSTRAINT [FK_STOCK_ADJUSTMENT_LINE_PRODUCT_STOCK]
            FOREIGN KEY ([PRODUCT_STOCK_GKEY]) REFERENCES [dbo].[PRODUCT_STOCK] ([GKEY]),
        CONSTRAINT [CK_STOCK_ADJUSTMENT_LINE_DIRECTION]
            CHECK ([DIRECTION] IN ('IN', 'OUT')),
        CONSTRAINT [CK_STOCK_ADJUSTMENT_LINE_STOCK_LEVEL]
            CHECK ([STOCK_LEVEL] IN ('CONSOLIDATED', 'SKU')),
        CONSTRAINT [CK_STOCK_ADJUSTMENT_LINE_MOVEMENT_KIND]
            CHECK ([MOVEMENT_KIND] IN ('WEIGHT', 'WHOLE_ITEM')),
        CONSTRAINT [CK_STOCK_ADJUSTMENT_LINE_VALUES]
            CHECK ([QTY] >= 0 AND [GROSS_WEIGHT] >= 0 AND [STONE_WEIGHT] >= 0 AND [NET_WEIGHT] >= 0),
        CONSTRAINT [CK_STOCK_ADJUSTMENT_LINE_SKU_SCOPE]
            CHECK (
                ([STOCK_LEVEL] = 'CONSOLIDATED' AND [PRODUCT_STOCK_GKEY] IS NULL AND [PRODUCT_SKU] IS NULL)
                OR
                ([STOCK_LEVEL] = 'SKU' AND [PRODUCT_STOCK_GKEY] IS NOT NULL AND [PRODUCT_SKU] IS NOT NULL)
            )
    );
    
    CREATE UNIQUE NONCLUSTERED INDEX [UX_STOCK_ADJUSTMENT_LINE_HEADER_LINE]
        ON [dbo].[STOCK_ADJUSTMENT_LINE] ([ADJUSTMENT_HDR_GKEY] ASC, [LINE_NBR] ASC);
    
    CREATE NONCLUSTERED INDEX [IX_STOCK_ADJUSTMENT_LINE_PRODUCT]
        ON [dbo].[STOCK_ADJUSTMENT_LINE] ([PRODUCT_GKEY] ASC, [PRODUCT_SKU] ASC);
    
    CREATE NONCLUSTERED INDEX [IX_STOCK_ADJUSTMENT_LINE_PRODUCT_STOCK]
        ON [dbo].[STOCK_ADJUSTMENT_LINE] ([PRODUCT_STOCK_GKEY] ASC)
        WHERE [PRODUCT_STOCK_GKEY] IS NOT NULL;
END;
GO

/* ---- Section 1: STOCK_TRANSFER_HEADER ---- */
IF OBJECT_ID(N'dbo.STOCK_TRANSFER_HEADER', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[STOCK_TRANSFER_HEADER] (
        [GKEY]                 INT             IDENTITY (1, 1) NOT NULL,
        [TRANSFER_NBR]         VARCHAR (50)    NOT NULL,
        [TRANSFER_DATE]        DATETIME2 (6)   NOT NULL,
        [TRANSFER_TYPE]        VARCHAR (20)    NOT NULL,
        [FROM_BRANCH]          VARCHAR (255)   NOT NULL,
        [FROM_TENANT_GKEY]     INT             NULL,
        [TO_REFERENCE_GKEY]    INT             NOT NULL,
        [TO_REFERENCE_CODE]    VARCHAR (50)    NOT NULL,
        [TO_REFERENCE_VALUE]   VARCHAR (50)    NOT NULL,
        [STATUS]               VARCHAR (20)    NOT NULL,
        [REMARKS]              VARCHAR (255)   NULL,
        [TOTAL_QTY]            INT             NOT NULL,
        [TOTAL_GROSS_WEIGHT]   DECIMAL (18, 3) NOT NULL,
        [TOTAL_STONE_WEIGHT]   DECIMAL (18, 3) NOT NULL,
        [TOTAL_NET_WEIGHT]     DECIMAL (18, 3) NOT NULL,
        [CREATED_BY]           VARCHAR (50)    NULL,
        [CREATED_ON]           DATETIME2 (6)   NOT NULL,
        [MODIFIED_BY]          VARCHAR (50)    NULL,
        [MODIFIED_ON]          DATETIME2 (6)   NULL,
        CONSTRAINT [PK_STOCK_TRANSFER_HEADER] PRIMARY KEY CLUSTERED ([GKEY] ASC),
        CONSTRAINT [FK_STOCK_TRANSFER_HEADER_MTblReferences]
            FOREIGN KEY ([TO_REFERENCE_GKEY]) REFERENCES [dbo].[MTBL_REFERENCES] ([gkey])
    );
    
    
    CREATE UNIQUE NONCLUSTERED INDEX [UX_STOCK_TRANSFER_HEADER_TRANSFER_NBR]
        ON [dbo].[STOCK_TRANSFER_HEADER] ([TRANSFER_NBR] ASC);
    
    
    CREATE NONCLUSTERED INDEX [IX_STOCK_TRANSFER_HEADER_TRANSFER_DATE]
        ON [dbo].[STOCK_TRANSFER_HEADER] ([TRANSFER_DATE] DESC);
    
    
    CREATE NONCLUSTERED INDEX [IX_STOCK_TRANSFER_HEADER_STATUS_TYPE_DESTINATION]
        ON [dbo].[STOCK_TRANSFER_HEADER] ([STATUS] ASC, [TRANSFER_TYPE] ASC, [TO_REFERENCE_GKEY] ASC);END;
GO

/* ---- Section 1: STOCK_TRANSFER_LINE ---- */
IF OBJECT_ID(N'dbo.STOCK_TRANSFER_LINE', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[STOCK_TRANSFER_LINE] (
        [GKEY]                  INT             IDENTITY (1, 1) NOT NULL,
        [TRANSFER_HDR_GKEY]     INT             NOT NULL,
        [LINE_NBR]              INT             NOT NULL,
        [PRODUCT_STOCK_GKEY]    INT             NULL,
        [PRODUCT_GKEY]          INT             NULL,
        [PRODUCT_ID]            VARCHAR (50)    NULL,
        [PRODUCT_SKU]           VARCHAR (50)    NULL,
        [PRODUCT_NAME]          VARCHAR (255)   NULL,
        [PRODUCT_CATEGORY]      VARCHAR (255)   NULL,
        [METAL]                 VARCHAR (50)    NULL,
        [PURITY]                VARCHAR (255)   NULL,
        [UOM]                   VARCHAR (50)    NULL,
        [QTY]                   INT             NOT NULL,
        [GROSS_WEIGHT]          DECIMAL (18, 3) NOT NULL,
        [STONE_WEIGHT]          DECIMAL (18, 3) NOT NULL,
        [NET_WEIGHT]            DECIMAL (18, 3) NOT NULL,
        [TRANSACTED_RATE]       DECIMAL (18, 2) NULL,
        [TRANSFER_VALUE]        DECIMAL (18, 2) NULL,
        [NOTES]                 VARCHAR (255)   NULL,
        CONSTRAINT [PK_STOCK_TRANSFER_LINE] PRIMARY KEY CLUSTERED ([GKEY] ASC),
        CONSTRAINT [FK_STOCK_TRANSFER_LINE_HEADER]
            FOREIGN KEY ([TRANSFER_HDR_GKEY]) REFERENCES [dbo].[STOCK_TRANSFER_HEADER] ([GKEY]),
        CONSTRAINT [FK_STOCK_TRANSFER_LINE_PRODUCT_STOCK]
            FOREIGN KEY ([PRODUCT_STOCK_GKEY]) REFERENCES [dbo].[PRODUCT_STOCK] ([GKEY])
    );
    
    
    CREATE UNIQUE NONCLUSTERED INDEX [UX_STOCK_TRANSFER_LINE_HEADER_LINE]
        ON [dbo].[STOCK_TRANSFER_LINE] ([TRANSFER_HDR_GKEY] ASC, [LINE_NBR] ASC);
    
    
    CREATE NONCLUSTERED INDEX [IX_STOCK_TRANSFER_LINE_PRODUCT_STOCK]
        ON [dbo].[STOCK_TRANSFER_LINE] ([PRODUCT_STOCK_GKEY] ASC);END;
GO

/* ---- Section 2: DAILY_REP_STOCK_MOVEMENT ---- */
IF OBJECT_ID(N'dbo.DAILY_REP_STOCK_MOVEMENT', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DAILY_REP_STOCK_MOVEMENT] (
        [GKEY]             INT             IDENTITY (1, 1) NOT NULL,
        [REPORT_DATE]      DATE            NOT NULL,
        [BRANCH_ID]        INT             NOT NULL,
        [TYPE]             VARCHAR (50)    NOT NULL,
        [REF_NBR]          VARCHAR (50)    NULL,
        [METAL]            VARCHAR (50)    NULL,
        [PRODUCT]          VARCHAR (50)    NOT NULL,
        [IN_QTY]           DECIMAL (18, 3) DEFAULT ((0)) NOT NULL,
        [IN_GROSS_WEIGHT]  DECIMAL (18, 3) DEFAULT ((0)) NOT NULL,
        [IN_STONE_WEIGHT]  DECIMAL (18, 3) DEFAULT ((0)) NOT NULL,
        [IN_NET_WEIGHT]    DECIMAL (18, 3) DEFAULT ((0)) NOT NULL,
        [OUT_QTY]          DECIMAL (18, 3) DEFAULT ((0)) NOT NULL,
        [OUT_GROSS_WEIGHT] DECIMAL (18, 3) DEFAULT ((0)) NOT NULL,
        [OUT_STONE_WEIGHT] DECIMAL (18, 3) DEFAULT ((0)) NOT NULL,
        [OUT_NET_WEIGHT]   DECIMAL (18, 3) DEFAULT ((0)) NOT NULL,
        CONSTRAINT [PK_DAILY_REP_STOCK_MOVEMENT] PRIMARY KEY CLUSTERED ([GKEY] ASC)
    );
END;
GO

/* ---- Section 2: DAILY_REP_STOCK_SUMRY_MOVEMENT ---- */
IF OBJECT_ID(N'dbo.DAILY_REP_STOCK_SUMRY_MOVEMENT', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DAILY_REP_STOCK_SUMRY_MOVEMENT] (
        [GKEY]             INT             IDENTITY (1, 1) NOT NULL,
        [REPORT_DATE]      DATE            NOT NULL,
        [BRANCH_ID]        INT             NOT NULL,
        [PRODUCT]          VARCHAR (50)    NULL,
        [METAL]            VARCHAR (50)    NULL,
        [OPENING_QTY]      DECIMAL (18, 3) DEFAULT ((0)) NOT NULL,
        [OPENING_WEIGHT]   DECIMAL (18, 3) DEFAULT ((0)) NOT NULL,
        [IN_QTY]           DECIMAL (18, 3) DEFAULT ((0)) NOT NULL,
        [IN_GROSS_WEIGHT]  DECIMAL (18, 3) DEFAULT ((0)) NOT NULL,
        [IN_STONE_WEIGHT]  DECIMAL (18, 3) DEFAULT ((0)) NOT NULL,
        [IN_NET_WEIGHT]    DECIMAL (18, 3) DEFAULT ((0)) NOT NULL,
        [OUT_QTY]          DECIMAL (18, 3) DEFAULT ((0)) NOT NULL,
        [OUT_GROSS_WEIGHT] DECIMAL (18, 3) DEFAULT ((0)) NOT NULL,
        [OUT_STONE_WEIGHT] DECIMAL (18, 3) DEFAULT ((0)) NOT NULL,
        [OUT_NET_WEIGHT]   DECIMAL (18, 3) DEFAULT ((0)) NOT NULL,
        [CLOSING_QTY]      DECIMAL (18, 3) DEFAULT ((0)) NOT NULL,
        [CLOSING_WEIGHT]   DECIMAL (18, 3) DEFAULT ((0)) NOT NULL,
        CONSTRAINT [PK_DAILY_REP_STOCK_SUMRY_MOVEMENT] PRIMARY KEY CLUSTERED ([GKEY] ASC)
    );
END;
GO

/* ---- Section 3: PRODUCT_STOCK_TMP ---- */
IF OBJECT_ID(N'dbo.PRODUCT_STOCK_TMP', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PRODUCT_STOCK_TMP (
        GKEY                  INT             IDENTITY (1, 1) NOT NULL,
        STOCK_SUMMARY_GKEY    INT             NULL,
        PRODUCT_GKEY          INT             NULL,
        PRODUCT_SKU           VARCHAR (50)    NULL,
        GROSS_WEIGHT          DECIMAL (10, 3) NULL,
        STONE_WEIGHT          DECIMAL (10, 3) NULL,
        NET_WEIGHT            DECIMAL (10, 3) NULL,
        SUPPLIED_GROSS_WEIGHT DECIMAL (10, 3) NULL,
        ADJUSTED_WEIGHT       DECIMAL (10, 3) NULL,
        SOLD_WEIGHT           DECIMAL (10, 3) NULL,
        BALANCE_WEIGHT        DECIMAL (10, 3) NULL,
        SUPPLIED_QTY          INT             NULL,
        SOLD_QTY              INT             NULL,
        STOCK_QTY             INT             NULL,
        STATUS                VARCHAR (50)    NULL,
        SUPPLIER_ID           VARCHAR (50)    NULL,
        VA_PERCENT            DECIMAL (4, 2)  NULL,
        WASTAGE_PERCENT       DECIMAL (4, 2)  NULL,
        WASTAGE_AMOUNT        DECIMAL (10, 2) NULL,
        SIZE_ID               INT             NULL,
        SIZE                  VARCHAR (50)    NULL,
        SIZE_UOM              VARCHAR (50)    NULL,
        IS_PRODUCT_SOLD       BIT             NULL,
        CREATED_BY            VARCHAR (30)    NULL,
        CREATED_ON            DATETIME2 (6)   NULL,
        MODIFIED_BY           VARCHAR (30)    NULL,
        MODIFIED_ON           DATETIME2 (6)   NULL,
        CATEGORY              VARCHAR (50)    NULL,
        IS_BARCODE_PRINTED    BIT             NULL,
        CONSTRAINT PK_PRODUCT_STOCK_TMP PRIMARY KEY CLUSTERED (GKEY)
    );
END;
GO

/* ---- Section 4: GST_GSTR1_DOCUMENT and GST_GSTR1_DOCUMENT_LINE ---- */
IF OBJECT_ID(N'dbo.GST_GSTR1_DOCUMENT', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GST_GSTR1_DOCUMENT (
        GKEY                     BIGINT         IDENTITY (1, 1) NOT NULL,
        SOURCE_GKEY              INT            NOT NULL,
        DOCUMENT_TYPE            VARCHAR (30)   NOT NULL,
        DOCUMENT_NBR             VARCHAR (50)   NOT NULL,
        DOCUMENT_DATE            DATE           NOT NULL,
        SUPPLIER_GSTIN           VARCHAR (15)   NOT NULL,
        RETURN_PERIOD            CHAR (6)       NOT NULL,
        RECIPIENT_GSTIN          VARCHAR (15)   NULL,
        RECIPIENT_STATE_CODE     CHAR (2)       NULL,
        IS_RECIPIENT_REGISTERED  BIT            NOT NULL,
        PLACE_OF_SUPPLY_CODE     CHAR (2)       NOT NULL,
        SUPPLY_TYPE              VARCHAR (20)   NOT NULL,
        TAX_TYPE                 VARCHAR (20)   NOT NULL,
        RETURN_CATEGORY          VARCHAR (30)   NOT NULL,
        GSTR1_TABLE              VARCHAR (10)   NULL,
        IS_REPORTABLE            BIT            NOT NULL CONSTRAINT DF_GST_GSTR1_DOCUMENT_IS_REPORTABLE DEFAULT (1),
        INVOICE_VALUE            DECIMAL (18, 2) NOT NULL,
        TAXABLE_VALUE            DECIMAL (18, 2) NOT NULL,
        CGST_AMOUNT              DECIMAL (18, 2) NOT NULL,
        SGST_AMOUNT              DECIMAL (18, 2) NOT NULL,
        IGST_AMOUNT              DECIMAL (18, 2) NOT NULL,
        CESS_AMOUNT              DECIMAL (18, 2) NOT NULL,
        IS_REVERSE_CHARGE        BIT            NOT NULL,
        IS_SEZ                   BIT            NOT NULL,
        IS_DEEMED_EXPORT         BIT            NOT NULL,
        IS_ECOMMERCE_SUPPLY      BIT            NOT NULL,
        ECOMMERCE_OPERATOR_GSTIN VARCHAR (15)   NULL,
        IS_AMENDMENT             BIT            NOT NULL,
        ORIGINAL_DOCUMENT_NBR    VARCHAR (50)   NULL,
        ORIGINAL_DOCUMENT_DATE   DATE           NULL,
        STATUS                   VARCHAR (20)   NOT NULL CONSTRAINT DF_GST_GSTR1_DOCUMENT_STATUS DEFAULT ('PENDING'),
        CREATED_ON               DATETIME2 (0)  NOT NULL CONSTRAINT DF_GST_GSTR1_DOCUMENT_CREATED_ON DEFAULT (sysdatetime()),
        UPDATED_ON               DATETIME2 (0)  NULL,
        CONSTRAINT PK_GST_GSTR1_DOCUMENT PRIMARY KEY CLUSTERED (GKEY)
    );

    CREATE UNIQUE NONCLUSTERED INDEX UQ_GST_GSTR1_DOCUMENT_SOURCE
        ON dbo.GST_GSTR1_DOCUMENT (SUPPLIER_GSTIN, DOCUMENT_TYPE, SOURCE_GKEY);
END;
GO

IF OBJECT_ID(N'dbo.GST_GSTR1_DOCUMENT_LINE', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GST_GSTR1_DOCUMENT_LINE (
        GKEY              BIGINT          IDENTITY (1, 1) NOT NULL,
        GST_DOCUMENT_GKEY BIGINT          NOT NULL,
        SOURCE_LINE_GKEY  INT             NULL,
        LINE_NBR          INT             NOT NULL,
        HSN_CODE          VARCHAR (20)    NULL,
        DESCRIPTION       VARCHAR (250)   NULL,
        QUANTITY          DECIMAL (18, 3) NOT NULL,
        TAXABLE_VALUE     DECIMAL (18, 2) NOT NULL,
        GST_RATE          DECIMAL (9, 3)  NOT NULL,
        CGST_RATE         DECIMAL (9, 3)  NOT NULL,
        SGST_RATE         DECIMAL (9, 3)  NOT NULL,
        IGST_RATE         DECIMAL (9, 3)  NOT NULL,
        CGST_AMOUNT       DECIMAL (18, 2) NOT NULL,
        SGST_AMOUNT       DECIMAL (18, 2) NOT NULL,
        IGST_AMOUNT       DECIMAL (18, 2) NOT NULL,
        CESS_AMOUNT       DECIMAL (18, 2) NOT NULL,
        UOM               VARCHAR (20)    NULL,
        UQC               VARCHAR (10)    NULL,
        GST_QUANTITY      DECIMAL (18, 3) NULL,
        CONSTRAINT PK_GST_GSTR1_DOCUMENT_LINE PRIMARY KEY CLUSTERED (GKEY),
        CONSTRAINT FK_GST_GSTR1_DOCUMENT_LINE_DOCUMENT FOREIGN KEY (GST_DOCUMENT_GKEY)
            REFERENCES dbo.GST_GSTR1_DOCUMENT (GKEY)
    );

    CREATE UNIQUE NONCLUSTERED INDEX UQ_GST_GSTR1_DOCUMENT_LINE
        ON dbo.GST_GSTR1_DOCUMENT_LINE (GST_DOCUMENT_GKEY, LINE_NBR);
END;
GO

/* ---- Section 5: FINANCE_SYNC_OUTBOX (finance sync queue) ---- */
IF OBJECT_ID(N'dbo.FINANCE_SYNC_OUTBOX', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FINANCE_SYNC_OUTBOX (
        GKEY              BIGINT         IDENTITY (1, 1) NOT NULL,
        EVENT_TYPE        VARCHAR (40)   NOT NULL,
        SOURCE_TYPE       VARCHAR (40)   NOT NULL,
        SOURCE_GKEY       INT            NOT NULL,
        SOURCE_EVENT_ID   VARCHAR (100)  NOT NULL,
        DOCUMENT_NO       VARCHAR (50)   NULL,
        DOCUMENT_DATE     DATETIME2 (7)  NULL,
        PAYLOAD           NVARCHAR (MAX) NOT NULL,
        STATUS            VARCHAR (20)   NOT NULL CONSTRAINT DF_FINANCE_SYNC_OUTBOX_STATUS DEFAULT ('PENDING'),
        RETRY_COUNT       INT            NOT NULL CONSTRAINT DF_FINANCE_SYNC_OUTBOX_RETRY_COUNT DEFAULT (0),
        CREATED_ON        DATETIME2 (7)  NOT NULL CONSTRAINT DF_FINANCE_SYNC_OUTBOX_CREATED_ON DEFAULT (sysdatetime()),
        LAST_ATTEMPT_ON   DATETIME2 (7)  NULL,
        SENT_ON           DATETIME2 (7)  NULL,
        LAST_ERROR        NVARCHAR (1000) NULL,
        CLAIM_TOKEN       VARCHAR (64)   NULL,
        CLAIM_EXPIRES_ON  DATETIME2 (7)  NULL,
        CONSTRAINT PK_FINANCE_SYNC_OUTBOX PRIMARY KEY CLUSTERED (GKEY)
    );

    CREATE UNIQUE NONCLUSTERED INDEX UX_FINANCE_SYNC_OUTBOX_SOURCE_EVENT
        ON dbo.FINANCE_SYNC_OUTBOX (SOURCE_EVENT_ID);

    CREATE NONCLUSTERED INDEX IX_FINANCE_SYNC_OUTBOX_PENDING
        ON dbo.FINANCE_SYNC_OUTBOX (STATUS, CREATED_ON);

    CREATE NONCLUSTERED INDEX IX_FINANCE_SYNC_OUTBOX_DISPATCH_CLAIM
        ON dbo.FINANCE_SYNC_OUTBOX (STATUS, CLAIM_EXPIRES_ON, CREATED_ON)
        INCLUDE (GKEY, CLAIM_TOKEN);
END;
GO
