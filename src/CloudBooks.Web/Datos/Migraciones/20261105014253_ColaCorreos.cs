using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudBooks.Web.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class ColaCorreos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Leida",
                table: "NotificacionesPedido",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CorreosPendientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClaveEnvio = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaveEvento = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Destinatario = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    Asunto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Html = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Referencia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    CreadoUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PrimerIntentoUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UltimoIntentoUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProximoIntentoUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BloqueadoHastaUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Bloqueo = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Intentos = table.Column<int>(type: "int", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IdProveedor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Proveedor = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorreosPendientes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CorreosPendientes_ClaveEvento",
                table: "CorreosPendientes",
                column: "ClaveEvento",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CorreosPendientes_Estado_ProximoIntentoUtc",
                table: "CorreosPendientes",
                columns: new[] { "Estado", "ProximoIntentoUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CorreosPendientes");

            migrationBuilder.DropColumn(
                name: "Leida",
                table: "NotificacionesPedido");
        }
    }
}
