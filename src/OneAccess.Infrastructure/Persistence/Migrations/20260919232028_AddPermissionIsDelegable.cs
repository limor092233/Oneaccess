using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneAccess.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPermissionIsDelegable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDelegable",
                table: "Permissions",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDelegable",
                table: "Permissions");
        }
    }
}
