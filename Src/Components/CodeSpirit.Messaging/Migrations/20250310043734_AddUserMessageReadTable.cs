using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.Messaging.Migrations
{
    /// <inheritdoc />
    public partial class AddUserMessageReadTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserMessageReads",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMessageReads", x => new { x.UserId, x.MessageId });
                    table.ForeignKey(
                        name: "FK_UserMessageReads_Messages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserMessageReads_MessageId",
                table: "UserMessageReads",
                column: "MessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserMessageReads");
        }
    }
}
