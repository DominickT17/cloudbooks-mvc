using System.ComponentModel.DataAnnotations;

namespace CloudBooks.Web.Modelos;

public class DetallePedido
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public Pedido Pedido { get; set; } = null!;
    public int LibroId { get; set; }
    public Libro Libro { get; set; } = null!;
    [Required, StringLength(200)] public string Titulo { get; set; } = "";
    public FormatoLibro Formato { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}
