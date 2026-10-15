-- =============================================================
-- Reference contract for FinanceTracker.dbo.usp_Post_Financial_Movement
-- =============================================================
-- This procedure lives in the FinanceTracker database, not InvEntry's.
-- It is included here only as a reference for the DataAccess
-- FinanceDocumentPoster implementation and for FinanceTracker-side
-- deployment. Adjust the body to reuse FinanceTracker's existing
-- posting logic; keep the parameter contract stable since InvEntry
-- calls this procedure once per settlement movement.
--
-- Suspense handling: when no ledger/account mapping is found for the
-- given OrgGkey/LocationGkey/PaymentGroup combination, the procedure
-- should NOT fail the call. Instead it should insert the transaction
-- into the normal FIN_TRANSACTION table with IS_SUSPENSE = 1 (add this
-- BIT column, default 0, if it does not already exist) so the amount is
-- never lost and can be reclassified later from the FinanceTracker side.
--
-- Business validation failures (closed business date, unknown payment
-- group, invalid amount, etc.) must be raised with THROW/RAISERROR using
-- an error number in the 50000-59999 range so DataAccess can distinguish
-- a permanent business rejection from a transient SQL failure.
-- =============================================================
CREATE PROCEDURE [dbo].[usp_Post_Financial_Movement]
(
    @OrgGkey            INT,
    @LocationGkey       INT,
    @SourceSystem       VARCHAR(20),
    @SourceEventId      VARCHAR(100),
    @DocumentType       VARCHAR(30),
    @DocumentNo         VARCHAR(50),
    @DocumentDate       DATE,
    @SourceDocumentType VARCHAR(30) = NULL,
    @SourceDocumentNo   VARCHAR(50) = NULL,
    @Description        VARCHAR(500) = NULL,
    @Direction          VARCHAR(10),   -- 'IN' or 'OUT'
    @PaymentGroup       VARCHAR(20),   -- e.g. 'CASH', 'UPI', 'BANK'
    @Amount             DECIMAL(18, 2),
    @SourceReference    VARCHAR(100) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Amount <= 0
    BEGIN
        RAISERROR('A financial movement amount must be positive.', 50001, 1);
        RETURN;
    END

    -- Idempotency: a movement already recorded for the same event and
    -- source reference must not be posted twice if this procedure is
    -- retried after a prior partial failure.
    IF EXISTS (
        SELECT 1 FROM dbo.FIN_TRANSACTION
        WHERE SOURCE_EVENT_ID = @SourceEventId
          AND SOURCE_REFERENCE = @SourceReference)
        RETURN;

    DECLARE @CategoryGkey INT, @PaymentModeGkey INT, @IsSuspense BIT = 0;

    SELECT @CategoryGkey = GKEY FROM dbo.TRANSACTION_CATEGORY
    WHERE DIRECTION = @Direction;

    SELECT @PaymentModeGkey = GKEY FROM dbo.PAYMENT_MODE
    WHERE MODE_CODE = @PaymentGroup;

    IF @CategoryGkey IS NULL OR @PaymentModeGkey IS NULL
        SET @IsSuspense = 1;

    INSERT INTO dbo.FIN_TRANSACTION
    (
        ORG_GKEY, LOCATION_GKEY, TRANSACTION_DATE, CATEGORY_GKEY,
        PAYMENT_MODE_GKEY, AMOUNT, DOCUMENT_NO, DESCRIPTION, STATUS,
        IS_MANUAL_ENTRY, IS_SUSPENSE, SOURCE_SYSTEM, SOURCE_EVENT_ID,
        SOURCE_REFERENCE
    )
    VALUES
    (
        @OrgGkey, @LocationGkey, @DocumentDate, @CategoryGkey,
        @PaymentModeGkey, @Amount, @DocumentNo, @Description, 'POSTED',
        0, @IsSuspense, @SourceSystem, @SourceEventId, @SourceReference
    );

    -- Maintain FIN_DOCUMENT so FinanceDocumentStatusReader /
    -- FinanceClosingFinancialEvidenceReader see the document as posted.
    IF NOT EXISTS (
        SELECT 1 FROM dbo.FIN_DOCUMENT
        WHERE ORG_GKEY = @OrgGkey AND LOCATION_GKEY = @LocationGkey
          AND SOURCE_EVENT_ID = @SourceEventId)
        INSERT INTO dbo.FIN_DOCUMENT
        (
            ORG_GKEY, LOCATION_GKEY, SOURCE_SYSTEM, SOURCE_EVENT_ID,
            DOCUMENT_TYPE, DOCUMENT_NO, DOCUMENT_DATE, STATUS
        )
        VALUES
        (
            @OrgGkey, @LocationGkey, @SourceSystem, @SourceEventId,
            @DocumentType, @DocumentNo, @DocumentDate, 'POSTED'
        );
END