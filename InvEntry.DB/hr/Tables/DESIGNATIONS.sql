CREATE TABLE [hr].[DESIGNATIONS] (
    [GKEY]            INT            IDENTITY (1, 1) NOT NULL,
    [CODE]            NVARCHAR (20)  NULL,
    [NAME]            NVARCHAR (100) NULL,
    [WORK_LEVEL]      INT            NULL,
    [DEPARTMENT_GKEY] INT            NULL,
    [IS_ACTIVE]       BIT            NOT NULL
);

