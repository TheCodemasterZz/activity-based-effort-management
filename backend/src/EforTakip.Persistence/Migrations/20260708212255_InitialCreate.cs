using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EforTakip.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ParentActivityId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Activities_Activities_ParentActivityId",
                        column: x => x.ParentActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Holidays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Holidays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ValueStreams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValueStreams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkCalendars",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkCalendars", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectCustomers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectCustomers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectCustomers_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectCustomers_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ValueStreamStages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ValueStreamId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValueStreamStages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValueStreamStages_ValueStreams_ValueStreamId",
                        column: x => x.ValueStreamId,
                        principalTable: "ValueStreams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    WorkCalendarId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employees_WorkCalendars_WorkCalendarId",
                        column: x => x.WorkCalendarId,
                        principalTable: "WorkCalendars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkCalendarDays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkCalendarId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    IsWorkingDay = table.Column<bool>(type: "boolean", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkCalendarDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkCalendarDays_WorkCalendars_WorkCalendarId",
                        column: x => x.WorkCalendarId,
                        principalTable: "WorkCalendars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StageActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ValueStreamStageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StageActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StageActivities_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StageActivities_ValueStreamStages_ValueStreamStageId",
                        column: x => x.ValueStreamStageId,
                        principalTable: "ValueStreamStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeWorkLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityL1Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityL2Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Hours = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeWorkLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeWorkLogs_Activities_ActivityL1Id",
                        column: x => x.ActivityL1Id,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeWorkLogs_Activities_ActivityL2Id",
                        column: x => x.ActivityL2Id,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeWorkLogs_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeWorkLogs_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeWorkLogs_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectEmployees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectEmployees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectEmployees_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectEmployees_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Activities",
                columns: new[] { "Id", "Description", "Name", "ParentActivityId" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0003-000000000000"), null, "Analysis", null },
                    { new Guid("00000000-0000-0000-0003-000000001000"), null, "Enterprise Architecture", null },
                    { new Guid("00000000-0000-0000-0003-000000002000"), null, "Solution Architecture", null },
                    { new Guid("00000000-0000-0000-0003-000000003000"), null, "Regulatory Compliance", null },
                    { new Guid("00000000-0000-0000-0003-000000004000"), null, "Documentation", null },
                    { new Guid("00000000-0000-0000-0003-000000005000"), null, "Governance", null },
                    { new Guid("00000000-0000-0000-0003-000000006000"), null, "Planning", null },
                    { new Guid("00000000-0000-0000-0003-000000007000"), null, "Requirements Quality Control", null },
                    { new Guid("00000000-0000-0000-0003-000000008000"), null, "Traceability", null },
                    { new Guid("00000000-0000-0000-0003-000000009000"), null, "Architecture Design", null },
                    { new Guid("00000000-0000-0000-0003-000000010000"), null, "Design Quality Control", null },
                    { new Guid("00000000-0000-0000-0003-000000011000"), null, "Development", null },
                    { new Guid("00000000-0000-0000-0003-000000012000"), null, "Test Automation", null },
                    { new Guid("00000000-0000-0000-0003-000000013000"), null, "Testing", null },
                    { new Guid("00000000-0000-0000-0003-000000014000"), null, "Code Quality Assurance", null },
                    { new Guid("00000000-0000-0000-0003-000000015000"), null, "Quality Assurance", null },
                    { new Guid("00000000-0000-0000-0003-000000016000"), null, "DevOps", null },
                    { new Guid("00000000-0000-0000-0003-000000017000"), null, "Infrastructure", null },
                    { new Guid("00000000-0000-0000-0003-000000018000"), null, "Test Preparation & Design", null },
                    { new Guid("00000000-0000-0000-0003-000000019000"), null, "System Ops", null },
                    { new Guid("00000000-0000-0000-0003-000000020000"), null, "Operations", null },
                    { new Guid("00000000-0000-0000-0003-000000021000"), null, "Knowledge Transfer", null },
                    { new Guid("00000000-0000-0000-0003-000000022000"), null, "Training & Enablement", null }
                });

            migrationBuilder.InsertData(
                table: "Holidays",
                columns: new[] { "Id", "Date", "Name" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0005-000000000001"), new DateOnly(2026, 1, 1), "Yılbaşı" },
                    { new Guid("00000000-0000-0000-0005-000000000002"), new DateOnly(2026, 4, 23), "Ulusal Egemenlik ve Çocuk Bayramı" },
                    { new Guid("00000000-0000-0000-0005-000000000003"), new DateOnly(2026, 5, 1), "Emek ve Dayanışma Günü" },
                    { new Guid("00000000-0000-0000-0005-000000000004"), new DateOnly(2026, 5, 19), "Atatürk'ü Anma, Gençlik ve Spor Bayramı" },
                    { new Guid("00000000-0000-0000-0005-000000000005"), new DateOnly(2026, 7, 15), "Demokrasi ve Millî Birlik Günü" },
                    { new Guid("00000000-0000-0000-0005-000000000006"), new DateOnly(2026, 8, 30), "Zafer Bayramı" },
                    { new Guid("00000000-0000-0000-0005-000000000007"), new DateOnly(2026, 10, 29), "Cumhuriyet Bayramı" }
                });

            migrationBuilder.InsertData(
                table: "Notifications",
                columns: new[] { "Id", "CreatedAtUtc", "IsRead", "Message" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0008-000000000001"), new DateTime(2026, 7, 8, 9, 0, 0, 0, DateTimeKind.Utc), false, "Temmuz ayı efor raporu hazır." },
                    { new Guid("00000000-0000-0000-0008-000000000002"), new DateTime(2026, 7, 7, 14, 30, 0, 0, DateTimeKind.Utc), false, "'Software Delivery' değer akışına yeni bir aşama eklendi." },
                    { new Guid("00000000-0000-0000-0008-000000000003"), new DateTime(2026, 7, 6, 11, 15, 0, 0, DateTimeKind.Utc), false, "Mesai takviminizde güncelleme yapıldı." },
                    { new Guid("00000000-0000-0000-0008-000000000004"), new DateTime(2026, 7, 3, 8, 0, 0, 0, DateTimeKind.Utc), true, "15 Temmuz Demokrasi ve Millî Birlik Günü tatil takvimine eklendi." },
                    { new Guid("00000000-0000-0000-0008-000000000005"), new DateTime(2026, 7, 1, 17, 45, 0, 0, DateTimeKind.Utc), true, "Sistem bakımı bu hafta sonu planlanmıştır." }
                });

            migrationBuilder.InsertData(
                table: "ValueStreams",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[] { new Guid("00000000-0000-0000-0001-000000000000"), null, "Software Delivery" });

            migrationBuilder.InsertData(
                table: "WorkCalendars",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0006-000000000000"), "Standart Ofis Mesaisi" },
                    { new Guid("00000000-0000-0000-0006-000000000001"), "Esnek Vardiya" }
                });

            migrationBuilder.InsertData(
                table: "Activities",
                columns: new[] { "Id", "Description", "Name", "ParentActivityId" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0003-000000000001"), null, "Business Requirement Analysis", new Guid("00000000-0000-0000-0003-000000000000") },
                    { new Guid("00000000-0000-0000-0003-000000000002"), null, "Feasibility Study", new Guid("00000000-0000-0000-0003-000000000000") },
                    { new Guid("00000000-0000-0000-0003-000000000003"), null, "Gap Analysis", new Guid("00000000-0000-0000-0003-000000000000") },
                    { new Guid("00000000-0000-0000-0003-000000000004"), null, "Risk Analysis", new Guid("00000000-0000-0000-0003-000000000000") },
                    { new Guid("00000000-0000-0000-0003-000000000005"), null, "Stakeholder Identification & Workshops", new Guid("00000000-0000-0000-0003-000000000000") },
                    { new Guid("00000000-0000-0000-0003-000000000006"), null, "Requirements Elicitation, Analysis & Prioritization", new Guid("00000000-0000-0000-0003-000000000000") },
                    { new Guid("00000000-0000-0000-0003-000000000007"), null, "Functional / Non-Functional Req. Definition", new Guid("00000000-0000-0000-0003-000000000000") },
                    { new Guid("00000000-0000-0000-0003-000000000008"), null, "Acceptance Criteria & Business Rules Definition", new Guid("00000000-0000-0000-0003-000000000000") },
                    { new Guid("00000000-0000-0000-0003-000000000009"), null, "Impact Analysis", new Guid("00000000-0000-0000-0003-000000000000") },
                    { new Guid("00000000-0000-0000-0003-000000000010"), null, "Root Cause Analysis", new Guid("00000000-0000-0000-0003-000000000000") },
                    { new Guid("00000000-0000-0000-0003-000000001001"), null, "High-Level Solution Design", new Guid("00000000-0000-0000-0003-000000001000") },
                    { new Guid("00000000-0000-0000-0003-000000001002"), null, "Business Capability Impact Mapping", new Guid("00000000-0000-0000-0003-000000001000") },
                    { new Guid("00000000-0000-0000-0003-000000002001"), null, "High-Level Resource & Budget Estimation", new Guid("00000000-0000-0000-0003-000000002000") },
                    { new Guid("00000000-0000-0000-0003-000000002002"), null, "Roadmapping", new Guid("00000000-0000-0000-0003-000000002000") },
                    { new Guid("00000000-0000-0000-0003-000000003001"), null, "Standards & Framework Alignment", new Guid("00000000-0000-0000-0003-000000003000") },
                    { new Guid("00000000-0000-0000-0003-000000003002"), null, "Non-Conformance & Gap Risk Analysis", new Guid("00000000-0000-0000-0003-000000003000") },
                    { new Guid("00000000-0000-0000-0003-000000003003"), null, "Technical File & Documentation Pre-check", new Guid("00000000-0000-0000-0003-000000003000") },
                    { new Guid("00000000-0000-0000-0003-000000004001"), null, "Business Requirements Document (BRD)", new Guid("00000000-0000-0000-0003-000000004000") },
                    { new Guid("00000000-0000-0000-0003-000000004002"), null, "High-Level Solution Design Document", new Guid("00000000-0000-0000-0003-000000004000") },
                    { new Guid("00000000-0000-0000-0003-000000004003"), null, "Project Management Plan", new Guid("00000000-0000-0000-0003-000000004000") },
                    { new Guid("00000000-0000-0000-0003-000000004004"), null, "Resource Plan", new Guid("00000000-0000-0000-0003-000000004000") },
                    { new Guid("00000000-0000-0000-0003-000000004005"), null, "Risk Register", new Guid("00000000-0000-0000-0003-000000004000") },
                    { new Guid("00000000-0000-0000-0003-000000004006"), null, "Work Breakdown Structure (WBS)", new Guid("00000000-0000-0000-0003-000000004000") },
                    { new Guid("00000000-0000-0000-0003-000000004007"), null, "Technical Requirements Document (TRD)", new Guid("00000000-0000-0000-0003-000000004000") },
                    { new Guid("00000000-0000-0000-0003-000000004008"), null, "Requirements Traceability Matrix (RTM)", new Guid("00000000-0000-0000-0003-000000004000") },
                    { new Guid("00000000-0000-0000-0003-000000004009"), null, "High-Level Design Document (HLD)", new Guid("00000000-0000-0000-0003-000000004000") },
                    { new Guid("00000000-0000-0000-0003-000000004010"), null, "Low-Level Design Document (LLD)", new Guid("00000000-0000-0000-0003-000000004000") },
                    { new Guid("00000000-0000-0000-0003-000000004011"), null, "Build Configuration Update", new Guid("00000000-0000-0000-0003-000000004000") },
                    { new Guid("00000000-0000-0000-0003-000000004012"), null, "Operational Runbook", new Guid("00000000-0000-0000-0003-000000004000") },
                    { new Guid("00000000-0000-0000-0003-000000004013"), null, "User Documentation", new Guid("00000000-0000-0000-0003-000000004000") },
                    { new Guid("00000000-0000-0000-0003-000000005001"), null, "Governance Meetings", new Guid("00000000-0000-0000-0003-000000005000") },
                    { new Guid("00000000-0000-0000-0003-000000006001"), null, "Project Charter", new Guid("00000000-0000-0000-0003-000000006000") },
                    { new Guid("00000000-0000-0000-0003-000000006002"), null, "Scope Planning", new Guid("00000000-0000-0000-0003-000000006000") },
                    { new Guid("00000000-0000-0000-0003-000000006003"), null, "Schedule Planning", new Guid("00000000-0000-0000-0003-000000006000") },
                    { new Guid("00000000-0000-0000-0003-000000006004"), null, "Resource Planning", new Guid("00000000-0000-0000-0003-000000006000") },
                    { new Guid("00000000-0000-0000-0003-000000006005"), null, "Cost Planning", new Guid("00000000-0000-0000-0003-000000006000") },
                    { new Guid("00000000-0000-0000-0003-000000006006"), null, "Risk Planning", new Guid("00000000-0000-0000-0003-000000006000") },
                    { new Guid("00000000-0000-0000-0003-000000006007"), null, "Communication Planning", new Guid("00000000-0000-0000-0003-000000006000") },
                    { new Guid("00000000-0000-0000-0003-000000006008"), null, "Project Baseline Approval", new Guid("00000000-0000-0000-0003-000000006000") },
                    { new Guid("00000000-0000-0000-0003-000000007001"), null, "SQA Review (Software Quality Assurance Review)", new Guid("00000000-0000-0000-0003-000000007000") },
                    { new Guid("00000000-0000-0000-0003-000000007002"), null, "Stakeholder Review & Scope Baseline Approval", new Guid("00000000-0000-0000-0003-000000007000") },
                    { new Guid("00000000-0000-0000-0003-000000007003"), null, "Technical Review (Developer / Tester / Tech Lead)", new Guid("00000000-0000-0000-0003-000000007000") },
                    { new Guid("00000000-0000-0000-0003-000000008001"), null, "Biz Req -> Tech Req Traceability", new Guid("00000000-0000-0000-0003-000000008000") },
                    { new Guid("00000000-0000-0000-0003-000000008002"), null, "Tech Req -> Design Spec Traceability", new Guid("00000000-0000-0000-0003-000000008000") },
                    { new Guid("00000000-0000-0000-0003-000000008003"), null, "Design -> Source Code Traceability", new Guid("00000000-0000-0000-0003-000000008000") },
                    { new Guid("00000000-0000-0000-0003-000000008004"), null, "Test Case Traceability", new Guid("00000000-0000-0000-0003-000000008000") },
                    { new Guid("00000000-0000-0000-0003-000000009001"), null, "High-Level Design (System/Global Design)", new Guid("00000000-0000-0000-0003-000000009000") },
                    { new Guid("00000000-0000-0000-0003-000000009002"), null, "Low-Level Design (Component/Detailed Design)", new Guid("00000000-0000-0000-0003-000000009000") },
                    { new Guid("00000000-0000-0000-0003-000000009003"), null, "Integration & API / Interface Design", new Guid("00000000-0000-0000-0003-000000009000") },
                    { new Guid("00000000-0000-0000-0003-000000009004"), null, "Database & Data Model Design", new Guid("00000000-0000-0000-0003-000000009000") },
                    { new Guid("00000000-0000-0000-0003-000000009005"), null, "Solution Design", new Guid("00000000-0000-0000-0003-000000009000") },
                    { new Guid("00000000-0000-0000-0003-000000009006"), null, "Technical Standards", new Guid("00000000-0000-0000-0003-000000009000") },
                    { new Guid("00000000-0000-0000-0003-000000009007"), null, "Architecture Alignment", new Guid("00000000-0000-0000-0003-000000009000") },
                    { new Guid("00000000-0000-0000-0003-000000009008"), null, "Architecture Review", new Guid("00000000-0000-0000-0003-000000009000") },
                    { new Guid("00000000-0000-0000-0003-000000010001"), null, "SQA Review (Software Quality Assurance Review)", new Guid("00000000-0000-0000-0003-000000010000") },
                    { new Guid("00000000-0000-0000-0003-000000010002"), null, "Technical Team Peer Review", new Guid("00000000-0000-0000-0003-000000010000") },
                    { new Guid("00000000-0000-0000-0003-000000011001"), null, "Backend Development", new Guid("00000000-0000-0000-0003-000000011000") },
                    { new Guid("00000000-0000-0000-0003-000000011002"), null, "Frontend Development", new Guid("00000000-0000-0000-0003-000000011000") },
                    { new Guid("00000000-0000-0000-0003-000000011003"), null, "Database Development", new Guid("00000000-0000-0000-0003-000000011000") },
                    { new Guid("00000000-0000-0000-0003-000000011004"), null, "Interface / API Development", new Guid("00000000-0000-0000-0003-000000011000") },
                    { new Guid("00000000-0000-0000-0003-000000011005"), null, "Configuration Development", new Guid("00000000-0000-0000-0003-000000011000") },
                    { new Guid("00000000-0000-0000-0003-000000011006"), null, "Integration Development", new Guid("00000000-0000-0000-0003-000000011000") },
                    { new Guid("00000000-0000-0000-0003-000000012001"), null, "Automation Development", new Guid("00000000-0000-0000-0003-000000012000") },
                    { new Guid("00000000-0000-0000-0003-000000012002"), null, "Automation Pipeline", new Guid("00000000-0000-0000-0003-000000012000") },
                    { new Guid("00000000-0000-0000-0003-000000012003"), null, "E2E Test Suite", new Guid("00000000-0000-0000-0003-000000012000") },
                    { new Guid("00000000-0000-0000-0003-000000013001"), null, "Unit Test Execution & Code Coverage Check", new Guid("00000000-0000-0000-0003-000000013000") },
                    { new Guid("00000000-0000-0000-0003-000000013002"), null, "Functional Testing", new Guid("00000000-0000-0000-0003-000000013000") },
                    { new Guid("00000000-0000-0000-0003-000000013003"), null, "Security Testing", new Guid("00000000-0000-0000-0003-000000013000") },
                    { new Guid("00000000-0000-0000-0003-000000013004"), null, "Performance Testing", new Guid("00000000-0000-0000-0003-000000013000") },
                    { new Guid("00000000-0000-0000-0003-000000013005"), null, "Regression Testing", new Guid("00000000-0000-0000-0003-000000013000") },
                    { new Guid("00000000-0000-0000-0003-000000013006"), null, "Integration Testing", new Guid("00000000-0000-0000-0003-000000013000") },
                    { new Guid("00000000-0000-0000-0003-000000013007"), null, "User Acceptance Testing (UAT)", new Guid("00000000-0000-0000-0003-000000013000") },
                    { new Guid("00000000-0000-0000-0003-000000013008"), null, "Test Closure & Reporting", new Guid("00000000-0000-0000-0003-000000013000") },
                    { new Guid("00000000-0000-0000-0003-000000013009"), null, "Smoke Testing", new Guid("00000000-0000-0000-0003-000000013000") },
                    { new Guid("00000000-0000-0000-0003-000000013010"), null, "Sanity Testing", new Guid("00000000-0000-0000-0003-000000013000") },
                    { new Guid("00000000-0000-0000-0003-000000014001"), null, "Code Review & Static Code Analysis", new Guid("00000000-0000-0000-0003-000000014000") },
                    { new Guid("00000000-0000-0000-0003-000000014002"), null, "SonarQube Review", new Guid("00000000-0000-0000-0003-000000014000") },
                    { new Guid("00000000-0000-0000-0003-000000014003"), null, "Secure Coding Review", new Guid("00000000-0000-0000-0003-000000014000") },
                    { new Guid("00000000-0000-0000-0003-000000015001"), null, "QA Review", new Guid("00000000-0000-0000-0003-000000015000") },
                    { new Guid("00000000-0000-0000-0003-000000015002"), null, "Build Verification", new Guid("00000000-0000-0000-0003-000000015000") },
                    { new Guid("00000000-0000-0000-0003-000000015003"), null, "Defect Logging & Tracking", new Guid("00000000-0000-0000-0003-000000015000") },
                    { new Guid("00000000-0000-0000-0003-000000015004"), null, "Defect Management & Retesting", new Guid("00000000-0000-0000-0003-000000015000") },
                    { new Guid("00000000-0000-0000-0003-000000015005"), null, "Defect Management", new Guid("00000000-0000-0000-0003-000000015000") },
                    { new Guid("00000000-0000-0000-0003-000000015006"), null, "Test Review (SQA)", new Guid("00000000-0000-0000-0003-000000015000") },
                    { new Guid("00000000-0000-0000-0003-000000015007"), null, "Test Case Peer Review", new Guid("00000000-0000-0000-0003-000000015000") },
                    { new Guid("00000000-0000-0000-0003-000000015008"), null, "QA Sign-off", new Guid("00000000-0000-0000-0003-000000015000") },
                    { new Guid("00000000-0000-0000-0003-000000016001"), null, "Build Execution", new Guid("00000000-0000-0000-0003-000000016000") },
                    { new Guid("00000000-0000-0000-0003-000000016002"), null, "CI/CD Pipeline", new Guid("00000000-0000-0000-0003-000000016000") },
                    { new Guid("00000000-0000-0000-0003-000000016003"), null, "Dependency Management", new Guid("00000000-0000-0000-0003-000000016000") },
                    { new Guid("00000000-0000-0000-0003-000000016004"), null, "Package Generation", new Guid("00000000-0000-0000-0003-000000016000") },
                    { new Guid("00000000-0000-0000-0003-000000016005"), null, "Release Package Preparation", new Guid("00000000-0000-0000-0003-000000016000") },
                    { new Guid("00000000-0000-0000-0003-000000016006"), null, "Artifact Packaging", new Guid("00000000-0000-0000-0003-000000016000") },
                    { new Guid("00000000-0000-0000-0003-000000016007"), null, "Rollback Planning", new Guid("00000000-0000-0000-0003-000000016000") },
                    { new Guid("00000000-0000-0000-0003-000000016008"), null, "Deploy to Staging", new Guid("00000000-0000-0000-0003-000000016000") },
                    { new Guid("00000000-0000-0000-0003-000000016009"), null, "Deploy to Production", new Guid("00000000-0000-0000-0003-000000016000") },
                    { new Guid("00000000-0000-0000-0003-000000017001"), null, "Environment Configuration", new Guid("00000000-0000-0000-0003-000000017000") },
                    { new Guid("00000000-0000-0000-0003-000000017002"), null, "Environment Setup", new Guid("00000000-0000-0000-0003-000000017000") },
                    { new Guid("00000000-0000-0000-0003-000000017003"), null, "Infra Provisioning", new Guid("00000000-0000-0000-0003-000000017000") },
                    { new Guid("00000000-0000-0000-0003-000000017004"), null, "Cloud Configuration", new Guid("00000000-0000-0000-0003-000000017000") },
                    { new Guid("00000000-0000-0000-0003-000000018001"), null, "Test Plan & Strategy Document Creation", new Guid("00000000-0000-0000-0003-000000018000") },
                    { new Guid("00000000-0000-0000-0003-000000018002"), null, "Test Case Writing & Test Scenario Design", new Guid("00000000-0000-0000-0003-000000018000") },
                    { new Guid("00000000-0000-0000-0003-000000018003"), null, "Test Data Preparation & Environment Readiness Check", new Guid("00000000-0000-0000-0003-000000018000") },
                    { new Guid("00000000-0000-0000-0003-000000018004"), null, "Test Environment Setup", new Guid("00000000-0000-0000-0003-000000018000") },
                    { new Guid("00000000-0000-0000-0003-000000019001"), null, "System Configuration", new Guid("00000000-0000-0000-0003-000000019000") },
                    { new Guid("00000000-0000-0000-0003-000000019002"), null, "Service Registration", new Guid("00000000-0000-0000-0003-000000019000") },
                    { new Guid("00000000-0000-0000-0003-000000019003"), null, "Health Check", new Guid("00000000-0000-0000-0003-000000019000") },
                    { new Guid("00000000-0000-0000-0003-000000019004"), null, "Platform Management", new Guid("00000000-0000-0000-0003-000000019000") },
                    { new Guid("00000000-0000-0000-0003-000000020001"), null, "Monitoring Setup", new Guid("00000000-0000-0000-0003-000000020000") },
                    { new Guid("00000000-0000-0000-0003-000000020002"), null, "Alerting Configuration", new Guid("00000000-0000-0000-0003-000000020000") },
                    { new Guid("00000000-0000-0000-0003-000000020003"), null, "Operations Handover", new Guid("00000000-0000-0000-0003-000000020000") },
                    { new Guid("00000000-0000-0000-0003-000000020004"), null, "Go-Live Checklist", new Guid("00000000-0000-0000-0003-000000020000") },
                    { new Guid("00000000-0000-0000-0003-000000020005"), null, "Incident Management", new Guid("00000000-0000-0000-0003-000000020000") },
                    { new Guid("00000000-0000-0000-0003-000000020006"), null, "Problem Management", new Guid("00000000-0000-0000-0003-000000020000") },
                    { new Guid("00000000-0000-0000-0003-000000020007"), null, "Change Management", new Guid("00000000-0000-0000-0003-000000020000") },
                    { new Guid("00000000-0000-0000-0003-000000020008"), null, "Service Request Management", new Guid("00000000-0000-0000-0003-000000020000") },
                    { new Guid("00000000-0000-0000-0003-000000020009"), null, "Event Management", new Guid("00000000-0000-0000-0003-000000020000") },
                    { new Guid("00000000-0000-0000-0003-000000020010"), null, "Service Level Management", new Guid("00000000-0000-0000-0003-000000020000") },
                    { new Guid("00000000-0000-0000-0003-000000020011"), null, "Configuration Management (CMDB)", new Guid("00000000-0000-0000-0003-000000020000") },
                    { new Guid("00000000-0000-0000-0003-000000021001"), null, "Knowledge Transfer Session", new Guid("00000000-0000-0000-0003-000000021000") },
                    { new Guid("00000000-0000-0000-0003-000000022001"), null, "Administrator Training", new Guid("00000000-0000-0000-0003-000000022000") },
                    { new Guid("00000000-0000-0000-0003-000000022002"), null, "Technician / Technical Training", new Guid("00000000-0000-0000-0003-000000022000") },
                    { new Guid("00000000-0000-0000-0003-000000022003"), null, "End User Training", new Guid("00000000-0000-0000-0003-000000022000") }
                });

            migrationBuilder.InsertData(
                table: "ValueStreamStages",
                columns: new[] { "Id", "Name", "Order", "ValueStreamId" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0002-000000000001"), "Demand Definition", 1, new Guid("00000000-0000-0000-0001-000000000000") },
                    { new Guid("00000000-0000-0000-0002-000000000002"), "Delivery Planning", 2, new Guid("00000000-0000-0000-0001-000000000000") },
                    { new Guid("00000000-0000-0000-0002-000000000003"), "Requirements Scoping", 3, new Guid("00000000-0000-0000-0001-000000000000") },
                    { new Guid("00000000-0000-0000-0002-000000000004"), "Technical Design", 4, new Guid("00000000-0000-0000-0001-000000000000") },
                    { new Guid("00000000-0000-0000-0002-000000000005"), "Development", 5, new Guid("00000000-0000-0000-0001-000000000000") },
                    { new Guid("00000000-0000-0000-0002-000000000006"), "Code Quality", 6, new Guid("00000000-0000-0000-0001-000000000000") },
                    { new Guid("00000000-0000-0000-0002-000000000007"), "Build & Compilation", 7, new Guid("00000000-0000-0000-0001-000000000000") },
                    { new Guid("00000000-0000-0000-0002-000000000008"), "Pre-deployment Testing", 8, new Guid("00000000-0000-0000-0001-000000000000") },
                    { new Guid("00000000-0000-0000-0002-000000000009"), "Release Packing", 9, new Guid("00000000-0000-0000-0001-000000000000") },
                    { new Guid("00000000-0000-0000-0002-000000000010"), "Deployment", 10, new Guid("00000000-0000-0000-0001-000000000000") },
                    { new Guid("00000000-0000-0000-0002-000000000011"), "Post Deployment Testing & Validation", 11, new Guid("00000000-0000-0000-0001-000000000000") },
                    { new Guid("00000000-0000-0000-0002-000000000012"), "Handover To Operations", 12, new Guid("00000000-0000-0000-0001-000000000000") },
                    { new Guid("00000000-0000-0000-0002-000000000013"), "Go Live", 13, new Guid("00000000-0000-0000-0001-000000000000") },
                    { new Guid("00000000-0000-0000-0002-000000000014"), "Maintenance", 14, new Guid("00000000-0000-0000-0001-000000000000") }
                });

            migrationBuilder.InsertData(
                table: "WorkCalendarDays",
                columns: new[] { "Id", "DayOfWeek", "EndTime", "IsWorkingDay", "StartTime", "WorkCalendarId" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0007-000000000000"), 1, new TimeOnly(18, 0, 0), true, new TimeOnly(9, 0, 0), new Guid("00000000-0000-0000-0006-000000000000") },
                    { new Guid("00000000-0000-0000-0007-000000000001"), 2, new TimeOnly(18, 0, 0), true, new TimeOnly(9, 0, 0), new Guid("00000000-0000-0000-0006-000000000000") },
                    { new Guid("00000000-0000-0000-0007-000000000002"), 3, new TimeOnly(18, 0, 0), true, new TimeOnly(9, 0, 0), new Guid("00000000-0000-0000-0006-000000000000") },
                    { new Guid("00000000-0000-0000-0007-000000000003"), 4, new TimeOnly(18, 0, 0), true, new TimeOnly(9, 0, 0), new Guid("00000000-0000-0000-0006-000000000000") },
                    { new Guid("00000000-0000-0000-0007-000000000004"), 5, new TimeOnly(18, 0, 0), true, new TimeOnly(9, 0, 0), new Guid("00000000-0000-0000-0006-000000000000") },
                    { new Guid("00000000-0000-0000-0007-000000000005"), 6, null, false, null, new Guid("00000000-0000-0000-0006-000000000000") },
                    { new Guid("00000000-0000-0000-0007-000000000006"), 0, null, false, null, new Guid("00000000-0000-0000-0006-000000000000") },
                    { new Guid("00000000-0000-0000-0007-000000000010"), 1, new TimeOnly(17, 0, 0), true, new TimeOnly(9, 0, 0), new Guid("00000000-0000-0000-0006-000000000001") },
                    { new Guid("00000000-0000-0000-0007-000000000011"), 2, new TimeOnly(17, 0, 0), true, new TimeOnly(9, 0, 0), new Guid("00000000-0000-0000-0006-000000000001") },
                    { new Guid("00000000-0000-0000-0007-000000000012"), 3, new TimeOnly(17, 0, 0), true, new TimeOnly(9, 0, 0), new Guid("00000000-0000-0000-0006-000000000001") },
                    { new Guid("00000000-0000-0000-0007-000000000013"), 4, new TimeOnly(17, 0, 0), true, new TimeOnly(9, 0, 0), new Guid("00000000-0000-0000-0006-000000000001") },
                    { new Guid("00000000-0000-0000-0007-000000000014"), 5, new TimeOnly(17, 0, 0), true, new TimeOnly(9, 0, 0), new Guid("00000000-0000-0000-0006-000000000001") },
                    { new Guid("00000000-0000-0000-0007-000000000015"), 6, new TimeOnly(14, 0, 0), true, new TimeOnly(10, 0, 0), new Guid("00000000-0000-0000-0006-000000000001") },
                    { new Guid("00000000-0000-0000-0007-000000000016"), 0, null, false, null, new Guid("00000000-0000-0000-0006-000000000001") }
                });

            migrationBuilder.InsertData(
                table: "StageActivities",
                columns: new[] { "Id", "ActivityId", "ValueStreamStageId" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0004-000000000001"), new Guid("00000000-0000-0000-0003-000000000000"), new Guid("00000000-0000-0000-0002-000000000001") },
                    { new Guid("00000000-0000-0000-0004-000000000002"), new Guid("00000000-0000-0000-0003-000000001000"), new Guid("00000000-0000-0000-0002-000000000001") },
                    { new Guid("00000000-0000-0000-0004-000000000003"), new Guid("00000000-0000-0000-0003-000000002000"), new Guid("00000000-0000-0000-0002-000000000001") },
                    { new Guid("00000000-0000-0000-0004-000000000004"), new Guid("00000000-0000-0000-0003-000000003000"), new Guid("00000000-0000-0000-0002-000000000001") },
                    { new Guid("00000000-0000-0000-0004-000000000005"), new Guid("00000000-0000-0000-0003-000000004000"), new Guid("00000000-0000-0000-0002-000000000001") },
                    { new Guid("00000000-0000-0000-0004-000000000006"), new Guid("00000000-0000-0000-0003-000000005000"), new Guid("00000000-0000-0000-0002-000000000001") },
                    { new Guid("00000000-0000-0000-0004-000000000007"), new Guid("00000000-0000-0000-0003-000000006000"), new Guid("00000000-0000-0000-0002-000000000002") },
                    { new Guid("00000000-0000-0000-0004-000000000008"), new Guid("00000000-0000-0000-0003-000000007000"), new Guid("00000000-0000-0000-0002-000000000002") },
                    { new Guid("00000000-0000-0000-0004-000000000009"), new Guid("00000000-0000-0000-0003-000000004000"), new Guid("00000000-0000-0000-0002-000000000002") },
                    { new Guid("00000000-0000-0000-0004-000000000010"), new Guid("00000000-0000-0000-0003-000000005000"), new Guid("00000000-0000-0000-0002-000000000002") },
                    { new Guid("00000000-0000-0000-0004-000000000011"), new Guid("00000000-0000-0000-0003-000000000000"), new Guid("00000000-0000-0000-0002-000000000003") },
                    { new Guid("00000000-0000-0000-0004-000000000012"), new Guid("00000000-0000-0000-0003-000000008000"), new Guid("00000000-0000-0000-0002-000000000003") },
                    { new Guid("00000000-0000-0000-0004-000000000013"), new Guid("00000000-0000-0000-0003-000000007000"), new Guid("00000000-0000-0000-0002-000000000003") },
                    { new Guid("00000000-0000-0000-0004-000000000014"), new Guid("00000000-0000-0000-0003-000000004000"), new Guid("00000000-0000-0000-0002-000000000003") },
                    { new Guid("00000000-0000-0000-0004-000000000015"), new Guid("00000000-0000-0000-0003-000000005000"), new Guid("00000000-0000-0000-0002-000000000003") },
                    { new Guid("00000000-0000-0000-0004-000000000016"), new Guid("00000000-0000-0000-0003-000000009000"), new Guid("00000000-0000-0000-0002-000000000004") },
                    { new Guid("00000000-0000-0000-0004-000000000017"), new Guid("00000000-0000-0000-0003-000000008000"), new Guid("00000000-0000-0000-0002-000000000004") },
                    { new Guid("00000000-0000-0000-0004-000000000018"), new Guid("00000000-0000-0000-0003-000000010000"), new Guid("00000000-0000-0000-0002-000000000004") },
                    { new Guid("00000000-0000-0000-0004-000000000019"), new Guid("00000000-0000-0000-0003-000000004000"), new Guid("00000000-0000-0000-0002-000000000004") },
                    { new Guid("00000000-0000-0000-0004-000000000020"), new Guid("00000000-0000-0000-0003-000000005000"), new Guid("00000000-0000-0000-0002-000000000004") },
                    { new Guid("00000000-0000-0000-0004-000000000021"), new Guid("00000000-0000-0000-0003-000000011000"), new Guid("00000000-0000-0000-0002-000000000005") },
                    { new Guid("00000000-0000-0000-0004-000000000022"), new Guid("00000000-0000-0000-0003-000000012000"), new Guid("00000000-0000-0000-0002-000000000005") },
                    { new Guid("00000000-0000-0000-0004-000000000023"), new Guid("00000000-0000-0000-0003-000000013000"), new Guid("00000000-0000-0000-0002-000000000005") },
                    { new Guid("00000000-0000-0000-0004-000000000024"), new Guid("00000000-0000-0000-0003-000000008000"), new Guid("00000000-0000-0000-0002-000000000005") },
                    { new Guid("00000000-0000-0000-0004-000000000025"), new Guid("00000000-0000-0000-0003-000000014000"), new Guid("00000000-0000-0000-0002-000000000006") },
                    { new Guid("00000000-0000-0000-0004-000000000026"), new Guid("00000000-0000-0000-0003-000000009000"), new Guid("00000000-0000-0000-0002-000000000006") },
                    { new Guid("00000000-0000-0000-0004-000000000027"), new Guid("00000000-0000-0000-0003-000000015000"), new Guid("00000000-0000-0000-0002-000000000006") },
                    { new Guid("00000000-0000-0000-0004-000000000028"), new Guid("00000000-0000-0000-0003-000000016000"), new Guid("00000000-0000-0000-0002-000000000007") },
                    { new Guid("00000000-0000-0000-0004-000000000029"), new Guid("00000000-0000-0000-0003-000000012000"), new Guid("00000000-0000-0000-0002-000000000007") },
                    { new Guid("00000000-0000-0000-0004-000000000030"), new Guid("00000000-0000-0000-0003-000000017000"), new Guid("00000000-0000-0000-0002-000000000007") },
                    { new Guid("00000000-0000-0000-0004-000000000031"), new Guid("00000000-0000-0000-0003-000000015000"), new Guid("00000000-0000-0000-0002-000000000007") },
                    { new Guid("00000000-0000-0000-0004-000000000032"), new Guid("00000000-0000-0000-0003-000000004000"), new Guid("00000000-0000-0000-0002-000000000007") },
                    { new Guid("00000000-0000-0000-0004-000000000033"), new Guid("00000000-0000-0000-0003-000000018000"), new Guid("00000000-0000-0000-0002-000000000008") },
                    { new Guid("00000000-0000-0000-0004-000000000034"), new Guid("00000000-0000-0000-0003-000000013000"), new Guid("00000000-0000-0000-0002-000000000008") },
                    { new Guid("00000000-0000-0000-0004-000000000035"), new Guid("00000000-0000-0000-0003-000000012000"), new Guid("00000000-0000-0000-0002-000000000008") },
                    { new Guid("00000000-0000-0000-0004-000000000036"), new Guid("00000000-0000-0000-0003-000000015000"), new Guid("00000000-0000-0000-0002-000000000008") },
                    { new Guid("00000000-0000-0000-0004-000000000037"), new Guid("00000000-0000-0000-0003-000000000000"), new Guid("00000000-0000-0000-0002-000000000008") },
                    { new Guid("00000000-0000-0000-0004-000000000038"), new Guid("00000000-0000-0000-0003-000000008000"), new Guid("00000000-0000-0000-0002-000000000008") },
                    { new Guid("00000000-0000-0000-0004-000000000039"), new Guid("00000000-0000-0000-0003-000000016000"), new Guid("00000000-0000-0000-0002-000000000009") },
                    { new Guid("00000000-0000-0000-0004-000000000040"), new Guid("00000000-0000-0000-0003-000000015000"), new Guid("00000000-0000-0000-0002-000000000009") },
                    { new Guid("00000000-0000-0000-0004-000000000041"), new Guid("00000000-0000-0000-0003-000000004000"), new Guid("00000000-0000-0000-0002-000000000009") },
                    { new Guid("00000000-0000-0000-0004-000000000042"), new Guid("00000000-0000-0000-0003-000000005000"), new Guid("00000000-0000-0000-0002-000000000009") },
                    { new Guid("00000000-0000-0000-0004-000000000043"), new Guid("00000000-0000-0000-0003-000000016000"), new Guid("00000000-0000-0000-0002-000000000010") },
                    { new Guid("00000000-0000-0000-0004-000000000044"), new Guid("00000000-0000-0000-0003-000000017000"), new Guid("00000000-0000-0000-0002-000000000010") },
                    { new Guid("00000000-0000-0000-0004-000000000045"), new Guid("00000000-0000-0000-0003-000000019000"), new Guid("00000000-0000-0000-0002-000000000010") },
                    { new Guid("00000000-0000-0000-0004-000000000046"), new Guid("00000000-0000-0000-0003-000000013000"), new Guid("00000000-0000-0000-0002-000000000011") },
                    { new Guid("00000000-0000-0000-0004-000000000047"), new Guid("00000000-0000-0000-0003-000000020000"), new Guid("00000000-0000-0000-0002-000000000011") },
                    { new Guid("00000000-0000-0000-0004-000000000048"), new Guid("00000000-0000-0000-0003-000000015000"), new Guid("00000000-0000-0000-0002-000000000011") },
                    { new Guid("00000000-0000-0000-0004-000000000049"), new Guid("00000000-0000-0000-0003-000000019000"), new Guid("00000000-0000-0000-0002-000000000011") },
                    { new Guid("00000000-0000-0000-0004-000000000050"), new Guid("00000000-0000-0000-0003-000000020000"), new Guid("00000000-0000-0000-0002-000000000012") },
                    { new Guid("00000000-0000-0000-0004-000000000051"), new Guid("00000000-0000-0000-0003-000000019000"), new Guid("00000000-0000-0000-0002-000000000012") },
                    { new Guid("00000000-0000-0000-0004-000000000052"), new Guid("00000000-0000-0000-0003-000000021000"), new Guid("00000000-0000-0000-0002-000000000012") },
                    { new Guid("00000000-0000-0000-0004-000000000053"), new Guid("00000000-0000-0000-0003-000000022000"), new Guid("00000000-0000-0000-0002-000000000012") },
                    { new Guid("00000000-0000-0000-0004-000000000054"), new Guid("00000000-0000-0000-0003-000000004000"), new Guid("00000000-0000-0000-0002-000000000012") },
                    { new Guid("00000000-0000-0000-0004-000000000055"), new Guid("00000000-0000-0000-0003-000000005000"), new Guid("00000000-0000-0000-0002-000000000012") },
                    { new Guid("00000000-0000-0000-0004-000000000056"), new Guid("00000000-0000-0000-0003-000000020000"), new Guid("00000000-0000-0000-0002-000000000013") },
                    { new Guid("00000000-0000-0000-0004-000000000057"), new Guid("00000000-0000-0000-0003-000000022000"), new Guid("00000000-0000-0000-0002-000000000013") },
                    { new Guid("00000000-0000-0000-0004-000000000058"), new Guid("00000000-0000-0000-0003-000000019000"), new Guid("00000000-0000-0000-0002-000000000013") },
                    { new Guid("00000000-0000-0000-0004-000000000059"), new Guid("00000000-0000-0000-0003-000000005000"), new Guid("00000000-0000-0000-0002-000000000013") },
                    { new Guid("00000000-0000-0000-0004-000000000060"), new Guid("00000000-0000-0000-0003-000000020000"), new Guid("00000000-0000-0000-0002-000000000014") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_ParentActivityId",
                table: "Activities",
                column: "ParentActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_WorkCalendarId",
                table: "Employees",
                column: "WorkCalendarId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWorkLogs_ActivityL1Id",
                table: "EmployeeWorkLogs",
                column: "ActivityL1Id");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWorkLogs_ActivityL2Id",
                table: "EmployeeWorkLogs",
                column: "ActivityL2Id");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWorkLogs_CustomerId",
                table: "EmployeeWorkLogs",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWorkLogs_EmployeeId_WorkDate",
                table: "EmployeeWorkLogs",
                columns: new[] { "EmployeeId", "WorkDate" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWorkLogs_ProjectId",
                table: "EmployeeWorkLogs",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_Date",
                table: "Holidays",
                column: "Date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCustomers_CustomerId",
                table: "ProjectCustomers",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCustomers_ProjectId_CustomerId",
                table: "ProjectCustomers",
                columns: new[] { "ProjectId", "CustomerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectEmployees_EmployeeId",
                table: "ProjectEmployees",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectEmployees_ProjectId_EmployeeId",
                table: "ProjectEmployees",
                columns: new[] { "ProjectId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StageActivities_ActivityId",
                table: "StageActivities",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_StageActivities_ValueStreamStageId_ActivityId",
                table: "StageActivities",
                columns: new[] { "ValueStreamStageId", "ActivityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ValueStreamStages_ValueStreamId",
                table: "ValueStreamStages",
                column: "ValueStreamId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkCalendarDays_WorkCalendarId_DayOfWeek",
                table: "WorkCalendarDays",
                columns: new[] { "WorkCalendarId", "DayOfWeek" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeWorkLogs");

            migrationBuilder.DropTable(
                name: "Holidays");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "ProjectCustomers");

            migrationBuilder.DropTable(
                name: "ProjectEmployees");

            migrationBuilder.DropTable(
                name: "StageActivities");

            migrationBuilder.DropTable(
                name: "WorkCalendarDays");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Activities");

            migrationBuilder.DropTable(
                name: "ValueStreamStages");

            migrationBuilder.DropTable(
                name: "WorkCalendars");

            migrationBuilder.DropTable(
                name: "ValueStreams");
        }
    }
}
