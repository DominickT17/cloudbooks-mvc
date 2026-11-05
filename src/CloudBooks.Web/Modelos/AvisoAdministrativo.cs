using System.ComponentModel.DataAnnotations;

namespace CloudBooks.Web.Modelos;

public class AvisoAdministrativo
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string ClaveEvento { get; set; } = "";
    [Required, MaxLength(200)] public string Titulo { get; set; } = "";
    [Required, MaxLength(500)] public string Mensaje { get; set; } = "";
    [MaxLength(100)] public string? Referencia { get; set; }
    public DateTime FechaUtc { get; set; }
    public bool Leido { get; set; }
}
