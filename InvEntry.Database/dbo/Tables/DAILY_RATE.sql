﻿CREATE TABLE [dbo].[DAILY_RATE] (
    [GKEY]           BIGINT         IDENTITY (1, 1) NOT NULL,
    [EFFECTIVE_DATE] DATETIME2 (7)  NOT NULL,
    [METAL]          NVARCHAR (20)  NULL,
    [PURITY]         NVARCHAR (20)  NULL,
    [CARAT]          NVARCHAR (20)  NULL,
    [PRICE]          DECIMAL (9, 2) NULL,
    [CREATED_BY]     NVARCHAR (30)  NULL,
    [CREATED_ON]     DATETIME2 (7)  NULL,
    [MODIFIED_BY]    NVARCHAR (20)  NULL,
    [MODIFIED_ON]    DATETIME2 (7)  NULL,
    [IS_DISPLAY]     BIT            CONSTRAINT [DF_DAILY_RATE_IS_DISPLAY] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_DAILY_RATE] PRIMARY KEY CLUSTERED ([GKEY] ASC)
);
