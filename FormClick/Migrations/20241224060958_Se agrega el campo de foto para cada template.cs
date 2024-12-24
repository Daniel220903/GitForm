using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormClick.Migrations
{
    /// <inheritdoc />
    public partial class Seagregaelcampodefotoparacadatemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "picture",
                table: "Templates",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "picture",
                table: "Templates");
        }
    }
}
