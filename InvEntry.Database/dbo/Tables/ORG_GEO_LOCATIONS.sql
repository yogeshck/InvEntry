﻿CREATE TABLE [dbo].[ORG_GEO_LOCATIONS] (
    [GKEY]          INT           NOT NULL,
    [EXTERNAL_ID]   VARCHAR (255) NULL,
    [LOCATION_TYPE] VARCHAR (255) NULL,
    [NAME]          VARCHAR (255) NULL,
    [PARENT_ID]     VARCHAR (255) NULL,
    [PINCODE]       VARCHAR (255) NULL,
    CONSTRAINT [PK__ORG_GEO___5F3369A044869AB7] PRIMARY KEY CLUSTERED ([GKEY] ASC)
);




GO
