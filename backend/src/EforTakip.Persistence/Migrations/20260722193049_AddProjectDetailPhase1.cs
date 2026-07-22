using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EforTakip.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectDetailPhase1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignedEmployeeId",
                table: "ProjectTasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DependsOnTaskId",
                table: "ProjectTasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentTaskId",
                table: "ProjectTasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "Projects",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectManagerEmployeeId",
                table: "Projects",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sponsor",
                table: "Projects",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StrategicGoal",
                table: "Projects",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProjectIssues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OwnerEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Resolution = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectIssues_Employees_OwnerEmployeeId",
                        column: x => x.OwnerEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectRisks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Probability = table.Column<int>(type: "integer", nullable: false),
                    Impact = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MitigationPlan = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    OwnerEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    IdentifiedDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectRisks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectRisks_Employees_OwnerEmployeeId",
                        column: x => x.OwnerEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_AssignedEmployeeId",
                table: "ProjectTasks",
                column: "AssignedEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_DependsOnTaskId",
                table: "ProjectTasks",
                column: "DependsOnTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_ParentTaskId",
                table: "ProjectTasks",
                column: "ParentTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProjectManagerEmployeeId",
                table: "Projects",
                column: "ProjectManagerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectIssues_OwnerEmployeeId",
                table: "ProjectIssues",
                column: "OwnerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectIssues_ProjectId",
                table: "ProjectIssues",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectRisks_OwnerEmployeeId",
                table: "ProjectRisks",
                column: "OwnerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectRisks_ProjectId",
                table: "ProjectRisks",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Employees_ProjectManagerEmployeeId",
                table: "Projects",
                column: "ProjectManagerEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTasks_Employees_AssignedEmployeeId",
                table: "ProjectTasks",
                column: "AssignedEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTasks_ProjectTasks_DependsOnTaskId",
                table: "ProjectTasks",
                column: "DependsOnTaskId",
                principalTable: "ProjectTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTasks_ProjectTasks_ParentTaskId",
                table: "ProjectTasks",
                column: "ParentTaskId",
                principalTable: "ProjectTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Employees_ProjectManagerEmployeeId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTasks_Employees_AssignedEmployeeId",
                table: "ProjectTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTasks_ProjectTasks_DependsOnTaskId",
                table: "ProjectTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTasks_ProjectTasks_ParentTaskId",
                table: "ProjectTasks");

            migrationBuilder.DropTable(
                name: "ProjectIssues");

            migrationBuilder.DropTable(
                name: "ProjectRisks");

            migrationBuilder.DropIndex(
                name: "IX_ProjectTasks_AssignedEmployeeId",
                table: "ProjectTasks");

            migrationBuilder.DropIndex(
                name: "IX_ProjectTasks_DependsOnTaskId",
                table: "ProjectTasks");

            migrationBuilder.DropIndex(
                name: "IX_ProjectTasks_ParentTaskId",
                table: "ProjectTasks");

            migrationBuilder.DropIndex(
                name: "IX_Projects_ProjectManagerEmployeeId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "AssignedEmployeeId",
                table: "ProjectTasks");

            migrationBuilder.DropColumn(
                name: "DependsOnTaskId",
                table: "ProjectTasks");

            migrationBuilder.DropColumn(
                name: "ParentTaskId",
                table: "ProjectTasks");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ProjectManagerEmployeeId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Sponsor",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "StrategicGoal",
                table: "Projects");
        }
    }
}
