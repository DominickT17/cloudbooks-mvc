using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudBooks.Web.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class RevisionComercialInventario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TipoRevision",
                table: "Pedidos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CantidadRevisionMinorista",
                table: "ConfiguracionSistema",
                type: "int",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<int>(
                name: "PorcentajeRevisionInventario",
                table: "ConfiguracionSistema",
                type: "int",
                nullable: false,
                defaultValue: 70);

            migrationBuilder.UpdateData(
                table: "ConfiguracionSistema",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CantidadRevisionMinorista", "PorcentajeRevisionInventario" },
                values: new object[] { 30, 70 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TipoRevision",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "CantidadRevisionMinorista",
                table: "ConfiguracionSistema");

            migrationBuilder.DropColumn(
                name: "PorcentajeRevisionInventario",
                table: "ConfiguracionSistema");
        }
    }
}
