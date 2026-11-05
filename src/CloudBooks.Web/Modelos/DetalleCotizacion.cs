using System.ComponentModel.DataAnnotations;

namespace CloudBooks.Web.Modelos;

public class DetalleCotizacion
{
    public int Id { get; set; }
    public int CotizacionId { get; set; }
    public Cotizacion Cotizacion { get; set; } = null!;
    [Required, StringLength(200)] public string Concepto { get; set; } = "";
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}
