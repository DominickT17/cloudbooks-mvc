using CloudBooks.Web.Modelos;
namespace CloudBooks.Web.ModelosVista;
public class InicioModelo
{
    public List<Libro> Libros { get; set; } = [];
    public bool SonMasVendidos { get; set; }
    public List<Libro> Recomendados { get; set; } = [];
    public List<Libro> Recientes { get; set; } = [];
    public List<Categoria> Categorias { get; set; } = [];
}
