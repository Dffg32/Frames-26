using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JuegardoApi.Migrations
{
    /// <inheritdoc />
    public partial class AnadirPuntuaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Puntuaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Puntos = table.Column<int>(type: "INTEGER", nullable: false),
                    Nivel = table.Column<int>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Puntuaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Puntuaciones_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Puntuaciones_Puntos",
                table: "Puntuaciones",
                column: "Puntos");

            migrationBuilder.CreateIndex(
                name: "IX_Puntuaciones_UsuarioId",
                table: "Puntuaciones",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Puntuaciones");
        }
    }
}
