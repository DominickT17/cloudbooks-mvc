using System.ComponentModel.DataAnnotations;

namespace CloudBooks.Web.Modelos;

public class IntentoPago
{
    public int Id { get; set; }
    [Required] public string UsuarioId { get; set; } = "";
    public UsuarioAplicacion Usuario { get; set; } = null!;
    public DateTime FechaUtc { get; set; }
    public ResultadoPago Resultado { get; set; }
    [StringLength(4)] public string? UltimosCuatro { get; set; }
}
