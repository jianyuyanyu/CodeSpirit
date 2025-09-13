using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.FileStorageApi.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Files",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BucketName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    StorageFileName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FileHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Extension = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AccessCount = table.Column<long>(type: "bigint", nullable: false),
                    LastAccessTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpirationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    DownloadUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    ETag = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Properties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Files", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileReferences",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FileId = table.Column<long>(type: "bigint", nullable: false),
                    SourceService = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SourceEntityType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SourceEntityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ReferenceType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsTemporary = table.Column<bool>(type: "bit", nullable: false),
                    ExpirationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConfirmedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Properties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileReferences_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImageMetadata",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FileId = table.Column<long>(type: "bigint", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    ColorDepth = table.Column<int>(type: "int", nullable: false),
                    Format = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    HasAlpha = table.Column<bool>(type: "bit", nullable: false),
                    IsAnimated = table.Column<bool>(type: "bit", nullable: false),
                    FrameCount = table.Column<int>(type: "int", nullable: false),
                    DpiX = table.Column<double>(type: "float", nullable: false),
                    DpiY = table.Column<double>(type: "float", nullable: false),
                    CameraModel = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DateTaken = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    ExifData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ColorPalette = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImageMetadata_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VideoMetadata",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    FileId = table.Column<long>(type: "bigint", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    Duration = table.Column<double>(type: "float", nullable: false),
                    Bitrate = table.Column<long>(type: "bigint", nullable: false),
                    FrameRate = table.Column<double>(type: "float", nullable: false),
                    VideoCodec = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AudioCodec = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Container = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    HasAudio = table.Column<bool>(type: "bit", nullable: false),
                    HasVideo = table.Column<bool>(type: "bit", nullable: false),
                    AudioSampleRate = table.Column<int>(type: "int", nullable: false),
                    AudioChannels = table.Column<int>(type: "int", nullable: false),
                    ThumbnailTimePosition = table.Column<double>(type: "float", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MetadataInfo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoMetadata_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileReferences_ExpirationTime",
                table: "FileReferences",
                column: "ExpirationTime");

            migrationBuilder.CreateIndex(
                name: "IX_FileReferences_FileId",
                table: "FileReferences",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_FileReferences_Source",
                table: "FileReferences",
                columns: new[] { "SourceService", "SourceEntityType", "SourceEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_FileReferences_Status",
                table: "FileReferences",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FileReferences_TenantId_FileId",
                table: "FileReferences",
                columns: new[] { "TenantId", "FileId" });

            migrationBuilder.CreateIndex(
                name: "IX_FileReferences_TenantId_IsTemporary_Status",
                table: "FileReferences",
                columns: new[] { "TenantId", "IsTemporary", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FileReferences_TenantId_Status",
                table: "FileReferences",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Files_Category",
                table: "Files",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Files_CreatedAt",
                table: "Files",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Files_ExpirationTime",
                table: "Files",
                column: "ExpirationTime");

            migrationBuilder.CreateIndex(
                name: "IX_Files_FileHash",
                table: "Files",
                column: "FileHash");

            migrationBuilder.CreateIndex(
                name: "IX_Files_Status",
                table: "Files",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Files_TenantId_BucketName",
                table: "Files",
                columns: new[] { "TenantId", "BucketName" });

            migrationBuilder.CreateIndex(
                name: "IX_Files_TenantId_Category",
                table: "Files",
                columns: new[] { "TenantId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_Files_TenantId_OriginalFileName",
                table: "Files",
                columns: new[] { "TenantId", "OriginalFileName" });

            migrationBuilder.CreateIndex(
                name: "IX_Files_TenantId_StorageFileName",
                table: "Files",
                columns: new[] { "TenantId", "StorageFileName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImageMetadata_DateTaken",
                table: "ImageMetadata",
                column: "DateTaken");

            migrationBuilder.CreateIndex(
                name: "IX_ImageMetadata_Dimensions",
                table: "ImageMetadata",
                columns: new[] { "Width", "Height" });

            migrationBuilder.CreateIndex(
                name: "IX_ImageMetadata_FileId",
                table: "ImageMetadata",
                column: "FileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImageMetadata_Format",
                table: "ImageMetadata",
                column: "Format");

            migrationBuilder.CreateIndex(
                name: "IX_ImageMetadata_GpsLocation",
                table: "ImageMetadata",
                columns: new[] { "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_VideoMetadata_CreatedTime",
                table: "VideoMetadata",
                column: "CreatedTime");

            migrationBuilder.CreateIndex(
                name: "IX_VideoMetadata_Dimensions",
                table: "VideoMetadata",
                columns: new[] { "Width", "Height" });

            migrationBuilder.CreateIndex(
                name: "IX_VideoMetadata_Duration",
                table: "VideoMetadata",
                column: "Duration");

            migrationBuilder.CreateIndex(
                name: "IX_VideoMetadata_FileId",
                table: "VideoMetadata",
                column: "FileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VideoMetadata_VideoCodec",
                table: "VideoMetadata",
                column: "VideoCodec");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileReferences");

            migrationBuilder.DropTable(
                name: "ImageMetadata");

            migrationBuilder.DropTable(
                name: "VideoMetadata");

            migrationBuilder.DropTable(
                name: "Files");
        }
    }
}
