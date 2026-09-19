using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneAccess.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJoinTableUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UserSubSystemAccesses_UserId_SubSystemId",
                table: "UserSubSystemAccesses",
                columns: new[] { "UserId", "SubSystemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId_RoleId",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserDivisionAssignments_UserId_DivisionId",
                table: "UserDivisionAssignments",
                columns: new[] { "UserId", "DivisionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleSubSystemAccesses_RoleId_SubSystemId",
                table: "RoleSubSystemAccesses",
                columns: new[] { "RoleId", "SubSystemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId_PermissionId",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserSubSystemAccesses_UserId_SubSystemId",
                table: "UserSubSystemAccesses");

            migrationBuilder.DropIndex(
                name: "IX_UserRoles_UserId_RoleId",
                table: "UserRoles");

            migrationBuilder.DropIndex(
                name: "IX_UserDivisionAssignments_UserId_DivisionId",
                table: "UserDivisionAssignments");

            migrationBuilder.DropIndex(
                name: "IX_RoleSubSystemAccesses_RoleId_SubSystemId",
                table: "RoleSubSystemAccesses");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_RoleId_PermissionId",
                table: "RolePermissions");
        }
    }
}
