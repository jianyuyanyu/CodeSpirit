using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.FileStorageApi.Migrations
{
    /// <inheritdoc />
    public partial class RemoveThumbnailTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Thumbnails");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Thumbnails",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    ImageMetadataId = table.Column<long>(type: "bigint", nullable: false),
                    ThumbnailFileId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    Height = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    SizeKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    Width = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Thumbnails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Thumbnails_Files_ThumbnailFileId",
                        column: x => x.ThumbnailFileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Thumbnails_ImageMetadata_ImageMetadataId",
                        column: x => x.ImageMetadataId,
                        principalTable: "ImageMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Thumbnails_ImageMetadataId_SizeKey",
                table: "Thumbnails",
                columns: new[] { "ImageMetadataId", "SizeKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Thumbnails_SizeKey",
                table: "Thumbnails",
                column: "SizeKey");

            migrationBuilder.CreateIndex(
                name: "IX_Thumbnails_ThumbnailFileId",
                table: "Thumbnails",
                column: "ThumbnailFileId");
        }
    }
}
