using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudBooks.Web.Modelos;

public enum EstadoSolicitudCotizacion { Solicitada, EnRevision, Cotizada, Aceptada, Rechazada, Vencida }

public class SolicitudCotizacion
{
    public int Id { get; set; }
    [NotMapped] public string Codigo => $"SOL-{Id:000000}";
    [Required] public string UsuarioId { get; set; } = "";
    public UsuarioAplicacion Usuario { get; set; } = null!;
    [Required] public int PerfilInstitucionalId { get; set; }
    public PerfilInstitucional PerfilInstitucional { get; set; } = null!;
    public string? AsesorId { get; set; }
    public UsuarioAplicacion? Asesor { get; set; }
    [Required, StringLength(200)] public string Titulo { get; set; } = "";
    public int Cantidad { get; set; }
    [Required, StringLength(50)] public string Acabado { get; set; } = "";
    [Required, StringLength(50)] public string Papel { get; set; } = "";
    public DateOnly FechaRequerida { get; set; }
    [StringLength(1000)] public string? Observaciones { get; set; }
    public DateTime FechaUtc { get; set; }
    public EstadoSolicitudCotizacion Estado { get; set; }
}
