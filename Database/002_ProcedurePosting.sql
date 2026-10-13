-- Apply to the OrthoX APPLICATION database before deploying the API.
-- Back up and review existing SORHEADER/SORDETAIL definitions with LiquidERP first.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.SORHEADER') IS NOT NULL AND COL_LENGTH('dbo.SORHEADER', 'ProcedureNumber') IS NULL
    THROW 51000, 'SORHEADER already exists with a different schema. Reconcile the mapping before deployment.', 1;
IF OBJECT_ID(N'dbo.SORDETAIL') IS NOT NULL AND COL_LENGTH('dbo.SORDETAIL', 'CheckedAt') IS NULL
    THROW 51000, 'SORDETAIL already exists with a different schema. Reconcile the mapping before deployment.', 1;
IF OBJECT_ID(N'dbo.SORHEADER', N'U') IS NULL
CREATE TABLE dbo.SORHEADER (
    SalesOrder nvarchar(50) NOT NULL, ProcedureNumber int NOT NULL,
    PatientNumber nvarchar(max) NOT NULL, PatientName nvarchar(max) NOT NULL,
    SurgeonName nvarchar(max) NOT NULL, ProcedureDate date NOT NULL,
    CgSetNumber nvarchar(max) NOT NULL, Customer nvarchar(max) NOT NULL, SourceHeaderJson nvarchar(max) NOT NULL,
    CONSTRAINT PK_SORHEADER PRIMARY KEY (SalesOrder, ProcedureNumber),
    CONSTRAINT CK_SORHEADER_Procedure CHECK (ProcedureNumber > 0)
);
IF OBJECT_ID(N'dbo.SORDETAIL', N'U') IS NULL
CREATE TABLE dbo.SORDETAIL (
    SalesOrder nvarchar(50) NOT NULL, Line int NOT NULL, ProcedureNumber int NOT NULL,
    ParentLine int NULL, KitFlag nvarchar(max) NOT NULL, ComponentSequence nvarchar(max) NOT NULL,
    SourceLineJson nvarchar(max) NOT NULL, SetCode nvarchar(max) NOT NULL, StockCode nvarchar(max) NOT NULL, Warehouse nvarchar(max) NOT NULL, Bin nvarchar(max) NOT NULL,
    Description nvarchar(max) NOT NULL, DeliveredQty decimal(18,6) NOT NULL,
    OrderQty decimal(18,6) NOT NULL, RepUsageQty decimal(18,6) NOT NULL,
    ReturnQty decimal(18,6) NULL, UsageQty decimal(18,6) NULL, Variance decimal(18,6) NULL,
    CheckedAt datetime2 NULL, Status nvarchar(max) NOT NULL,
    CONSTRAINT PK_SORDETAIL PRIMARY KEY (SalesOrder, Line),
    CONSTRAINT FK_SORDETAIL_HEADER FOREIGN KEY (SalesOrder, ProcedureNumber) REFERENCES dbo.SORHEADER(SalesOrder, ProcedureNumber),
    CONSTRAINT CK_SORDETAIL_Return CHECK (ReturnQty IS NULL OR (ReturnQty >= 0 AND ReturnQty <= DeliveredQty))
);
IF OBJECT_ID(N'dbo.ProcedurePostAttempts', N'U') IS NULL
CREATE TABLE dbo.ProcedurePostAttempts (
    Id uniqueidentifier NOT NULL PRIMARY KEY, SalesOrder nvarchar(50) NOT NULL,
    Status nvarchar(max) NOT NULL, CreatedAt datetime2 NOT NULL, CreatedBy nvarchar(max) NOT NULL,
    SnapshotJson nvarchar(max) NOT NULL,
    CONSTRAINT UX_ProcedurePostAttempts_SalesOrder UNIQUE(SalesOrder)
);
IF OBJECT_ID(N'dbo.ProcedurePostSteps', N'U') IS NULL
CREATE TABLE dbo.ProcedurePostSteps (
    Id bigint IDENTITY PRIMARY KEY, AttemptId uniqueidentifier NOT NULL,
    ProcedureNumber int NULL, BusinessObject nvarchar(max) NOT NULL,
    Status nvarchar(max) NOT NULL, RequestXml nvarchar(max) NOT NULL,
    ParametersXml nvarchar(max) NOT NULL, ResponseXml nvarchar(max) NOT NULL, CreatedAt datetime2 NOT NULL,
    CONSTRAINT FK_ProcedurePostSteps_Attempt FOREIGN KEY(AttemptId) REFERENCES dbo.ProcedurePostAttempts(Id)
);
IF OBJECT_ID(N'dbo.UnsuccessfulSOMaster', N'U') IS NULL
CREATE TABLE dbo.UnsuccessfulSOMaster (
    Id uniqueidentifier NOT NULL PRIMARY KEY, SalesOrder nvarchar(max) NOT NULL,
    Error nvarchar(max) NOT NULL, CreatedAt datetime2 NOT NULL,
    NotificationSentAt datetime2 NULL, NotificationError nvarchar(max) NULL
);
IF OBJECT_ID(N'dbo.UnsuccessfulSODetail', N'U') IS NULL
CREATE TABLE dbo.UnsuccessfulSODetail (
    Id bigint IDENTITY PRIMARY KEY, MasterId uniqueidentifier NOT NULL,
    ProcedureNumber int NULL, BusinessObject nvarchar(max) NOT NULL,
    RequestXml nvarchar(max) NOT NULL, ResponseXml nvarchar(max) NOT NULL,
    CONSTRAINT FK_UnsuccessfulSODetail_Master FOREIGN KEY(MasterId) REFERENCES dbo.UnsuccessfulSOMaster(Id)
);
COMMIT;
