CREATE TABLE [dbo].[mtbl_references] (
    [gkey]      INT            IDENTITY (1, 1) NOT NULL,
    [ref_name]  NVARCHAR (50)  NOT NULL,
    [ref_code]  NVARCHAR (50)  NOT NULL,
    [ref_value] NVARCHAR (50)  NOT NULL,
    [sort_seq]  SMALLINT       NOT NULL,
    [ref_desc]  NVARCHAR (MAX) NULL,
    [module]    NVARCHAR (50)  NULL,
    [is_active] BIT            NOT NULL,
    CONSTRAINT [PK_mtbl_references] PRIMARY KEY CLUSTERED ([gkey] ASC),
    CONSTRAINT [FK_mtbl_references_mtbl_references] FOREIGN KEY ([gkey]) REFERENCES [dbo].[mtbl_references] ([gkey])
);

