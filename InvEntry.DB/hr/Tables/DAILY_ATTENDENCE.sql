CREATE TABLE [hr].[DAILY_ATTENDENCE] (
    [GKEY]               INT             IDENTITY (1, 1) NOT NULL,
    [EMPLOYEE_GKEY]      INT             NOT NULL,
    [WORK_SITE]          NVARCHAR (50)   NULL,
    [WORK_DATE]          DATE            NOT NULL,
    [WORK_STATUS]        NVARCHAR (1)    DEFAULT ('P') NOT NULL,
    [WORK_HOURS_DAY]     DECIMAL (6, 2)  NULL,
    [FIRST_IN]           DATETIME2 (7)   NULL,
    [LAST_OUT]           DATETIME2 (7)   NULL,
    [PUNCH_COUNT]        INT             DEFAULT ((0)) NOT NULL,
    [LEAVE_REQUEST_GKEY] INT             NULL,
    [NOTES]              NVARCHAR (1000) NULL,
    [TENANT_GKEY]        INT             NULL,
    [CREATED_BY]         NVARCHAR (100)  NULL,
    [CREATED_ON]         DATETIME2 (7)   DEFAULT (sysutcdatetime()) NOT NULL,
    [MODIFIED_BY]        NVARCHAR (100)  NULL,
    [MODIFIED_ON]        DATETIME2 (7)   NULL,
    PRIMARY KEY CLUSTERED ([GKEY] ASC),
    UNIQUE NONCLUSTERED ([EMPLOYEE_GKEY] ASC, [WORK_DATE] ASC)
);
GO
