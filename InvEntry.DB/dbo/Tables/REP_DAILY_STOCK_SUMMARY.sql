CREATE TABLE [dbo].[REP_DAILY_STOCK_SUMMARY] (
    [TRANSACTION_DATE]       DATETIME2 (7)   NULL,
    [PRODUCT_CATEGORY]       VARCHAR (50)    NULL,
    [OPENING_STOCK]          DECIMAL (18, 3) NULL,
    [STOCK_IN]               DECIMAL (18, 3) NULL,
    [STOCK_TRANSFER_IN]      DECIMAL (18, 3) NULL,
    [STOCK_OUT]              DECIMAL (18, 3) NULL,
    [STOCK_TRANSFER_OUT]     DECIMAL (18, 3) NULL,
    [CLOSING_STOCK]          DECIMAL (18, 3) NULL,
    [METAL]                  VARCHAR (50)    NULL,
    [gkey]                   INT             IDENTITY (1, 1) NOT NULL,
    [OPENING_STOCK_QTY]      INT             NULL,
    [STOCK_IN_QTY]           INT             NULL,
    [STOCK_TRANSFER_IN_QTY]  INT             NULL,
    [STOCK_OUT_QTY]          INT             NULL,
    [STOCK_TRANSFER_OUT_QTY] INT             NULL,
    [CLOSING_STOCK_QTY]      INT             NULL,
    CONSTRAINT [PK_REP_DAILY_STOCK_SUMMARY] PRIMARY KEY CLUSTERED ([gkey] ASC)
);

