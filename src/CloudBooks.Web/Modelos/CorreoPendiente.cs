using System.ComponentModel.DataAnnotations;

namespace CloudBooks.Web.Modelos;

public enum EstadoCorreo { Pendiente, EnProceso, AceptadoProveedor, Simulado, ReintentoPendiente, Fallido }

public class CorreoPendiente
{
    public int Id { get; set; }
    public Guid ClaveEnvio { get; set; } = Guid.NewGuid();
    [Required, MaxLength(200)] public string ClaveEvento { get; set; } = "";
    [Required, MaxLength(254)] public string Destinatario { get; set; } = "";
    [Required, MaxLength(200)] public string Asunto { get; set; } = "";
    [Required] public string Html { get; set; } = "";
    public string? TextoPlano { get; set; }
    [Required, MaxLength(80)] public string Tipo { get; set; } = "";
    [MaxLength(100)] public string? Referencia { get; set; }
    public EstadoCorreo Estado { get; set; }
    public DateTime CreadoUtc { get; set; }
    public DateTime? PrimerIntentoUtc { get; set; }
    public DateTime? UltimoIntentoUtc { get; set; }
    public DateTime? ProximoIntentoUtc { get; set; }
    public DateTime? BloqueadoHastaUtc { get; set; }
    public Guid? Bloqueo { get; set; }
    public int Intentos { get; set; }
    [MaxLength(200)] public string? Error { get; set; }
    [MaxLength(100)] public string? IdProveedor { get; set; }
    [MaxLength(30)] public string? Proveedor { get; set; }
}
