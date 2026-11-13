using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudBooks.Web.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class LogoCorreo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "LogoCorreo",
                table: "CorreosPendientes",
                type: "varbinary(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogoCorreo",
                table: "CorreosPendientes");
        }
    }
}
