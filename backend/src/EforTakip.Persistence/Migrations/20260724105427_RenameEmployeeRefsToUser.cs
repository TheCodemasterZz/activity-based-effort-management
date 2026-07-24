using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EforTakip.Persistence.Migrations
{
    /// <summary>
    /// EmployeeId -> UserId geçişi (Faz 3). Otomatik üretilen Drop+Create yerine tamamen
    /// Rename + FK yeniden bağlama kullanılır — hiçbir tablo silinmez, hiçbir veri kaybolmaz.
    /// FK'lar artık Employees yerine Users tablosuna işaret eder (tümü Restrict).
    /// WorkLogApprovals'a daha önce hiç olmayan Users FK'sı da burada eklenir.
    /// </summary>
    public partial class RenameEmployeeRefsToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Employees'e işaret eden tüm FK'lar kalkar (yeniden Users'a bağlanacaklar).
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectIssues_Employees_OwnerEmployeeId",
                table: "ProjectIssues");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectRisks_Employees_OwnerEmployeeId",
                table: "ProjectRisks");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Employees_ProjectManagerEmployeeId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTasks_Employees_AssignedEmployeeId",
                table: "ProjectTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeWorkLogs_Employees_EmployeeId",
                table: "EmployeeWorkLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeLeaves_Employees_EmployeeId",
                table: "EmployeeLeaves");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectEmployees_Employees_EmployeeId",
                table: "ProjectEmployees");

            // Adı değişen tabloların diğer FK'ları da (yeni adla yeniden eklenmek üzere) kalkar.
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeWorkLogs_Activities_ActivityL1Id",
                table: "EmployeeWorkLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeWorkLogs_Activities_ActivityL2Id",
                table: "EmployeeWorkLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeWorkLogs_Projects_ProjectId",
                table: "EmployeeWorkLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeWorkLogs_WorkLogApprovals_ApprovalId",
                table: "EmployeeWorkLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectEmployees_Projects_ProjectId",
                table: "ProjectEmployees");

            // 2) Tablo rename'leri — veri yerinde kalır.
            migrationBuilder.RenameTable(name: "EmployeeWorkLogs", newName: "WorkLogs");
            migrationBuilder.RenameTable(name: "EmployeeLeaves", newName: "Leaves");
            migrationBuilder.RenameTable(name: "ProjectEmployees", newName: "ProjectUsers");

            // 3) Kolon rename'leri.
            migrationBuilder.RenameColumn(name: "EmployeeId", table: "WorkLogs", newName: "UserId");
            migrationBuilder.RenameColumn(name: "EmployeeId", table: "Leaves", newName: "UserId");
            migrationBuilder.RenameColumn(name: "EmployeeId", table: "ProjectUsers", newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "EmployeeId", table: "WorkLogApprovals", newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "AssignedEmployeeId", table: "ProjectTasks", newName: "AssignedUserId");

            migrationBuilder.RenameColumn(
                name: "ProjectManagerEmployeeId", table: "Projects", newName: "ProjectManagerUserId");

            migrationBuilder.RenameColumn(
                name: "OwnerEmployeeId", table: "ProjectRisks", newName: "OwnerUserId");

            migrationBuilder.RenameColumn(
                name: "OwnerEmployeeId", table: "ProjectIssues", newName: "OwnerUserId");

            // 4) Index rename'leri.
            migrationBuilder.RenameIndex(
                name: "IX_EmployeeWorkLogs_ActivityL1Id", table: "WorkLogs",
                newName: "IX_WorkLogs_ActivityL1Id");
            migrationBuilder.RenameIndex(
                name: "IX_EmployeeWorkLogs_ActivityL2Id", table: "WorkLogs",
                newName: "IX_WorkLogs_ActivityL2Id");
            migrationBuilder.RenameIndex(
                name: "IX_EmployeeWorkLogs_ApprovalId", table: "WorkLogs",
                newName: "IX_WorkLogs_ApprovalId");
            migrationBuilder.RenameIndex(
                name: "IX_EmployeeWorkLogs_ProjectId", table: "WorkLogs",
                newName: "IX_WorkLogs_ProjectId");
            migrationBuilder.RenameIndex(
                name: "IX_EmployeeWorkLogs_EmployeeId_EntryType_WorkDate", table: "WorkLogs",
                newName: "IX_WorkLogs_UserId_EntryType_WorkDate");

            migrationBuilder.RenameIndex(
                name: "IX_EmployeeLeaves_EmployeeId_StartDate_EndDate", table: "Leaves",
                newName: "IX_Leaves_UserId_StartDate_EndDate");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectEmployees_EmployeeId", table: "ProjectUsers",
                newName: "IX_ProjectUsers_UserId");
            migrationBuilder.RenameIndex(
                name: "IX_ProjectEmployees_ProjectId_EmployeeId", table: "ProjectUsers",
                newName: "IX_ProjectUsers_ProjectId_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_WorkLogApprovals_EmployeeId_EntryType_PeriodStart_PeriodEnd",
                table: "WorkLogApprovals",
                newName: "IX_WorkLogApprovals_UserId_EntryType_PeriodStart_PeriodEnd");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectTasks_AssignedEmployeeId", table: "ProjectTasks",
                newName: "IX_ProjectTasks_AssignedUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_ProjectManagerEmployeeId", table: "Projects",
                newName: "IX_Projects_ProjectManagerUserId");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectRisks_OwnerEmployeeId", table: "ProjectRisks",
                newName: "IX_ProjectRisks_OwnerUserId");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectIssues_OwnerEmployeeId", table: "ProjectIssues",
                newName: "IX_ProjectIssues_OwnerUserId");

            // 5) PK constraint adları — RenameTable bunları değiştirmez, ileride üretilecek
            // migration'lar yeni adları bekler.
            migrationBuilder.Sql("ALTER TABLE \"WorkLogs\" RENAME CONSTRAINT \"PK_EmployeeWorkLogs\" TO \"PK_WorkLogs\";");
            migrationBuilder.Sql("ALTER TABLE \"Leaves\" RENAME CONSTRAINT \"PK_EmployeeLeaves\" TO \"PK_Leaves\";");
            migrationBuilder.Sql("ALTER TABLE \"ProjectUsers\" RENAME CONSTRAINT \"PK_ProjectEmployees\" TO \"PK_ProjectUsers\";");

            // 6) FK'lar yeni adlarla geri eklenir — kullanıcı FK'ları artık Users'a işaret eder.
            migrationBuilder.AddForeignKey(
                name: "FK_WorkLogs_Users_UserId", table: "WorkLogs", column: "UserId",
                principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkLogs_Activities_ActivityL1Id", table: "WorkLogs", column: "ActivityL1Id",
                principalTable: "Activities", principalColumn: "Id", onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkLogs_Activities_ActivityL2Id", table: "WorkLogs", column: "ActivityL2Id",
                principalTable: "Activities", principalColumn: "Id", onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkLogs_Projects_ProjectId", table: "WorkLogs", column: "ProjectId",
                principalTable: "Projects", principalColumn: "Id", onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkLogs_WorkLogApprovals_ApprovalId", table: "WorkLogs", column: "ApprovalId",
                principalTable: "WorkLogApprovals", principalColumn: "Id", onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Leaves_Users_UserId", table: "Leaves", column: "UserId",
                principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectUsers_Users_UserId", table: "ProjectUsers", column: "UserId",
                principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectUsers_Projects_ProjectId", table: "ProjectUsers", column: "ProjectId",
                principalTable: "Projects", principalColumn: "Id", onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectIssues_Users_OwnerUserId", table: "ProjectIssues", column: "OwnerUserId",
                principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectRisks_Users_OwnerUserId", table: "ProjectRisks", column: "OwnerUserId",
                principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Users_ProjectManagerUserId", table: "Projects", column: "ProjectManagerUserId",
                principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTasks_Users_AssignedUserId", table: "ProjectTasks", column: "AssignedUserId",
                principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.Restrict);

            // Daha önce hiç olmayan FK — veri güvenliği için bu fazda eklendi.
            migrationBuilder.AddForeignKey(
                name: "FK_WorkLogApprovals_Users_UserId", table: "WorkLogApprovals", column: "UserId",
                principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_WorkLogs_Users_UserId", table: "WorkLogs");
            migrationBuilder.DropForeignKey(name: "FK_WorkLogs_Activities_ActivityL1Id", table: "WorkLogs");
            migrationBuilder.DropForeignKey(name: "FK_WorkLogs_Activities_ActivityL2Id", table: "WorkLogs");
            migrationBuilder.DropForeignKey(name: "FK_WorkLogs_Projects_ProjectId", table: "WorkLogs");
            migrationBuilder.DropForeignKey(name: "FK_WorkLogs_WorkLogApprovals_ApprovalId", table: "WorkLogs");
            migrationBuilder.DropForeignKey(name: "FK_Leaves_Users_UserId", table: "Leaves");
            migrationBuilder.DropForeignKey(name: "FK_ProjectUsers_Users_UserId", table: "ProjectUsers");
            migrationBuilder.DropForeignKey(name: "FK_ProjectUsers_Projects_ProjectId", table: "ProjectUsers");
            migrationBuilder.DropForeignKey(name: "FK_ProjectIssues_Users_OwnerUserId", table: "ProjectIssues");
            migrationBuilder.DropForeignKey(name: "FK_ProjectRisks_Users_OwnerUserId", table: "ProjectRisks");
            migrationBuilder.DropForeignKey(name: "FK_Projects_Users_ProjectManagerUserId", table: "Projects");
            migrationBuilder.DropForeignKey(name: "FK_ProjectTasks_Users_AssignedUserId", table: "ProjectTasks");
            migrationBuilder.DropForeignKey(name: "FK_WorkLogApprovals_Users_UserId", table: "WorkLogApprovals");

            migrationBuilder.Sql("ALTER TABLE \"WorkLogs\" RENAME CONSTRAINT \"PK_WorkLogs\" TO \"PK_EmployeeWorkLogs\";");
            migrationBuilder.Sql("ALTER TABLE \"Leaves\" RENAME CONSTRAINT \"PK_Leaves\" TO \"PK_EmployeeLeaves\";");
            migrationBuilder.Sql("ALTER TABLE \"ProjectUsers\" RENAME CONSTRAINT \"PK_ProjectUsers\" TO \"PK_ProjectEmployees\";");

            migrationBuilder.RenameIndex(
                name: "IX_WorkLogs_ActivityL1Id", table: "WorkLogs",
                newName: "IX_EmployeeWorkLogs_ActivityL1Id");
            migrationBuilder.RenameIndex(
                name: "IX_WorkLogs_ActivityL2Id", table: "WorkLogs",
                newName: "IX_EmployeeWorkLogs_ActivityL2Id");
            migrationBuilder.RenameIndex(
                name: "IX_WorkLogs_ApprovalId", table: "WorkLogs",
                newName: "IX_EmployeeWorkLogs_ApprovalId");
            migrationBuilder.RenameIndex(
                name: "IX_WorkLogs_ProjectId", table: "WorkLogs",
                newName: "IX_EmployeeWorkLogs_ProjectId");
            migrationBuilder.RenameIndex(
                name: "IX_WorkLogs_UserId_EntryType_WorkDate", table: "WorkLogs",
                newName: "IX_EmployeeWorkLogs_EmployeeId_EntryType_WorkDate");
            migrationBuilder.RenameIndex(
                name: "IX_Leaves_UserId_StartDate_EndDate", table: "Leaves",
                newName: "IX_EmployeeLeaves_EmployeeId_StartDate_EndDate");
            migrationBuilder.RenameIndex(
                name: "IX_ProjectUsers_UserId", table: "ProjectUsers",
                newName: "IX_ProjectEmployees_EmployeeId");
            migrationBuilder.RenameIndex(
                name: "IX_ProjectUsers_ProjectId_UserId", table: "ProjectUsers",
                newName: "IX_ProjectEmployees_ProjectId_EmployeeId");
            migrationBuilder.RenameIndex(
                name: "IX_WorkLogApprovals_UserId_EntryType_PeriodStart_PeriodEnd",
                table: "WorkLogApprovals",
                newName: "IX_WorkLogApprovals_EmployeeId_EntryType_PeriodStart_PeriodEnd");
            migrationBuilder.RenameIndex(
                name: "IX_ProjectTasks_AssignedUserId", table: "ProjectTasks",
                newName: "IX_ProjectTasks_AssignedEmployeeId");
            migrationBuilder.RenameIndex(
                name: "IX_Projects_ProjectManagerUserId", table: "Projects",
                newName: "IX_Projects_ProjectManagerEmployeeId");
            migrationBuilder.RenameIndex(
                name: "IX_ProjectRisks_OwnerUserId", table: "ProjectRisks",
                newName: "IX_ProjectRisks_OwnerEmployeeId");
            migrationBuilder.RenameIndex(
                name: "IX_ProjectIssues_OwnerUserId", table: "ProjectIssues",
                newName: "IX_ProjectIssues_OwnerEmployeeId");

            migrationBuilder.RenameColumn(name: "UserId", table: "WorkLogs", newName: "EmployeeId");
            migrationBuilder.RenameColumn(name: "UserId", table: "Leaves", newName: "EmployeeId");
            migrationBuilder.RenameColumn(name: "UserId", table: "ProjectUsers", newName: "EmployeeId");
            migrationBuilder.RenameColumn(name: "UserId", table: "WorkLogApprovals", newName: "EmployeeId");
            migrationBuilder.RenameColumn(name: "AssignedUserId", table: "ProjectTasks", newName: "AssignedEmployeeId");
            migrationBuilder.RenameColumn(name: "ProjectManagerUserId", table: "Projects", newName: "ProjectManagerEmployeeId");
            migrationBuilder.RenameColumn(name: "OwnerUserId", table: "ProjectRisks", newName: "OwnerEmployeeId");
            migrationBuilder.RenameColumn(name: "OwnerUserId", table: "ProjectIssues", newName: "OwnerEmployeeId");

            migrationBuilder.RenameTable(name: "WorkLogs", newName: "EmployeeWorkLogs");
            migrationBuilder.RenameTable(name: "Leaves", newName: "EmployeeLeaves");
            migrationBuilder.RenameTable(name: "ProjectUsers", newName: "ProjectEmployees");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeWorkLogs_Employees_EmployeeId", table: "EmployeeWorkLogs", column: "EmployeeId",
                principalTable: "Employees", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeWorkLogs_Activities_ActivityL1Id", table: "EmployeeWorkLogs", column: "ActivityL1Id",
                principalTable: "Activities", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeWorkLogs_Activities_ActivityL2Id", table: "EmployeeWorkLogs", column: "ActivityL2Id",
                principalTable: "Activities", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeWorkLogs_Projects_ProjectId", table: "EmployeeWorkLogs", column: "ProjectId",
                principalTable: "Projects", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeWorkLogs_WorkLogApprovals_ApprovalId", table: "EmployeeWorkLogs", column: "ApprovalId",
                principalTable: "WorkLogApprovals", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeLeaves_Employees_EmployeeId", table: "EmployeeLeaves", column: "EmployeeId",
                principalTable: "Employees", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(
                name: "FK_ProjectEmployees_Employees_EmployeeId", table: "ProjectEmployees", column: "EmployeeId",
                principalTable: "Employees", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(
                name: "FK_ProjectEmployees_Projects_ProjectId", table: "ProjectEmployees", column: "ProjectId",
                principalTable: "Projects", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(
                name: "FK_ProjectIssues_Employees_OwnerEmployeeId", table: "ProjectIssues", column: "OwnerEmployeeId",
                principalTable: "Employees", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(
                name: "FK_ProjectRisks_Employees_OwnerEmployeeId", table: "ProjectRisks", column: "OwnerEmployeeId",
                principalTable: "Employees", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Employees_ProjectManagerEmployeeId", table: "Projects", column: "ProjectManagerEmployeeId",
                principalTable: "Employees", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTasks_Employees_AssignedEmployeeId", table: "ProjectTasks", column: "AssignedEmployeeId",
                principalTable: "Employees", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        }
    }
}
