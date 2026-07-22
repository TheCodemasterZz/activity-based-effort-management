using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EforTakip.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectoryIdToAttributeMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_DirectoryAttributeMappings_DirectoryId_AdAttributeName",
                table: "DirectoryAttributeMappings",
                columns: new[] { "DirectoryId", "AdAttributeName" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DirectoryAttributeMappings_Directories_DirectoryId",
                table: "DirectoryAttributeMappings",
                column: "DirectoryId",
                principalTable: "Directories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DirectoryAttributeMappings_Directories_DirectoryId",
                table: "DirectoryAttributeMappings");

            migrationBuilder.DropIndex(
                name: "IX_DirectoryAttributeMappings_DirectoryId_AdAttributeName",
                table: "DirectoryAttributeMappings");
        }
    }
}
