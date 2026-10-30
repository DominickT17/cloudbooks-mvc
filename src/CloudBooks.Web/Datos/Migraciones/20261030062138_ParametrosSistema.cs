using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudBooks.Web.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class ParametrosSistema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TasaImpuesto",
                table: "Pedidos",
                type: "decimal(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0.12m);

            migrationBuilder.AddColumn<decimal>(
                name: "TasaImpuesto",
                table: "Cotizaciones",
                type: "decimal(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0.12m);

            migrationBuilder.CreateTable(
                name: "ConfiguracionSistema",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TasaImpuesto = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    UmbralStockBajo = table.Column<int>(type: "int", nullable: false),
                    DiasVigencia = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionSistema", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ConfiguracionSistema",
                columns: new[] { "Id", "DiasVigencia", "TasaImpuesto", "UmbralStockBajo" },
                values: new object[] { 1, 15, 0.12m, 5 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracionSistema");

            migrationBuilder.DropColumn(
                name: "TasaImpuesto",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "TasaImpuesto",
                table: "Cotizaciones");
        }
    }
}
