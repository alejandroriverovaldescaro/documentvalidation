-- Stored Procedure: Insert Face Verification Record
CREATE OR ALTER PROCEDURE usp_InsertFaceVerification
    @VerificationId UNIQUEIDENTIFIER,
    @DocumentId UNIQUEIDENTIFIER,
    @IsMatch BIT,
    @ConfidenceScore DECIMAL(5,2),
    @LivePhotoPath NVARCHAR(500) = NULL,
    @VerificationStatus NVARCHAR(50),
    @ErrorMessage NVARCHAR(MAX) = NULL,
    @CreatedBy NVARCHAR(256) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO FaceVerifications (
        VerificationId,
        DocumentId,
        VerificationTimestamp,
        IsMatch,
        ConfidenceScore,
        LivePhotoPath,
        VerificationStatus,
        ErrorMessage,
        CreatedBy
    )
    VALUES (
        @VerificationId,
        @DocumentId,
        GETUTCDATE(),
        @IsMatch,
        @ConfidenceScore,
        @LivePhotoPath,
        @VerificationStatus,
        @ErrorMessage,
        @CreatedBy
    );
END
GO

-- Stored Procedure: Get Verification History for a Document
CREATE OR ALTER PROCEDURE usp_GetVerificationHistory
    @DocumentId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        VerificationId,
        DocumentId,
        VerificationTimestamp,
        IsMatch,
        ConfidenceScore,
        LivePhotoPath,
        VerificationStatus,
        ErrorMessage,
        CreatedBy
    FROM FaceVerifications
    WHERE DocumentId = @DocumentId
    ORDER BY VerificationTimestamp DESC;
END
GO

-- Stored Procedure: Get Verification Statistics
CREATE OR ALTER PROCEDURE usp_GetVerificationStats
    @StartDate DATETIME2 = NULL,
    @EndDate DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @StartDate IS NULL
        SET @StartDate = DATEADD(DAY, -30, GETUTCDATE());
    
    IF @EndDate IS NULL
        SET @EndDate = GETUTCDATE();

    SELECT 
        COUNT(*) AS TotalVerifications,
        SUM(CASE WHEN IsMatch = 1 THEN 1 ELSE 0 END) AS SuccessfulMatches,
        SUM(CASE WHEN IsMatch = 0 THEN 1 ELSE 0 END) AS FailedMatches,
        AVG(ConfidenceScore) AS AverageConfidenceScore,
        MIN(ConfidenceScore) AS MinConfidenceScore,
        MAX(ConfidenceScore) AS MaxConfidenceScore,
        COUNT(DISTINCT DocumentId) AS UniqueDocuments
    FROM FaceVerifications
    WHERE VerificationTimestamp BETWEEN @StartDate AND @EndDate;
END
GO
