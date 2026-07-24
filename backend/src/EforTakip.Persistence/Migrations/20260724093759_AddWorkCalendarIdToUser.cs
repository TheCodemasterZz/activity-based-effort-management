using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EforTakip.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkCalendarIdToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WorkCalendarId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_WorkCalendarId",
                table: "Users",
                column: "WorkCalendarId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_WorkCalendars_WorkCalendarId",
                table: "Users",
                column: "WorkCalendarId",
                principalTable: "WorkCalendars",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_WorkCalendars_WorkCalendarId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_WorkCalendarId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "WorkCalendarId",
                table: "Users");
        }
    }
}
