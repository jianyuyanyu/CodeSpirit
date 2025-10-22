using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.IdentityApi.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class UpdateUserNameIndexWithTenantId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                table: "ApplicationUser");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUser_NormalizedUserName_TenantId",
                table: "ApplicationUser",
                columns: new[] { "NormalizedUserName", "TenantId" },
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ApplicationUser_NormalizedUserName_TenantId",
                table: "ApplicationUser");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "ApplicationUser",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");
        }
    }
}
