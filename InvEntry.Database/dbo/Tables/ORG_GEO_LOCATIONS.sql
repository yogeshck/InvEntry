﻿CREATE TABLE [dbo].[ORG_GEO_LOCATIONS] (
    [GKEY]          VARCHAR (255) NOT NULL,
    [EXTERNAL_ID]   VARCHAR (255) NULL,
    [LOCATION_TYPE] VARCHAR (255) NULL,
    [NAME]          VARCHAR (255) NULL,
    [PARENT_ID]     VARCHAR (255) NULL,
    [PINCODE]       VARCHAR (255) NULL,
    PRIMARY KEY CLUSTERED ([GKEY] ASC)
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [SYS_C009033]
    ON [dbo].[ORG_GEO_LOCATIONS]([GKEY] ASC);
