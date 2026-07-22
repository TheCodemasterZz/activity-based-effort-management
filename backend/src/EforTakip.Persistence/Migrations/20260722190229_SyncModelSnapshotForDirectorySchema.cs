using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EforTakip.Persistence.Migrations
{
    /// <summary>
    /// The Directories/DirectoryUsers/DirectoryAttributeMappings/DirectoryUserAttributes
    /// tables (and all of their indexes/foreign keys) were already created by the prior
    /// AddDirectoriesAndPendingSchemaChanges, AddDirectoryUserAttributes,
    /// AddDirectoryUserAttributeReference and WidenDirectoryUserAttributeValue migrations.
    /// A merge had left EforTakipDbContextModelSnapshot out of sync with those migrations
    /// (the snapshot did not reflect the Directory-related model at all), which made
    /// `dotnet ef migrations add` try to regenerate all of that schema from scratch. This
    /// migration exists only to bring the snapshot back in line with the already-applied
    /// schema, so it intentionally omits every operation that duplicates existing objects.
    ///
    /// The one genuinely new piece captured here is the `DirectoryAttributeMappings.DirectoryId`
    /// column added to the domain entity in a prior task: unlike `DirectoryUsers.DirectoryId`
    /// (present since the original migration), this column has never been created in the
    /// database, so it is added for real below.
    /// </summary>
    public partial class SyncModelSnapshotForDirectorySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DirectoryId",
                table: "DirectoryAttributeMappings",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DirectoryId",
                table: "DirectoryAttributeMappings");
        }
    }
}
