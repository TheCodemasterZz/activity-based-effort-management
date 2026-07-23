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

            migrationBuilder.DropIndex(
                name: "IX_WorkLogApprovals_EmployeeId_PeriodStart_PeriodEnd",
                table: "WorkLogApprovals");

            migrationBuilder.AddColumn<string>(
                name: "EntryType",
                table: "EmployeeWorkLogs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EntryType",
                table: "WorkLogApprovals",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWorkLogs_EmployeeId_EntryType_WorkDate",
                table: "EmployeeWorkLogs",
                columns: new[] { "EmployeeId", "EntryType", "WorkDate" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkLogApprovals_EmployeeId_EntryType_PeriodStart_PeriodEnd",
                table: "WorkLogApprovals",
                columns: new[] { "EmployeeId", "EntryType", "PeriodStart", "PeriodEnd" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmployeeWorkLogs_EmployeeId_EntryType_WorkDate",
                table: "EmployeeWorkLogs");

            migrationBuilder.DropIndex(
                name: "IX_WorkLogApprovals_EmployeeId_EntryType_PeriodStart_PeriodEnd",
                table: "WorkLogApprovals");

            migrationBuilder.DropColumn(
                name: "EntryType",
                table: "EmployeeWorkLogs");

            migrationBuilder.DropColumn(
                name: "EntryType",
                table: "WorkLogApprovals");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWorkLogs_EmployeeId_WorkDate",
                table: "EmployeeWorkLogs",
                columns: new[] { "EmployeeId", "WorkDate" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkLogApprovals_EmployeeId_PeriodStart_PeriodEnd",
                table: "WorkLogApprovals",
                columns: new[] { "EmployeeId", "PeriodStart", "PeriodEnd" });
        }
    }
}
