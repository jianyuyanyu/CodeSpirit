using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.IdentityApi.Migrations.MySql
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
                unique: true);
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
                unique: true);
        }
    }
}
