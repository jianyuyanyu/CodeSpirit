using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.PathfinderApi.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddGoalEvaluationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClarityScore",
                table: "Goals",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompletenessScore",
                table: "Goals",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExecutabilityScore",
                table: "Goals",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClarityScore",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "CompletenessScore",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "ExecutabilityScore",
                table: "Goals");
        }
    }
}
