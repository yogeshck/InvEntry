﻿CREATE TABLE [dbo].[PRODUCT_STOCK_SUMMARY] (
    [GKEY]                  INT             IDENTITY (1, 1) NOT NULL,
    [CATEGORY]              VARCHAR (50)    NULL,
    [PRODUCT_GKEY]          INT             NULL,
    [PRODUCT_SKU]           VARCHAR (50)    NULL,
    [GROSS_WEIGHT]          DECIMAL (10, 3) NULL,
    [STONE_WEIGHT]          DECIMAL (10, 3) NULL,
    [NET_WEIGHT]            DECIMAL (10, 3) NULL,
    [SUPPLIED_GROSS_WEIGHT] DECIMAL (10, 3) NULL,
    [ADJUSTED_WEIGHT]       DECIMAL (10, 3) NULL,
    [SOLD_WEIGHT]           DECIMAL (10, 3) NULL,
    [BALANCE_WEIGHT]        DECIMAL (10, 3) NULL,
    [SUPPLIED_QTY]          INT             NULL,
    [ADJUSTED_QTY]          INT             NULL,
    [SOLD_QTY]              INT             NULL,
    [STOCK_QTY]             INT             NULL,
    [STATUS]                VARCHAR (50)    NULL,
    [VA_PERCENT]            DECIMAL (4, 2)  NULL,
    [WASTAGE_PERCENT]       DECIMAL (4, 2)  NULL,
    [WASTAGE_AMOUNT]        DECIMAL (10, 2) NULL,
    [UOM]                   VARCHAR (50)    NULL,
    [CREATED_BY]            VARCHAR (30)    NULL,
    [CREATED_ON]            DATETIME2 (6)   NULL,
    [MODIFIED_BY]           VARCHAR (30)    NULL,
    [MODIFIED_ON]           DATETIME2 (6)   NULL,
    CONSTRAINT [PK_PRODUCT_STOCK_SUMMARY] PRIMARY KEY CLUSTERED ([GKEY] ASC)
);

