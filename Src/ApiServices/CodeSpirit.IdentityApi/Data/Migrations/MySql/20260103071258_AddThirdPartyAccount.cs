using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.IdentityApi.Data.Migrations.MySql
{
    /// <inheritdoc />
    public partial class AddThirdPartyAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ThirdPartyAccount",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    PlatformType = table.Column<int>(type: "int", nullable: false),
                    OpenId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UnionId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SessionKey = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastLoginTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsPrimary = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThirdPartyAccount", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThirdPartyAccount_ApplicationUser_UserId",
                        column: x => x.UserId,
                        principalTable: "ApplicationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ThirdPartyAccount_TenantId_PlatformType_OpenId",
                table: "ThirdPartyAccount",
                columns: new[] { "TenantId", "PlatformType", "OpenId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThirdPartyAccount_TenantId_UnionId",
                table: "ThirdPartyAccount",
                columns: new[] { "TenantId", "UnionId" },
                unique: true,
                filter: "[UnionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ThirdPartyAccount_UserId",
                table: "ThirdPartyAccount",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ThirdPartyAccount");
        }
    }
}
