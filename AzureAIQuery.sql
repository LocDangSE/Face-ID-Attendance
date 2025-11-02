USE [AttendanceSystemDB]
GO
/****** Object:  Table [dbo].[AttendanceRecords]    Script Date: 11/2/2025 12:53:38 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AttendanceRecords](
	[AttendanceID] [uniqueidentifier] NOT NULL,
	[SessionID] [uniqueidentifier] NOT NULL,
	[StudentID] [uniqueidentifier] NOT NULL,
	[CheckInTime] [datetime2](7) NOT NULL,
	[ConfidenceScore] [decimal](5, 4) NULL,
	[Status] [nvarchar](20) NOT NULL,
	[IsManualOverride] [bit] NOT NULL,
	[Notes] [nvarchar](500) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[AttendanceID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Session_Student] UNIQUE NONCLUSTERED 
(
	[SessionID] ASC,
	[StudentID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AttendanceSessions]    Script Date: 11/2/2025 12:53:38 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AttendanceSessions](
	[SessionID] [uniqueidentifier] NOT NULL,
	[ClassID] [uniqueidentifier] NOT NULL,
	[SessionDate] [date] NOT NULL,
	[SessionStartTime] [datetime2](7) NOT NULL,
	[SessionEndTime] [datetime2](7) NULL,
	[Status] [nvarchar](20) NOT NULL,
	[Location] [nvarchar](200) NULL,
	[Notes] [nvarchar](1000) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[SessionID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ClassEnrollments]    Script Date: 11/2/2025 12:53:38 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ClassEnrollments](
	[EnrollmentID] [uniqueidentifier] NOT NULL,
	[ClassID] [uniqueidentifier] NOT NULL,
	[StudentID] [uniqueidentifier] NOT NULL,
	[EnrolledAt] [datetime2](7) NOT NULL,
	[Status] [nvarchar](20) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[EnrollmentID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Class_Student] UNIQUE NONCLUSTERED 
(
	[ClassID] ASC,
	[StudentID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Classes]    Script Date: 11/2/2025 12:53:38 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Classes](
	[ClassID] [uniqueidentifier] NOT NULL,
	[ClassName] [nvarchar](200) NOT NULL,
	[ClassCode] [nvarchar](50) NOT NULL,
	[Description] [nvarchar](1000) NULL,
	[AzurePersonGroupId] [nvarchar](255) NULL,
	[Location] [nvarchar](200) NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[ClassID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[ClassCode] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Students]    Script Date: 11/2/2025 12:53:38 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Students](
	[StudentID] [uniqueidentifier] NOT NULL,
	[StudentNumber] [nvarchar](50) NOT NULL,
	[FirstName] [nvarchar](100) NOT NULL,
	[LastName] [nvarchar](100) NOT NULL,
	[Email] [nvarchar](255) NOT NULL,
	[AzurePersonId] [nvarchar](255) NULL,
	[ProfilePhotoUrl] [nvarchar](500) NULL,
	[AzureFaceId] [nvarchar](255) NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[StudentID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[StudentNumber] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[AttendanceRecords] ADD  DEFAULT (newid()) FOR [AttendanceID]
GO
ALTER TABLE [dbo].[AttendanceRecords] ADD  DEFAULT (getdate()) FOR [CheckInTime]
GO
ALTER TABLE [dbo].[AttendanceRecords] ADD  DEFAULT ('Present') FOR [Status]
GO
ALTER TABLE [dbo].[AttendanceRecords] ADD  DEFAULT ((0)) FOR [IsManualOverride]
GO
ALTER TABLE [dbo].[AttendanceRecords] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[AttendanceSessions] ADD  DEFAULT (newid()) FOR [SessionID]
GO
ALTER TABLE [dbo].[AttendanceSessions] ADD  DEFAULT ('InProgress') FOR [Status]
GO
ALTER TABLE [dbo].[AttendanceSessions] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[ClassEnrollments] ADD  DEFAULT (newid()) FOR [EnrollmentID]
GO
ALTER TABLE [dbo].[ClassEnrollments] ADD  DEFAULT (getdate()) FOR [EnrolledAt]
GO
ALTER TABLE [dbo].[ClassEnrollments] ADD  DEFAULT ('Active') FOR [Status]
GO
ALTER TABLE [dbo].[Classes] ADD  DEFAULT (newid()) FOR [ClassID]
GO
ALTER TABLE [dbo].[Classes] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[Classes] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Students] ADD  DEFAULT (newid()) FOR [StudentID]
GO
ALTER TABLE [dbo].[Students] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[Students] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[AttendanceRecords]  WITH CHECK ADD  CONSTRAINT [FK_Attendance_Session] FOREIGN KEY([SessionID])
REFERENCES [dbo].[AttendanceSessions] ([SessionID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AttendanceRecords] CHECK CONSTRAINT [FK_Attendance_Session]
GO
ALTER TABLE [dbo].[AttendanceRecords]  WITH CHECK ADD  CONSTRAINT [FK_Attendance_Student] FOREIGN KEY([StudentID])
REFERENCES [dbo].[Students] ([StudentID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AttendanceRecords] CHECK CONSTRAINT [FK_Attendance_Student]
GO
ALTER TABLE [dbo].[AttendanceSessions]  WITH CHECK ADD  CONSTRAINT [FK_Session_Class] FOREIGN KEY([ClassID])
REFERENCES [dbo].[Classes] ([ClassID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AttendanceSessions] CHECK CONSTRAINT [FK_Session_Class]
GO
ALTER TABLE [dbo].[ClassEnrollments]  WITH CHECK ADD  CONSTRAINT [FK_Enrollment_Class] FOREIGN KEY([ClassID])
REFERENCES [dbo].[Classes] ([ClassID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ClassEnrollments] CHECK CONSTRAINT [FK_Enrollment_Class]
GO
ALTER TABLE [dbo].[ClassEnrollments]  WITH CHECK ADD  CONSTRAINT [FK_Enrollment_Student] FOREIGN KEY([StudentID])
REFERENCES [dbo].[Students] ([StudentID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ClassEnrollments] CHECK CONSTRAINT [FK_Enrollment_Student]
GO
ALTER TABLE [dbo].[AttendanceRecords]  WITH CHECK ADD  CONSTRAINT [CHK_Attendance_Confidence] CHECK  (([ConfidenceScore] IS NULL OR [ConfidenceScore]>=(0) AND [ConfidenceScore]<=(1)))
GO
ALTER TABLE [dbo].[AttendanceRecords] CHECK CONSTRAINT [CHK_Attendance_Confidence]
GO
ALTER TABLE [dbo].[AttendanceRecords]  WITH CHECK ADD  CONSTRAINT [CHK_Attendance_Status] CHECK  (([Status]='Excused' OR [Status]='Late' OR [Status]='Absent' OR [Status]='Present'))
GO
ALTER TABLE [dbo].[AttendanceRecords] CHECK CONSTRAINT [CHK_Attendance_Status]
GO
ALTER TABLE [dbo].[AttendanceSessions]  WITH CHECK ADD  CONSTRAINT [CHK_Session_EndTime] CHECK  (([SessionEndTime] IS NULL OR [SessionEndTime]>=[SessionStartTime]))
GO
ALTER TABLE [dbo].[AttendanceSessions] CHECK CONSTRAINT [CHK_Session_EndTime]
GO
ALTER TABLE [dbo].[AttendanceSessions]  WITH CHECK ADD  CONSTRAINT [CHK_Session_Status] CHECK  (([Status]='Cancelled' OR [Status]='Completed' OR [Status]='InProgress'))
GO
ALTER TABLE [dbo].[AttendanceSessions] CHECK CONSTRAINT [CHK_Session_Status]
GO
ALTER TABLE [dbo].[ClassEnrollments]  WITH CHECK ADD  CONSTRAINT [CHK_Enrollment_Status] CHECK  (([Status]='Completed' OR [Status]='Dropped' OR [Status]='Active'))
GO
ALTER TABLE [dbo].[ClassEnrollments] CHECK CONSTRAINT [CHK_Enrollment_Status]
GO
ALTER TABLE [dbo].[Students]  WITH CHECK ADD  CONSTRAINT [CHK_Student_Email] CHECK  (([Email] like '%@%.%'))
GO
ALTER TABLE [dbo].[Students] CHECK CONSTRAINT [CHK_Student_Email]
GO
