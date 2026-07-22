using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EforTakip.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectoryUserAttributeReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReferencedDirectoryUserId",
                table: "DirectoryUserAttributes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DirectoryUserAttributes_ReferencedDirectoryUserId",
                table: "DirectoryUserAttributes",
                column: "ReferencedDirectoryUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_DirectoryUserAttributes_DirectoryUsers_ReferencedDirectoryU~",
                table: "DirectoryUserAttributes",
                column: "ReferencedDirectoryUserId",
                principalTable: "DirectoryUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DirectoryUserAttributes_DirectoryUsers_ReferencedDirectoryU~",
                table: "DirectoryUserAttributes");

            migrationBuilder.DropIndex(
                name: "IX_DirectoryUserAttributes_ReferencedDirectoryUserId",
                table: "DirectoryUserAttributes");

            migrationBuilder.DropColumn(
                name: "ReferencedDirectoryUserId",
                table: "DirectoryUserAttributes");
        }
    }
}
