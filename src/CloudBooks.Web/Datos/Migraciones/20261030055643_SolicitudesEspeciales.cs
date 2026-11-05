using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudBooks.Web.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class SolicitudesEspeciales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "SolicitudesCotizacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PerfilInstitucionalId = table.Column<int>(type: "int", nullable: false),
                    AsesorId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Titulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    Acabado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Papel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FechaRequerida = table.Column<DateOnly>(type: "date", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FechaUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesCotizacion", x => x.Id);
                    table.CheckConstraint("CK_SolicitudCotizacion_Cantidad", "[Cantidad] > 0");
                    table.ForeignKey(
                        name: "FK_SolicitudesCotizacion_AspNetUsers_AsesorId",
                        column: x => x.AsesorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesCotizacion_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesCotizacion_PerfilesInstitucionales_PerfilInstitucionalId",
                        column: x => x.PerfilInstitucionalId,
                        principalTable: "PerfilesInstitucionales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCotizacion_AsesorId",
                table: "SolicitudesCotizacion",
                column: "AsesorId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCotizacion_PerfilInstitucionalId",
                table: "SolicitudesCotizacion",
                column: "PerfilInstitucionalId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCotizacion_UsuarioId",
                table: "SolicitudesCotizacion",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolicitudesCotizacion");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "AspNetUsers");
        }
    }
}
