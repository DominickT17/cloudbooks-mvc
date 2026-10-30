namespace CloudBooks.Web.Modelos;

public class DetalleCarrito
{
    public int Id { get; set; }
    public int CarritoId { get; set; }
    public Carrito Carrito { get; set; } = null!;
    public int LibroId { get; set; }
    public Libro Libro { get; set; } = null!;
    public int Cantidad { get; set; }
}
