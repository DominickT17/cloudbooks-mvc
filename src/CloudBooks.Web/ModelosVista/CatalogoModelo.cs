using CloudBooks.Web.Modelos;

namespace CloudBooks.Web.ModelosVista;

public class CatalogoModelo
{
    public string? Buscar { get; set; }
    public int? CategoriaId { get; set; }
    public FormatoLibro? Formato { get; set; }
    public decimal? PrecioMinimo { get; set; }
    public decimal? PrecioMaximo { get; set; }
    public string? Orden { get; set; }
    public List<Categoria> Categorias { get; set; } = [];
    public List<Libro> Libros { get; set; } = [];
}
