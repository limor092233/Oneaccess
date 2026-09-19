using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneAccess.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubSystemAudienceUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SubSystems_Audience",
                table: "SubSystems",
                column: "Audience",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubSystems_Audience",
                table: "SubSystems");
        }
    }
}
