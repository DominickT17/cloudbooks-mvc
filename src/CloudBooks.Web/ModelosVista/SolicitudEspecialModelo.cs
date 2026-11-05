using System.ComponentModel.DataAnnotations;

namespace CloudBooks.Web.ModelosVista;

public class SolicitudEspecialModelo
{
    [Required(ErrorMessage = "Ingresa el título u obra."), StringLength(200), Display(Name = "Título u obra")] public string Titulo { get; set; } = "";
    [Range(1, 10000, ErrorMessage = "La cantidad debe estar entre 1 y 10000."), Display(Name = "Cantidad estimada")] public int Cantidad { get; set; } = 1;
    [Required(ErrorMessage = "Selecciona el acabado.")] public string Acabado { get; set; } = "";
    [Required(ErrorMessage = "Selecciona el papel.")] public string Papel { get; set; } = "";
    [Required(ErrorMessage = "Selecciona la fecha requerida."), Display(Name = "Fecha requerida")] public DateOnly? FechaRequerida { get; set; }
    [StringLength(1000)] public string? Observaciones { get; set; }
}
