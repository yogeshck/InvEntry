CREATE TABLE [dbo].[VOUCHER_TYPES] (
    [GKEY]                   INT          NOT NULL,
    [DOCUMENT_TYPE]          VARCHAR (50) NULL,
    [ABBREVIATION]           VARCHAR (50) NULL,
    [MTBL_VOUCHER_TYPE_GKEY] INT          NULL,
    [LAST_USED_NUMBER]       INT          NULL,
    [IS_TAXABLE]             BIT          NULL,
    [NARRATION]              VARCHAR (50) NULL,
    [DOC_NBR_METHOD]         VARCHAR (50) NULL,
    [DOC_NBR_LENGTH]         INT          NULL,
    [DOC_NBR_PREFILL]        VARCHAR (50) NULL,
    [DOC_NBR_PREFIX]         VARCHAR (50) NULL,
    [DOC_NBR_SUFFIX]         VARCHAR (50) NULL,
    [IS_ACTIVE]              BIT          NULL,
    [USED_FOR]               VARCHAR (50) NULL,
    CONSTRAINT [PK_VOUCHER_TYPES] PRIMARY KEY CLUSTERED ([GKEY] ASC)
);

