using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.IdentityApi.Migrations.MySql
{
    /// <inheritdoc />
    public partial class ConfigureApiKeyManualId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApiKeys_ApplicationUser_UserId",
                table: "ApiKeys");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ApiKeys",
                table: "ApiKeys");

            migrationBuilder.RenameTable(
                name: "ApiKeys",
                newName: "ApiKey");

            migrationBuilder.RenameIndex(
                name: "IX_ApiKeys_UserId",
                table: "ApiKey",
                newName: "IX_ApiKey_UserId");

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "ApiKey",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ApiKey",
                table: "ApiKey",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKey_ExpiresAt",
                table: "ApiKey",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKey_IsActive",
                table: "ApiKey",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKey_KeyHash",
                table: "ApiKey",
                column: "KeyHash");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKey_TenantId_UserId",
                table: "ApiKey",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ApiKey_ApplicationUser_UserId",
                table: "ApiKey",
                column: "UserId",
                principalTable: "ApplicationUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApiKey_ApplicationUser_UserId",
                table: "ApiKey");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ApiKey",
                table: "ApiKey");

            migrationBuilder.DropIndex(
                name: "IX_ApiKey_ExpiresAt",
                table: "ApiKey");

            migrationBuilder.DropIndex(
                name: "IX_ApiKey_IsActive",
                table: "ApiKey");

            migrationBuilder.DropIndex(
                name: "IX_ApiKey_KeyHash",
                table: "ApiKey");

            migrationBuilder.DropIndex(
                name: "IX_ApiKey_TenantId_UserId",
                table: "ApiKey");

            migrationBuilder.RenameTable(
                name: "ApiKey",
                newName: "ApiKeys");

            migrationBuilder.RenameIndex(
                name: "IX_ApiKey_UserId",
                table: "ApiKeys",
                newName: "IX_ApiKeys_UserId");

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "ApiKeys",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ApiKeys",
                table: "ApiKeys",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ApiKeys_ApplicationUser_UserId",
                table: "ApiKeys",
                column: "UserId",
                principalTable: "ApplicationUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
