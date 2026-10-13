-- Run against the OrthoX APP database before deploying the updated backend.
-- Nullable fields deliberately leave historical records unknown, not falsely unposted.
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 IF OBJECT_ID(N'dbo.PatientProcedures', N'U') IS NULL
  THROW 50001, 'PatientProcedures is missing. Run the earlier procedure migrations first.', 1;
 IF COL_LENGTH('dbo.PatientProcedures', 'ReplacementSalesOrder') IS NULL
  ALTER TABLE dbo.PatientProcedures ADD ReplacementSalesOrder nvarchar(50) NULL;
 IF COL_LENGTH('dbo.PatientProcedures', 'PostingStatus') IS NULL
  ALTER TABLE dbo.PatientProcedures ADD PostingStatus nvarchar(30) NULL;
 IF COL_LENGTH('dbo.PatientProcedures', 'PostedAt') IS NULL
  ALTER TABLE dbo.PatientProcedures ADD PostedAt datetime2 NULL;
 IF COL_LENGTH('dbo.PatientProcedures', 'PostingAttemptId') IS NULL
  ALTER TABLE dbo.PatientProcedures ADD PostingAttemptId uniqueidentifier NULL;
 COMMIT;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT > 0 ROLLBACK;
 THROW;
END CATCH;
