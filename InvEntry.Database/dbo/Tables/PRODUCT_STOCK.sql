﻿CREATE TABLE [dbo].[PRODUCT_STOCK] (
    [GKEY]                  BIGINT          IDENTITY (2003, 1) NOT NULL,
    [TENANT_GKEY]           VARBINARY (255) NULL,
    [BASE_UNIT]             VARCHAR (255)   NULL,
    [BRAND]                 VARCHAR (255)   NULL,
    [PRODUCT_GKEY]          BIGINT          NULL,
    [HSN_CODE]              VARCHAR (255)   NULL,
    [METAL]                 VARCHAR (255)   NULL,
    [MODEL]                 VARCHAR (255)   NULL,
    [UOM]                   VARCHAR (255)   NULL,
    [GROSS_WEIGHT]          DECIMAL (9, 2)  NULL,
    [NET_WEIGHT]            DECIMAL (9, 2)  NULL,
    [OTHER_WEIGHT]          DECIMAL (9, 2)  NULL,
    [PRODUCT_DESC]          VARCHAR (255)   NULL,
    [PRODUCT_IMAGE_REF]     VARCHAR (255)   NULL,
    [QTY]                   INT             NULL,
    [SET_ID_GKEY]           VARCHAR (255)   NULL,
    [STATUS]                VARCHAR (255)   NULL,
    [STOCK_ID]              BIGINT          NULL,
    [SUPPLIER_ID]           BIGINT          NULL,
    [TAX_RULE]              NVARCHAR (50)   NULL,
    [TAXABLE]               BIT             NULL,
    [ACTIVE_FOR_SALE]       BIT             NULL,
    [CREATED_BY]            VARCHAR (30)    NULL,
    [CREATED_ON]            DATETIME2 (6)   NULL,
    [MODIFIED_BY]           VARCHAR (30)    NULL,
    [MODIFIED_ON]           DATETIME2 (6)   NULL,
    [DELETED_FLAG]          BIT             NULL,
    [PURCHASE_REF]          NVARCHAR (50)   NULL,
    [PRODUCT_ID]            VARCHAR (20)    NULL,
    [VA_PERCENT]            DECIMAL (9, 2)  NULL,
    [PRODUCT_NAME]          NVARCHAR (200)  NULL,
    [PRODUCT_PURITY]        NVARCHAR (10)   NULL,
    [PRODUCT_CATEGORY]      NVARCHAR (50)   NULL,
    [SUPPLIER_RATE]         DECIMAL (9, 2)  NULL,
    [MAKING_CHARGES]        DECIMAL (9, 2)  NULL,
    [CUSTOMER_ORDER_REF_ID] NVARCHAR (50)   NULL,
    [SIZE]                  INT             NULL,
    [SIZE_ID]               NVARCHAR (10)   NULL,
    [SIZE_UOM]              NVARCHAR (5)    NULL,
    [WASTAGE_AMOUNT]        DECIMAL (9, 2)  NULL,
    [WASTAGE_PERCENT]       DECIMAL (9, 2)  NULL,
    [PRODUCT_SKU]           NVARCHAR (50)   NULL,
    [ORIGINAL_GROSS_WIEGHT] DECIMAL (9, 2)  NULL,
    [ADJUSTED_WIEGHT]       DECIMAL (9, 2)  NULL,
    [DOC_REF]               NVARCHAR (50)   NULL,
    [DOC_DATE]              DATETIME        NULL,
    [PRODUCT_SOLD]          BIT             NULL,
    CONSTRAINT [PK_PRODUCT_STOCK] PRIMARY KEY CLUSTERED ([GKEY] ASC)
);
