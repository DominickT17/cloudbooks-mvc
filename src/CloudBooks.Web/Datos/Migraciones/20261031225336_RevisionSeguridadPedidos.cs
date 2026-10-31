using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudBooks.Web.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class RevisionSeguridadPedidos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AltaPrioridad",
                table: "Pedidos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MotivosRevision",
                table: "Pedidos",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Revisado",
                table: "Pedidos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TitularTarjeta",
                table: "Pedidos",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoRevision",
                table: "ConfiguracionSistema",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 10000m);

            migrationBuilder.CreateTable(
                name: "NotificacionesPedido",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PedidoId = table.Column<int>(type: "int", nullable: false),
                    Destinatario = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Mensaje = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificacionesPedido", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificacionesPedido_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "ConfiguracionSistema",
                keyColumn: "Id",
                keyValue: 1,
                column: "MontoRevision",
                value: 10000m);

            migrationBuilder.CreateIndex(
                name: "IX_NotificacionesPedido_PedidoId",
                table: "NotificacionesPedido",
                column: "PedidoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificacionesPedido");

            migrationBuilder.DropColumn(
                name: "AltaPrioridad",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "MotivosRevision",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "Revisado",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "TitularTarjeta",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "MontoRevision",
                table: "ConfiguracionSistema");
        }
    }
}
