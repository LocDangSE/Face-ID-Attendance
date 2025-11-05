using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FaceIdBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionSnapshotTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Classes",
                columns: table => new
                {
                    ClassID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    ClassName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ClassCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AzurePersonGroupId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Classes__CB1927A0A783E26D", x => x.ClassID);
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    StudentID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    StudentNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AzurePersonId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ProfilePhotoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AzureFaceId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Students__32C52A79FE3CA4F8", x => x.StudentID);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceSessions",
                columns: table => new
                {
                    SessionID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    ClassID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SessionStartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SessionEndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "InProgress"),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Attendan__C9F49270DC14CDA5", x => x.SessionID);
                    table.ForeignKey(
                        name: "FK_Session_Class",
                        column: x => x.ClassID,
                        principalTable: "Classes",
                        principalColumn: "ClassID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClassEnrollments",
                columns: table => new
                {
                    EnrollmentID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    ClassID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnrolledAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())"),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ClassEnr__7F6877FB0FB61386", x => x.EnrollmentID);
                    table.ForeignKey(
                        name: "FK_Enrollment_Class",
                        column: x => x.ClassID,
                        principalTable: "Classes",
                        principalColumn: "ClassID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Enrollment_Student",
                        column: x => x.StudentID,
                        principalTable: "Students",
                        principalColumn: "StudentID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceRecords",
                columns: table => new
                {
                    AttendanceID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    SessionID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CheckInTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())"),
                    ConfidenceScore = table.Column<decimal>(type: "decimal(5,4)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Present"),
                    IsManualOverride = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Attendan__8B69263CAA59BE09", x => x.AttendanceID);
                    table.ForeignKey(
                        name: "FK_Attendance_Session",
                        column: x => x.SessionID,
                        principalTable: "AttendanceSessions",
                        principalColumn: "SessionID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Attendance_Student",
                        column: x => x.StudentID,
                        principalTable: "Students",
                        principalColumn: "StudentID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionSnapshots",
                columns: table => new
                {
                    SnapshotID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    SessionID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalStudents = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    PresentCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    AbsentCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LateCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    AttendanceRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    CapturedImagesFolder = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RecognitionResultsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttendanceRecordsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SessionMetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    GeneratedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SessionStartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SessionEndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SessionDuration = table.Column<TimeSpan>(type: "time", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__SessionS__SnapshotID", x => x.SnapshotID);
                    table.ForeignKey(
                        name: "FK_SessionSnapshot_Session",
                        column: x => x.SessionID,
                        principalTable: "AttendanceSessions",
                        principalColumn: "SessionID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_CheckInTime",
                table: "AttendanceRecords",
                column: "CheckInTime");

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_SessionID",
                table: "AttendanceRecords",
                column: "SessionID");

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_Status",
                table: "AttendanceRecords",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_StudentID",
                table: "AttendanceRecords",
                column: "StudentID");

            migrationBuilder.CreateIndex(
                name: "UQ_Session_Student",
                table: "AttendanceRecords",
                columns: new[] { "SessionID", "StudentID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_Class_Date",
                table: "AttendanceSessions",
                columns: new[] { "ClassID", "SessionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_ClassID",
                table: "AttendanceSessions",
                column: "ClassID");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_Date",
                table: "AttendanceSessions",
                column: "SessionDate");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_Status",
                table: "AttendanceSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_ClassID",
                table: "ClassEnrollments",
                column: "ClassID");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_Status",
                table: "ClassEnrollments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_StudentID",
                table: "ClassEnrollments",
                column: "StudentID");

            migrationBuilder.CreateIndex(
                name: "UQ_Class_Student",
                table: "ClassEnrollments",
                columns: new[] { "ClassID", "StudentID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Classes_ClassCode",
                table: "Classes",
                column: "ClassCode");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_IsActive",
                table: "Classes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "UQ__Classes__2ECD4A55E3D4F333",
                table: "Classes",
                column: "ClassCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionSnapshots_GeneratedAt",
                table: "SessionSnapshots",
                column: "GeneratedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SessionSnapshots_SessionID",
                table: "SessionSnapshots",
                column: "SessionID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_AzurePersonId",
                table: "Students",
                column: "AzurePersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_IsActive",
                table: "Students",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Students_StudentNumber",
                table: "Students",
                column: "StudentNumber");

            migrationBuilder.CreateIndex(
                name: "UQ__Students__DD81BF6C90048E4A",
                table: "Students",
                column: "StudentNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceRecords");

            migrationBuilder.DropTable(
                name: "ClassEnrollments");

            migrationBuilder.DropTable(
                name: "SessionSnapshots");

            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.DropTable(
                name: "AttendanceSessions");

            migrationBuilder.DropTable(
                name: "Classes");
        }
    }
}
