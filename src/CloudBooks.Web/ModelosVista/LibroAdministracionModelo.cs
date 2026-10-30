using System.ComponentModel.DataAnnotations;
using CloudBooks.Web.Modelos;

namespace CloudBooks.Web.ModelosVista;

public class LibroAdministracionModelo
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Ingresa el ISBN."), StringLength(20), RegularExpression(@"[0-9Xx-]+", ErrorMessage = "El ISBN solo puede contener dígitos, guiones o X.")] public string ISBN { get; set; } = "";
    [Required(ErrorMessage = "Ingresa el título."), StringLength(200), Display(Name = "Título")] public string Titulo { get; set; } = "";
    [Required(ErrorMessage = "Ingresa la sinopsis."), StringLength(4000)] public string Sinopsis { get; set; } = "";
    [Range(1450, 2100, ErrorMessage = "Ingresa un año válido."), Display(Name = "Año de edición")] public int AnioEdicion { get; set; }
    [Range(typeof(decimal), "0.01", "1000000", ErrorMessage = "Ingresa un precio válido."), Display(Name = "Precio base (Q)")] public decimal PrecioBase { get; set; }
    [Range(0, 1000000, ErrorMessage = "El stock no puede ser negativo.")] public int Stock { get; set; }
    public int StockOriginal { get; set; }
    [Required(ErrorMessage = "Selecciona una portada."), Display(Name = "Portada local")] public string ImagenPortada { get; set; } = "";
    public FormatoLibro Formato { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una editorial."), Display(Name = "Editorial")] public int EditorialId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una categoría."), Display(Name = "Categoría")] public int CategoriaId { get; set; }
    [MinLength(1, ErrorMessage = "Selecciona al menos un autor."), Display(Name = "Autores")] public int[] AutoresIds { get; set; } = [];
    public List<Autor> Autores { get; set; } = [];
    public List<Categoria> Categorias { get; set; } = [];
    public List<Editorial> Editoriales { get; set; } = [];
    public List<string> Portadas { get; set; } = [];
}
