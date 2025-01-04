CREATE TABLE [dbo].[MTBL_REFERENCES] (
    [gkey]      INT           IDENTITY (1, 1) NOT NULL,
    [ref_name]  VARCHAR (50)  NOT NULL,
    [ref_code]  VARCHAR (50)  NOT NULL,
    [ref_value] VARCHAR (50)  NOT NULL,
    [sort_seq]  INT           NOT NULL,
    [ref_desc]  VARCHAR (500) NULL,
    [module]    VARCHAR (50)  NULL,
    [is_active] BIT           NOT NULL,
    CONSTRAINT [PK_mtbl_references] PRIMARY KEY CLUSTERED ([gkey] ASC),
    CONSTRAINT [FK_mtbl_references_mtbl_references] FOREIGN KEY ([gkey]) REFERENCES [dbo].[MTBL_REFERENCES] ([gkey])
);

