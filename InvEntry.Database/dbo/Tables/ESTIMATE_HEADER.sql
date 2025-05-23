﻿CREATE TABLE [dbo].[ESTIMATE_HEADER] (
    [GKEY]               INT             IDENTITY (15000, 1) NOT NULL,
    [EST_NBR]            VARCHAR (50)    NULL,
    [EST_DATE]           DATETIME2 (6)   NULL,
    [PAYMENT_MODE]       VARCHAR (50)    NULL,
    [CUST_GKEY]          INT             NULL,
    [CUST_MOBILE]        VARCHAR (50)    NULL,
    [EST_REFUND]         DECIMAL (10, 2) NULL,
    [EST_TAXABLE_AMOUNT] DECIMAL (10, 2) NULL,
    [IS_TAX_APPLICABLE]  BIT             NOT NULL,
    [GROSS_RCB_AMOUNT]   DECIMAL (18, 2) NULL,
    [OLD_GOLD_AMOUNT]    DECIMAL (10, 2) NULL,
    [OLD_SILVER_AMOUNT]  DECIMAL (10, 2) NULL,
    [TAX_TYPE]           VARCHAR (50)    NULL,
    [GST_LOC_SELLER]     VARCHAR (50)    NULL,
    [GST_LOC_BUYER]      VARCHAR (50)    NULL,
    [CGST_PERCENT]       DECIMAL (4, 2)  NULL,
    [CGST_AMOUNT]        DECIMAL (10, 2) NULL,
    [SGST_PERCENT]       DECIMAL (4, 2)  NULL,
    [SGST_AMOUNT]        DECIMAL (10, 2) NULL,
    [IGST_PERCENT]       DECIMAL (4, 2)  NULL,
    [IGST_AMOUNT]        DECIMAL (10, 2) NULL,
    [AMOUNT_PAYABLE]     DECIMAL (10, 2) NULL,
    [DISCOUNT_PERCENT]   DECIMAL (10, 2) NULL,
    [DISCOUNT_AMOUNT]    DECIMAL (10, 2) NULL,
    [ADVANCE_ADJ]        DECIMAL (10, 2) NULL,
    [PAYMENT_DUE_DATE]   DATETIME2 (6)   NULL,
    [RD_AMOUNT_ADJ]      DECIMAL (10, 2) NULL,
    [EST_BALANCE]        DECIMAL (10, 2) NULL,
    [RECD_AMOUNT]        DECIMAL (10, 2) NULL,
    [ROUND_OFF]          DECIMAL (10, 2) NULL,
    [EST_NOTES]          VARCHAR (250)   NULL,
    [DELIVERY_METHOD]    VARCHAR (50)    NULL,
    [DELIVERY_REF]       INT             NULL,
    [ORDER_NBR]          VARCHAR (50)    NULL,
    [ORDER_DATE]         DATETIME2 (6)   NULL,
    [ESTL_TAX_TOTAL]     DECIMAL (18, 2) NULL,
    [CREATED_BY]         VARCHAR (50)    NULL,
    [CREATED_ON]         DATETIME2 (6)   NULL,
    [MODIFIED_BY]        VARCHAR (50)    NULL,
    [MODIFIED_ON]        DATETIME2 (6)   NULL,
    [TENANT_GKEY]        INT             NULL,
    CONSTRAINT [PK_ESTIMATE_HEADER] PRIMARY KEY CLUSTERED ([GKEY] ASC)
);

