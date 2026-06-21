CREATE TABLE [dbo].[StockVerifyScan] (
    [Gkey]      INT            IDENTITY (1, 1) NOT NULL,
    [SessionId] BIGINT         NOT NULL,
    [Barcode]   VARCHAR (50)   NOT NULL,
    [ScanTime]  DATETIME2 (7)  CONSTRAINT [DF__StockVeri__ScanT__1B9317B3] DEFAULT (sysdatetime()) NOT NULL,
    [Status]    VARBINARY (50) NULL,
    CONSTRAINT [PK__StockVer__1630EB62E2A9B55D] PRIMARY KEY CLUSTERED ([Gkey] ASC)
);

