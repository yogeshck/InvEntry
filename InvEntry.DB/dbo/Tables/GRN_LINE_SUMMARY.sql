CREATE TABLE [dbo].[GRN_LINE_SUMMARY] (
    [gkey]             INT             IDENTITY (1, 1) NOT NULL,
    [grn_hdr_gkey]     INT             NULL,
    [line_nbr]         INT             NULL,
    [product_gkey]     INT             NULL,
    [gross_weight]     DECIMAL (10, 3) NULL,
    [stone_weight]     DECIMAL (10, 3) NULL,
    [net_weight]       DECIMAL (10, 3) NULL,
    [supplied_qty]     INT             NULL,
    [product_category] VARCHAR (50)    NULL,
    [product_purity]   VARCHAR (50)    NULL,
    [uom]              VARCHAR (50)    NULL,
    CONSTRAINT [PK_grn_line_summary] PRIMARY KEY CLUSTERED ([gkey] ASC)
);

