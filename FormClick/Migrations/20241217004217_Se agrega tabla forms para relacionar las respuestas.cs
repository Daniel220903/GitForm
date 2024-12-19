using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormClick.Migrations
{
    /// <inheritdoc />
    public partial class Seagregatablaformspararelacionarlasrespuestas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ResponseId",
                table: "Answers",
                nullable: false,
                defaultValue: 0); // Asigna un valor por defecto si es requerido
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResponseId",
                table: "Answer");
        }
    }
}
