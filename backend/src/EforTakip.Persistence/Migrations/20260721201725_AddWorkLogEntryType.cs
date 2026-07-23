using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EforTakip.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkLogEntryType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmployeeWorkLogs_EmployeeId_WorkDate",
                table: "EmployeeWorkLogs");

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovalId",
                table: "EmployeeWorkLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntryType",
                table: "EmployeeWorkLogs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

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
                    ApprovedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EntryType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkLogApprovals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWorkLogs_ApprovalId",
                table: "EmployeeWorkLogs",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWorkLogs_EmployeeId_EntryType_WorkDate",
                table: "EmployeeWorkLogs",
                columns: new[] { "EmployeeId", "EntryType", "WorkDate" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLeaves_EmployeeId_StartDate_EndDate",
                table: "EmployeeLeaves",
                columns: new[] { "EmployeeId", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkLogApprovals_EmployeeId_EntryType_PeriodStart_PeriodEnd",
                table: "WorkLogApprovals",
                columns: new[] { "EmployeeId", "EntryType", "PeriodStart", "PeriodEnd" });

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
                name: "EmployeeLeaves");

            migrationBuilder.DropTable(
                name: "WorkLogApprovals");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeWorkLogs_ApprovalId",
                table: "EmployeeWorkLogs");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeWorkLogs_EmployeeId_EntryType_WorkDate",
                table: "EmployeeWorkLogs");

            migrationBuilder.DropColumn(
                name: "ApprovalId",
                table: "EmployeeWorkLogs");

            migrationBuilder.DropColumn(
                name: "EntryType",
                table: "EmployeeWorkLogs");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWorkLogs_EmployeeId_WorkDate",
                table: "EmployeeWorkLogs",
                columns: new[] { "EmployeeId", "WorkDate" });
        }
    }
}
