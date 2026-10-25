CREATE TABLE [dbo].[StockVerifyScan] (
    [Gkey]      INT           IDENTITY (1, 1) NOT NULL,
    [SessionId] BIGINT        NOT NULL,
    [Barcode]   VARCHAR (50)  NOT NULL,
    [ScanTime]  DATETIME2 (7) CONSTRAINT [DF__StockVeri__ScanT__756D6ECB] DEFAULT (sysdatetime()) NOT NULL,
    [Status]    VARCHAR (50)  NULL,
    CONSTRAINT [PK__StockVer__1630EB62F93C6C28] PRIMARY KEY CLUSTERED ([Gkey] ASC)
);
GO
