CREATE TABLE [dbo].[PRODUCT_TRANSACTION] (
    [GKEY]              FLOAT (53)      NULL,
    [REF_GKEY]          FLOAT (53)      NULL,
    [TRANS_DATE]        DATETIME        NULL,
    [DOC_REF_NBR]       VARCHAR (20)    NULL,
    [DOC_TYPE]          VARCHAR (20)    NULL,
    [PRODUCT_REF_GKEY]  FLOAT (53)      NULL,
    [COLUMN1]           DECIMAL (10, 3) NULL,
    [TRANS_QTY]         DECIMAL (10, 3) NULL,
    [CB_QTY]            DECIMAL (10, 3) NULL,
    [UNIT_TRANS_PRICE]  DECIMAL (19, 2) NULL,
    [TRANSACTION_VALUE] DECIMAL (19, 2) NULL,
    [TRANS_NOTE]        VARCHAR (250)   NULL
);

