using System.ComponentModel.DataAnnotations;

namespace CloudBooks.Web.Modelos;

public class Autor
{
    public int Id { get; set; }
    [Required, StringLength(150)] public string Nombre { get; set; } = string.Empty;
    public List<Libro> Libros { get; set; } = [];
}
