-- Run only in the OrthoX APPLICATION database. This does not modify SYSPRO.
-- Specific recovery: order 57189, login failed, no business-object steps.
-- Fix the SYSPRO login configuration before retrying from the application.
-- Preserves the complete attempt and staged-line snapshot in an audit archive.
-- Existing UnsuccessfulSOMaster/UnsuccessfulSODetail records are retained.
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @AttemptId uniqueidentifier = '30617634-E722-4CB0-A24F-88A821624686';
DECLARE @SalesOrder nvarchar(50) = N'000000000057189';
DECLARE @LineCount int;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.ProcedurePostAttempts WITH (UPDLOCK, HOLDLOCK)
        WHERE Id = @AttemptId AND SalesOrder = @SalesOrder AND Status = N'RequiresReview'
    )
        THROW 51000, 'Expected failed attempt not found, already reset, or status changed. Nothing reset.', 1;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.UnsuccessfulSOMaster WITH (UPDLOCK, HOLDLOCK)
        WHERE Id = @AttemptId AND SalesOrder = @SalesOrder
          AND Error = N'SYSPRO login did not return a valid session.'
    )
        THROW 51001, 'This reset is only valid for the confirmed login failure.', 1;

    IF EXISTS (SELECT 1 FROM dbo.ProcedurePostSteps WITH (UPDLOCK, HOLDLOCK) WHERE AttemptId = @AttemptId)
       OR EXISTS (SELECT 1 FROM dbo.UnsuccessfulSODetail WITH (UPDLOCK, HOLDLOCK) WHERE MasterId = @AttemptId)
        THROW 51002, 'Posting steps exist. Reconcile SYSPRO before attempting recovery.', 1;

    SELECT @LineCount = COUNT(*)
    FROM dbo.SORDETAIL WITH (UPDLOCK, HOLDLOCK) WHERE SalesOrder = @SalesOrder;

    IF @LineCount = 0 OR EXISTS (
        SELECT 1 FROM dbo.SORDETAIL WHERE SalesOrder = @SalesOrder AND Status <> N'SysproPostingFailed'
    )
        THROW 51003, 'Expected failed staging lines not found, or their status changed.', 1;

    IF EXISTS (
        SELECT 1 FROM dbo.SORDETAIL
        WHERE SalesOrder = @SalesOrder AND KitFlag <> N'P'
          AND (CheckedAt IS NULL OR ReturnQty IS NULL OR ReturnQty < 0 OR ReturnQty > DeliveredQty
               OR UsageQty IS NULL OR Variance IS NULL OR Variance <> 0
               OR UsageQty <> DeliveredQty - ReturnQty OR RepUsageQty <> UsageQty)
    )
        THROW 51004, 'Saved warehouse checks or zero-variance validation are incomplete. Do not force ReadyToPostSyspro.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.SORHEADER WITH (HOLDLOCK) WHERE SalesOrder = @SalesOrder)
       OR EXISTS (
        SELECT 1 FROM dbo.SORHEADER AS h WITH (HOLDLOCK)
        WHERE h.SalesOrder = @SalesOrder AND NOT EXISTS (
            SELECT 1 FROM dbo.SORDETAIL AS d
            WHERE d.SalesOrder = h.SalesOrder AND d.ProcedureNumber = h.ProcedureNumber AND d.KitFlag = N'P'
        )
    )
        THROW 51005, 'Each saved procedure must have at least one kit parent.', 1;

    IF OBJECT_ID(N'dbo.ProcedurePostAttemptRecoveryArchive', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ProcedurePostAttemptRecoveryArchive (
            AttemptId uniqueidentifier NOT NULL PRIMARY KEY,
            SalesOrder nvarchar(50) NOT NULL,
            ArchivedAt datetime2 NOT NULL,
            ArchivedBy nvarchar(256) NOT NULL,
            RecoveryReason nvarchar(400) NOT NULL,
            AttemptJson nvarchar(max) NOT NULL,
            StagingJson nvarchar(max) NOT NULL
        );
    END;

    INSERT dbo.ProcedurePostAttemptRecoveryArchive
        (AttemptId, SalesOrder, ArchivedAt, ArchivedBy, RecoveryReason, AttemptJson, StagingJson)
    SELECT @AttemptId, @SalesOrder, SYSUTCDATETIME(), COALESCE(ORIGINAL_LOGIN(), N'Unknown'),
        N'Confirmed login failure with no business-object steps. Archived before unlocking a new attempt.',
        (SELECT a.* FROM dbo.ProcedurePostAttempts AS a WHERE a.Id = @AttemptId FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
        (SELECT d.* FROM dbo.SORDETAIL AS d WHERE d.SalesOrder = @SalesOrder ORDER BY d.Line FOR JSON PATH);

    UPDATE dbo.SORDETAIL
    SET Status = N'ReadyToPostSyspro'
    WHERE SalesOrder = @SalesOrder AND Status = N'SysproPostingFailed';

    IF @@ROWCOUNT <> @LineCount
        THROW 51006, 'Unexpected staging update count. Recovery cancelled.', 1;

    -- Remove ONLY the archived, never-posted attempt to release the unique order lock.
    DELETE FROM dbo.ProcedurePostAttempts
    WHERE Id = @AttemptId AND SalesOrder = @SalesOrder AND Status = N'RequiresReview';

    IF @@ROWCOUNT <> 1
        THROW 51007, 'Unexpected attempt delete count. Recovery cancelled.', 1;

    COMMIT TRANSACTION;

    SELECT N'Ready for a new attempt after correcting SYSPRO login.' AS Result,
           @SalesOrder AS SalesOrder, @AttemptId AS ArchivedAttemptId, @LineCount AS ResetLines;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
