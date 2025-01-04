CREATE TABLE [dbo].[ORG_BANK_DETAILS] (
    [GKEY]              INT           NOT NULL,
    [BANK_NAME]         VARCHAR (200) NULL,
    [BRANCH_NAME]       VARCHAR (200) NULL,
    [IFSC_CODE]         VARCHAR (20)  NOT NULL,
    [BANK_ACCOUNT_NBR]  VARCHAR (200) NULL,
    [BANK_CUST_REF_NBR] VARCHAR (200) NULL,
    [IS_PRIMARY]        VARCHAR (1)   NULL,
    [MOBILE_ENABLED]    VARCHAR (1)   NULL,
    [USE_BY_REF_GKEY]   BIGINT        NULL,
    [USE_BY_REF_NAME]   VARCHAR (200) NULL,
    [ONLINE_ENABLED]    VARCHAR (1)   NULL,
    [LOCATION_GKEY]     BIGINT        NULL,
    CONSTRAINT [BANK_DETAILS_PK] PRIMARY KEY CLUSTERED ([GKEY] ASC)
);




GO



GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = 'True/  False', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'ORG_BANK_DETAILS', @level2type = N'COLUMN', @level2name = N'ONLINE_ENABLED';

