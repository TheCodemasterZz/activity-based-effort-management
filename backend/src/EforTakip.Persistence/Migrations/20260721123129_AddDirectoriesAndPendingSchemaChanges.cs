using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EforTakip.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectoriesAndPendingSchemaChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Projects",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovalId",
                table: "EmployeeWorkLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Directories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Source = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DirectoryType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Hostname = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Port = table.Column<int>(type: "integer", nullable: false),
                    UseSsl = table.Column<bool>(type: "boolean", nullable: false),
                    BindUsername = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    BindPasswordEncrypted = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    BaseDn = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    AdditionalUserDn = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    AdditionalGroupDn = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Permission = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    UserObjectClass = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UserObjectFilter = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    UsernameAttribute = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UsernameRdnAttribute = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FirstNameAttribute = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastNameAttribute = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DisplayNameAttribute = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EmailAttribute = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UniqueIdAttribute = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SyncSchedule = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Directories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DirectoryAttributeMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdAttributeName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    SystemFieldName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    FieldType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsSynced = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DirectoryAttributeMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeLeaves",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsFullDay = table.Column<bool>(type: "boolean", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeLeaves", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeLeaves_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkLogApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ApprovedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkLogApprovals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DirectoryUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DirectoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Username = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    LastName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ObjectGuid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastSyncedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DirectoryUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DirectoryUsers_Directories_DirectoryId",
                        column: x => x.DirectoryId,
                        principalTable: "Directories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWorkLogs_ApprovalId",
                table: "EmployeeWorkLogs",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_DirectoryUsers_DirectoryId",
                table: "DirectoryUsers",
                column: "DirectoryId");

            migrationBuilder.CreateIndex(
                name: "IX_DirectoryUsers_Username",
                table: "DirectoryUsers",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLeaves_EmployeeId_StartDate_EndDate",
                table: "EmployeeLeaves",
                columns: new[] { "EmployeeId", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkLogApprovals_EmployeeId_PeriodStart_PeriodEnd",
                table: "WorkLogApprovals",
                columns: new[] { "EmployeeId", "PeriodStart", "PeriodEnd" });

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeWorkLogs_WorkLogApprovals_ApprovalId",
                table: "EmployeeWorkLogs",
                column: "ApprovalId",
                principalTable: "WorkLogApprovals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeWorkLogs_WorkLogApprovals_ApprovalId",
                table: "EmployeeWorkLogs");

            migrationBuilder.DropTable(
                name: "DirectoryAttributeMappings");

            migrationBuilder.DropTable(
                name: "DirectoryUsers");

            migrationBuilder.DropTable(
                name: "EmployeeLeaves");

            migrationBuilder.DropTable(
                name: "WorkLogApprovals");

            migrationBuilder.DropTable(
                name: "Directories");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeWorkLogs_ApprovalId",
                table: "EmployeeWorkLogs");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ApprovalId",
                table: "EmployeeWorkLogs");
        }
    }
}
