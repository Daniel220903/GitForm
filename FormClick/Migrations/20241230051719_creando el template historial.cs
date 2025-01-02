using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormClick.Migrations
{
    public partial class creandoeltemplatehistorial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TemplateHistorials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Topic = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Picture = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Public = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TemplateId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateHistorials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateHistorials_Templates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "Templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction); // Cambiado a NoAction
                    table.ForeignKey(
                        name: "FK_TemplateHistorials_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction); // Cambiado a NoAction
                });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateHistorials_TemplateId",
                table: "TemplateHistorials",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateHistorials_UserId",
                table: "TemplateHistorials",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TemplateHistorials"); // Corregido para eliminar la tabla correcta
        }
    }
}
