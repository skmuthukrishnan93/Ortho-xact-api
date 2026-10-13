SET XACT_ABORT ON;
BEGIN TRY
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.PatientProcedures','LineQuantitiesJson') IS NULL
 ALTER TABLE dbo.PatientProcedures ADD LineQuantitiesJson nvarchar(max) NOT NULL CONSTRAINT DF_PatientProcedures_LineQuantitiesJson DEFAULT N'{}';
DECLARE @pk sysname;
SELECT @pk=name FROM sys.key_constraints WHERE parent_object_id=OBJECT_ID('dbo.SORDETAIL') AND type='PK';
IF @pk IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id JOIN sys.key_constraints kc ON kc.parent_object_id=ic.object_id AND kc.unique_index_id=ic.index_id WHERE kc.parent_object_id=OBJECT_ID('dbo.SORDETAIL') AND kc.type='PK' AND c.name='ProcedureNumber')
BEGIN
 DECLARE @dropPrimaryKeySql nvarchar(max);
 SET @dropPrimaryKeySql = N'ALTER TABLE dbo.SORDETAIL DROP CONSTRAINT ' + QUOTENAME(@pk) + N';';
 EXEC sys.sp_executesql @dropPrimaryKeySql;
 ALTER TABLE dbo.SORDETAIL ADD CONSTRAINT PK_SORDETAIL PRIMARY KEY(SalesOrder, ProcedureNumber, Line);
END;
IF @pk IS NULL ALTER TABLE dbo.SORDETAIL ADD CONSTRAINT PK_SORDETAIL PRIMARY KEY(SalesOrder, ProcedureNumber, Line);
COMMIT;
END TRY
BEGIN CATCH
IF @@TRANCOUNT>0 ROLLBACK;
THROW;
END CATCH;
