using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.IdentityApi.Migrations
{
    /// <inheritdoc />
    public partial class AddCascadeDeleteToLoginLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LoginLogs_ApplicationUser_UserId",
                table: "LoginLogs");

            migrationBuilder.AddForeignKey(
                name: "FK_LoginLogs_ApplicationUser_UserId",
                table: "LoginLogs",
                column: "UserId",
                principalTable: "ApplicationUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LoginLogs_ApplicationUser_UserId",
                table: "LoginLogs");

            migrationBuilder.AddForeignKey(
                name: "FK_LoginLogs_ApplicationUser_UserId",
                table: "LoginLogs",
                column: "UserId",
                principalTable: "ApplicationUser",
                principalColumn: "Id");
        }
    }
}
