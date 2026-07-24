using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EforTakip.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameDirectoryUserToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(name: "DirectoryUsers", newName: "Users");
            migrationBuilder.RenameTable(name: "DirectoryUserAttributes", newName: "UserAttributes");
            migrationBuilder.RenameTable(name: "DirectoryUserRoles", newName: "UserRoles");

            migrationBuilder.RenameColumn(
                name: "DirectoryUserId", table: "UserAttributes", newName: "UserId");
            migrationBuilder.RenameColumn(
                name: "ReferencedDirectoryUserId", table: "UserAttributes", newName: "ReferencedUserId");
            migrationBuilder.RenameColumn(
                name: "DirectoryUserId", table: "UserRoles", newName: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserId", table: "UserRoles", newName: "DirectoryUserId");
            migrationBuilder.RenameColumn(
                name: "ReferencedUserId", table: "UserAttributes", newName: "ReferencedDirectoryUserId");
            migrationBuilder.RenameColumn(
                name: "UserId", table: "UserAttributes", newName: "DirectoryUserId");

            migrationBuilder.RenameTable(name: "UserRoles", newName: "DirectoryUserRoles");
            migrationBuilder.RenameTable(name: "UserAttributes", newName: "DirectoryUserAttributes");
            migrationBuilder.RenameTable(name: "Users", newName: "DirectoryUsers");
        }
    }
}
