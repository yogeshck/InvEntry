﻿CREATE TABLE [dbo].[INVOICE_HEADER] (
    [GKEY]               INT             IDENTITY (15000, 1) NOT NULL,
    [INV_NBR]            VARCHAR (50)    NULL,
    [INV_DATE]           DATETIME2 (6)   NULL,
    [PAYMENT_MODE]       VARCHAR (50)    NULL,
    [CUST_GKEY]          INT             NULL,
    [CUST_MOBILE]        VARCHAR (50)    NULL,
    [INV_REFUND]         DECIMAL (10, 2) CONSTRAINT [DF__INVOICE_H__INV_R__534D60F1] DEFAULT ('0.00') NULL,
    [INV_TAXABLE_AMOUNT] DECIMAL (10, 2) CONSTRAINT [DF__INVOICE_H__INV_T__5441852A] DEFAULT ('0.00') NULL,
    [IS_TAX_APPLICABLE]  BIT             CONSTRAINT [DF_INVOICE_HEADER_IS_TAX_APPLICABLE] DEFAULT ((1)) NOT NULL,
    [OLD_GOLD_AMOUNT]    DECIMAL (10, 2) CONSTRAINT [DF__INVOICE_H__OLD_G__5535A963] DEFAULT ('0.00') NULL,
    [OLD_SILVER_AMOUNT]  DECIMAL (10, 2) CONSTRAINT [DF__INVOICE_H__OLD_S__5629CD9C] DEFAULT ('0.00') NULL,
    [TAX_TYPE]           VARCHAR (50)    NULL,
    [GST_LOC_SELLER]     VARCHAR (50)    NULL,
    [GST_LOC_BUYER]      VARCHAR (50)    NULL,
    [CGST_PERCENT]       DECIMAL (4, 2)  CONSTRAINT [DF__INVOICE_H__CGST___571DF1D5] DEFAULT ('0.00') NULL,
    [CGST_AMOUNT]        DECIMAL (10, 2) CONSTRAINT [DF__INVOICE_H__CGST___5812160E] DEFAULT ('0.00') NULL,
    [SGST_PERCENT]       DECIMAL (4, 2)  CONSTRAINT [DF__INVOICE_H__SGST___59063A47] DEFAULT ('0.00') NULL,
    [SGST_AMOUNT]        DECIMAL (10, 2) CONSTRAINT [DF__INVOICE_H__SGST___59FA5E80] DEFAULT ('0.00') NULL,
    [IGST_PERCENT]       DECIMAL (4, 2)  CONSTRAINT [DF__INVOICE_H__IGST___5AEE82B9] DEFAULT ('0.00') NULL,
    [IGST_AMOUNT]        DECIMAL (10, 2) CONSTRAINT [DF__INVOICE_H__IGST___5BE2A6F2] DEFAULT ('0.00') NULL,
    [AMOUNT_PAYABLE]     DECIMAL (10, 2) CONSTRAINT [DF__INVOICE_H__AMOUN__5CD6CB2B] DEFAULT ('0.00') NULL,
    [DISCOUNT_PERCENT]   DECIMAL (10, 2) CONSTRAINT [DF__INVOICE_H__DISCO__5DCAEF64] DEFAULT ('0.00') NULL,
    [DISCOUNT_AMOUNT]    DECIMAL (10, 2) CONSTRAINT [DF__INVOICE_H__DISCO__5EBF139D] DEFAULT ('0.00') NULL,
    [ADVANCE_ADJ]        DECIMAL (10, 2) CONSTRAINT [DF__INVOICE_H__ADVAN__5FB337D6] DEFAULT ('0.00') NULL,
    [PAYMENT_DUE_DATE]   DATETIME2 (6)   NULL,
    [RD_AMOUNT_ADJ]      DECIMAL (10, 2) CONSTRAINT [DF__INVOICE_H__RD_AM__60A75C0F] DEFAULT ('0.00') NULL,
    [RECD_AMOUNT]        DECIMAL (10, 2) CONSTRAINT [DF__INVOICE_H__RECD___619B8048] DEFAULT ('0.00') NULL,
    [INV_BALANCE]        DECIMAL (10, 2) CONSTRAINT [DF__INVOICE_H__INV_B__628FA481] DEFAULT ('0.00') NULL,
    [ROUND_OFF]          DECIMAL (10, 2) CONSTRAINT [DF__INVOICE_H__ROUND__6383C8BA] DEFAULT ('0.00') NULL,
    [INV_NOTES]          VARCHAR (250)   NULL,
    [CREATED_BY]         VARCHAR (50)    NULL,
    [CREATED_ON]         DATETIME2 (6)   NULL,
    [MODIFIED_BY]        VARCHAR (50)    NULL,
    [MODIFIED_ON]        DATETIME2 (6)   NULL,
    [TENANT_GKEY]        INT             NULL,
    [DELIVERY_METHOD]    VARCHAR (50)    NULL,
    [DELIVERY_REF]       INT             NULL,
    [ORDER_NBR]          VARCHAR (50)    NULL,
    [ORDER_DATE]         DATETIME2 (6)   NULL,
    [GROSS_RCB_AMOUNT]   DECIMAL (18, 2) NULL,
    [INVL_TAX_TOTAL]     DECIMAL (18, 2) NULL,
    [SALES_PERSON]       NVARCHAR (50)   NULL,
    CONSTRAINT [PK_INVOICE_HEADER] PRIMARY KEY CLUSTERED ([GKEY] ASC)
);

