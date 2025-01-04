CREATE TABLE [dbo].[MTBL_LEDGERS] (
    [GKEY]                INT            IDENTITY (99, 1) NOT NULL,
    [LEDGER_NAME]         NVARCHAR (MAX) NULL,
    [DESCRIPTION]         VARCHAR (MAX)  NULL,
    [ACCOUNT_GROUP_NAME]  VARCHAR (MAX)  NULL,
    [STATUS]              BIT            NULL,
    [SUB_GROUP]           INT            NULL,
    [PRIMARY_GROUP]       INT            NULL,
    [MEMBER_GKEY]         INT            NULL,
    [MEMBER_TYPE]         NVARCHAR (MAX) NULL,
    [TENANT_GKEY]         INT            NULL,
    [LEDGER_ACCOUNT_CODE] INT            NULL,
    CONSTRAINT [PK_MTBL_LEDGERS] PRIMARY KEY CLUSTERED ([GKEY] ASC)
);

