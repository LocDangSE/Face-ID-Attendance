-- Create SessionSnapshots table for storing session history/receipts
-- Run this script manually in SQL Server Management Studio or via dotnet ef

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SessionSnapshots]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SessionSnapshots] (
        [SnapshotID] uniqueidentifier NOT NULL DEFAULT (newid()),
        [SessionID] uniqueidentifier NOT NULL,
        [TotalStudents] int NOT NULL DEFAULT 0,
        [PresentCount] int NOT NULL DEFAULT 0,
        [AbsentCount] int NOT NULL DEFAULT 0,
        [LateCount] int NOT NULL DEFAULT 0,
        [AttendanceRate] decimal(5, 2) NOT NULL,
        [CapturedImagesFolder] nvarchar(500) NULL,
        [RecognitionResultsJson] nvarchar(max) NULL,
        [AttendanceRecordsJson] nvarchar(max) NULL,
        [SessionMetadataJson] nvarchar(max) NULL,
        [GeneratedAt] datetime2 NOT NULL DEFAULT (getutcdate()),
        [GeneratedBy] nvarchar(100) NULL,
        [SessionStartTime] datetime2 NULL,
        [SessionEndTime] datetime2 NULL,
        [SessionDuration] time(7) NULL,
        CONSTRAINT [PK__SessionS__SnapshotID] PRIMARY KEY ([SnapshotID]),
        CONSTRAINT [FK_SessionSnapshot_Session] FOREIGN KEY ([SessionID]) 
            REFERENCES [dbo].[AttendanceSessions]([SessionID]) 
            ON DELETE CASCADE
    );

    -- Create indexes for performance
    CREATE UNIQUE INDEX [IX_SessionSnapshots_SessionID] ON [dbo].[SessionSnapshots]([SessionID]);
    CREATE INDEX [IX_SessionSnapshots_GeneratedAt] ON [dbo].[SessionSnapshots]([GeneratedAt] DESC);

    PRINT 'SessionSnapshots table created successfully!';
END
ELSE
BEGIN
    PRINT 'SessionSnapshots table already exists.';
END
GO
