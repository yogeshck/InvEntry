CREATE TABLE [hr].[LEAVE_TYPES] (
    [GKEY]              INT            IDENTITY (1, 1) NOT NULL,
    [CODE]              NVARCHAR (50)  NOT NULL,
    [NAME]              NVARCHAR (50)  NOT NULL,
    [DESCRIPTION]       NVARCHAR (200) NULL,
    [IS_PAID]           BIT            DEFAULT ((1)) NULL,
    [IS_CARRY_FORWARD]  BIT            DEFAULT ((0)) NULL,
    [MAX_CARRY_FORWARD] DECIMAL (5, 2) DEFAULT ((0)) NULL,
    [IS_ENCASHABLE]     BIT            DEFAULT ((0)) NULL,
    [MAX_ENCASHMENT]    DECIMAL (5, 2) DEFAULT ((0)) NULL,
    [MIN_SERVICE_DAYS]  INT            DEFAULT ((0)) NULL,
    [MAX_PER_MONTH]     DECIMAL (5, 2) NULL,
    [MAX_PER_YEAR]      DECIMAL (5, 2) NULL,
    [GENDER]            NVARCHAR (50)  NULL,
    [REQUIRES_PROOF]    BIT            DEFAULT ((0)) NULL,
    [APPROVAL_LEVELS]   INT            DEFAULT ((1)) NULL,
    [IS_ACTIVE]         BIT            DEFAULT ((1)) NULL,
    PRIMARY KEY CLUSTERED ([GKEY] ASC),
    UNIQUE NONCLUSTERED ([CODE] ASC)
);
GO
