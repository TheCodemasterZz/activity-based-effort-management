using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EforTakip.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectoryUserAttributes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncedUtc",
                table: "Directories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DirectoryUserAttributes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DirectoryUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttributeMappingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DirectoryUserAttributes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DirectoryUserAttributes_DirectoryAttributeMappings_Attribut~",
                        column: x => x.AttributeMappingId,
                        principalTable: "DirectoryAttributeMappings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DirectoryUserAttributes_DirectoryUsers_DirectoryUserId",
                        column: x => x.DirectoryUserId,
                        principalTable: "DirectoryUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DirectoryUserAttributes_AttributeMappingId",
                table: "DirectoryUserAttributes",
                column: "AttributeMappingId");

            migrationBuilder.CreateIndex(
                name: "IX_DirectoryUserAttributes_DirectoryUserId_AttributeMappingId",
                table: "DirectoryUserAttributes",
                columns: new[] { "DirectoryUserId", "AttributeMappingId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DirectoryUserAttributes");

            migrationBuilder.DropColumn(
                name: "LastSyncedUtc",
                table: "Directories");
        }
    }
}
