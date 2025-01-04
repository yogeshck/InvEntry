CREATE TABLE [dbo].[ORG_PLACES] (
    [gkey]                  INT            IDENTITY (1, 1) NOT NULL,
    [pincode]               INT            NOT NULL,
    [locality_village_name] NVARCHAR (100) NULL,
    [post_office_name]      NVARCHAR (100) NULL,
    [sub_district_name]     NVARCHAR (100) NULL,
    [district_name]         NVARCHAR (100) NULL,
    [state_name]            NVARCHAR (100) NULL,
    CONSTRAINT [PK_ORG_PLACES] PRIMARY KEY CLUSTERED ([gkey] ASC)
);

