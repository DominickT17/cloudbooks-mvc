using System.ComponentModel.DataAnnotations;

namespace CloudBooks.Web.Modelos;

public class Carrito
{
    public int Id { get; set; }
    [Required] public string UsuarioId { get; set; } = "";
    public UsuarioAplicacion Usuario { get; set; } = null!;
    public List<DetalleCarrito> Detalles { get; set; } = [];
}
