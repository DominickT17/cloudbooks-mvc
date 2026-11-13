using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudBooks.Web.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class ComprobanteCorreo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "ComprobantePdf",
                table: "CorreosPendientes",
                type: "varbinary(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ComprobantePdf",
                table: "CorreosPendientes");
        }
    }
}
