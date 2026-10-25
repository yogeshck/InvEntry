CREATE TABLE [hr].[HOLIDAY_CALENDAR] (
    [GKEY]                INT            IDENTITY (1, 1) NOT NULL,
    [HOLIDAY_DATE]        DATETIME2 (7)  NOT NULL,
    [HOLIDAY_NAME]        NVARCHAR (100) NOT NULL,
    [HOLIDAY_DESCRIPTION] NVARCHAR (200) NULL,
    [HOLIDAY_TYPE]        NVARCHAR (50)  DEFAULT ('National') NULL,
    [IS_RECURRING]        BIT            DEFAULT ((0)) NULL,
    [IS_OPTIONAL]         BIT            DEFAULT ((0)) NULL,
    [LOCATION]            NVARCHAR (50)  NULL,
    [IS_ACTIVE]           BIT            DEFAULT ((1)) NULL,
    PRIMARY KEY CLUSTERED ([GKEY] ASC)
);
GO
