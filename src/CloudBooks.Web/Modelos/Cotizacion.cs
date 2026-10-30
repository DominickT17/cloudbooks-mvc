using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudBooks.Web.Modelos;

public class Cotizacion
{
    public int Id { get; set; }
    [NotMapped] public string Codigo => $"COT-{Id:000000}";
    public int SolicitudCotizacionId { get; set; }
    public SolicitudCotizacion Solicitud { get; set; } = null!;
    public DateTime FechaUtc { get; set; }
    public DateOnly FechaVencimiento { get; set; }
    public EstadoSolicitudCotizacion Estado { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DescuentoVolumen { get; set; }
    public decimal DescuentoInstitucional { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Total { get; set; }
    public List<DetalleCotizacion> Detalles { get; set; } = [];
}
