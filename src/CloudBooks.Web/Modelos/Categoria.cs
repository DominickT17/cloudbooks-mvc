using System.ComponentModel.DataAnnotations;

namespace CloudBooks.Web.Modelos;

public class Categoria
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Nombre { get; set; } = string.Empty;
}
