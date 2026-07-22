using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EforTakip.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWorkLogCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeWorkLogs_Customers_CustomerId",
                table: "EmployeeWorkLogs");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeWorkLogs_CustomerId",
                table: "EmployeeWorkLogs");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "EmployeeWorkLogs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                table: "EmployeeWorkLogs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWorkLogs_CustomerId",
                table: "EmployeeWorkLogs",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeWorkLogs_Customers_CustomerId",
                table: "EmployeeWorkLogs",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
