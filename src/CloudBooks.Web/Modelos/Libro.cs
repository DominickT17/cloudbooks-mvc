using System.ComponentModel.DataAnnotations;

namespace CloudBooks.Web.Modelos;

public class Libro
{
    public int Id { get; set; }
    [Required, StringLength(20)] public string ISBN { get; set; } = string.Empty;
    [Required, StringLength(200)] public string Titulo { get; set; } = string.Empty;
    [Required] public string Sinopsis { get; set; } = string.Empty;
    public int AnioEdicion { get; set; }
    public decimal PrecioBase { get; set; }
    public int Stock { get; set; }
    [StringLength(300)] public string ImagenPortada { get; set; } = string.Empty;
    public FormatoLibro Formato { get; set; }
    public bool Activo { get; set; } = true;
    public int EditorialId { get; set; }
    public Editorial Editorial { get; set; } = null!;
    public int CategoriaId { get; set; }
    public Categoria Categoria { get; set; } = null!;
    public List<Autor> Autores { get; set; } = [];
}

public enum FormatoLibro { TapaBlanda, TapaDura, Ebook }
