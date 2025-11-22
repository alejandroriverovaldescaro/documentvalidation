-- Create UploadedDocuments table (if not exists)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UploadedDocuments')
BEGIN
    CREATE TABLE UploadedDocuments (
        DocumentId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        DocumentType NVARCHAR(100) NOT NULL,
        DocumentPath NVARCHAR(500) NOT NULL,
        PhotoPath NVARCHAR(500) NULL,
        UploadTimestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UploadedBy NVARCHAR(256) NULL
    );
END
GO

-- Create FaceVerifications table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FaceVerifications')
BEGIN
    CREATE TABLE FaceVerifications (
        VerificationId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        DocumentId UNIQUEIDENTIFIER NOT NULL,
        VerificationTimestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        IsMatch BIT NOT NULL,
        ConfidenceScore DECIMAL(5,2) NOT NULL,
        LivePhotoPath NVARCHAR(500) NULL,
        VerificationStatus NVARCHAR(50) NOT NULL, -- 'Success', 'Failed', 'Error'
        ErrorMessage NVARCHAR(MAX) NULL,
        CreatedBy NVARCHAR(256) NULL,
        FOREIGN KEY (DocumentId) REFERENCES UploadedDocuments(DocumentId)
    );

    CREATE INDEX IX_FaceVerifications_DocumentId ON FaceVerifications(DocumentId);
    CREATE INDEX IX_FaceVerifications_Timestamp ON FaceVerifications(VerificationTimestamp);
END
GO
