﻿CREATE TABLE [dbo].[ORG_CONTACT_EMAIL] (
    [GKEY]          BIGINT       NOT NULL,
    [CUST_REF_GKEY] FLOAT (53)   NOT NULL,
    [EMAIL_ID]      VARCHAR (50) NULL,
    [ACTIVE_FLAG]   VARCHAR (1)  NULL,
    [STATUS]        VARCHAR (20) NULL,
    CONSTRAINT [EMAIL_PK] PRIMARY KEY CLUSTERED ([GKEY] ASC)
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [EMAIL_PK_2]
    ON [dbo].[ORG_CONTACT_EMAIL]([GKEY] ASC);

