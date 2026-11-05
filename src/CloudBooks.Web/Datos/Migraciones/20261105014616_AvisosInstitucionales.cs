using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudBooks.Web.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class AvisosInstitucionales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AvisosAdministrativos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClaveEvento = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Mensaje = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Referencia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FechaUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Leido = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvisosAdministrativos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AvisosAdministrativos_ClaveEvento",
                table: "AvisosAdministrativos",
                column: "ClaveEvento",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AvisosAdministrativos");
        }
    }
}
