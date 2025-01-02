using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormClick.Migrations
{
    /// <inheritdoc />
    public partial class SeagregaelcampodeoriginalIdentemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OriginalId",
                table: "Templates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OriginalId",
                table: "TemplateHistorials",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginalId",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "OriginalId",
                table: "TemplateHistorials");
        }
    }
}
