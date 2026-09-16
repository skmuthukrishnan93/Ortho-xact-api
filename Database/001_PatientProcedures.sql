-- Run against the OrthoX application database before deploying the updated API.
IF OBJECT_ID(N'dbo.PatientProcedures', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PatientProcedures (
        SalesOrder nvarchar(50) NOT NULL,
        ProcedureNumber int NOT NULL CHECK (ProcedureNumber > 0),
        PatientNumber nvarchar(200) NOT NULL,
        PatientName nvarchar(200) NOT NULL,
        SurgeonName nvarchar(200) NOT NULL,
        ProcedureDate date NOT NULL,
        CgSetNumber nvarchar(200) NOT NULL,
        LineNumbersJson nvarchar(max) NOT NULL,
        CONSTRAINT PK_PatientProcedures PRIMARY KEY (SalesOrder, ProcedureNumber),
        CONSTRAINT CK_PatientProcedures_Lines CHECK (ISJSON(LineNumbersJson) = 1)
    );
END;
