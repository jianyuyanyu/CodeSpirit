using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.FileStorageApi.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantIdToImageMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "ImageMetadata",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ImageMetadata");
        }
    }
}
