-- Run in the app database after 004_ProcedureReplacementReporting.sql.
SELECT p.SalesOrder AS OriginalSalesOrder, p.ProcedureNumber,
       p.ReplacementSalesOrder, p.PostingStatus AS ProcedurePostingStatus,
       p.PostedAt AS ProcedurePostedAtUtc, p.PostingAttemptId,
       a.Status AS OverallPostingStatus, a.CreatedBy AS PostedBy
FROM dbo.PatientProcedures p
LEFT JOIN dbo.ProcedurePostAttempts a ON a.Id = p.PostingAttemptId
ORDER BY p.SalesOrder, p.ProcedureNumber;
