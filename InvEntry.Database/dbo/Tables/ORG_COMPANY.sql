﻿CREATE TABLE [dbo].[ORG_COMPANY] (
    [GKEY]            INT           NOT NULL,
    [ACCOUNT_ID]      VARCHAR (25)  NULL,
    [NAME]            VARCHAR (255) NULL,
    [DRAFT_ID]        BIGINT        NULL,
    [INV_ID]          BIGINT        NULL,
    [PAN_NBR]         VARCHAR (15)  NULL,
    [NOTES]           VARCHAR (255) NULL,
    [SERVICE_TAX_NBR] VARCHAR (15)  NULL,
    [STATUS]          VARCHAR (15)  NULL,
    [DELETE_FLAG]     SMALLINT      NULL,
    [CREATED_BY]      VARCHAR (30)  NULL,
    [CREATED_ON]      DATETIME2 (6) NULL,
    [MODIFIED_BY]     VARCHAR (30)  NULL,
    [MODIFIED_ON]     DATETIME2 (6) NULL,
    [TENANT_GKEY]     BIGINT        NOT NULL,
    [TAGLINE]         VARCHAR (250) NULL,
    [CIN_NBR]         VARCHAR (50)  NULL,
    [TIN_NBR]         VARCHAR (50)  NULL,
    [ADDRESS_GKEY]    INT           NULL,
    [GST_NBR]         VARCHAR (20)  NULL,
    [THIS_COMPANY]    BIT           NULL,
    CONSTRAINT [ORG_COMPANY_PK] PRIMARY KEY CLUSTERED ([GKEY] ASC)
);

