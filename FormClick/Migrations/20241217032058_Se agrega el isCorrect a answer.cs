using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormClick.Migrations
{
    /// <inheritdoc />
    public partial class SeagregaelisCorrectaanswer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCorrect",
                table: "Answers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCorrect",
                table: "Answers");
        }
    }
}
