using System.ComponentModel.DataAnnotations;

namespace CloudBooks.Web.ModelosVista;

public class CotizacionModelo
{
    public int SolicitudId { get; set; }
    [Required(ErrorMessage = "Ingresa el concepto."), StringLength(200)] public string Concepto { get; set; } = "";
    [Range(1, 10000, ErrorMessage = "La cantidad debe estar entre 1 y 10000.")] public int Cantidad { get; set; }
    [Range(typeof(decimal), "0.01", "1000000", ErrorMessage = "Ingresa un precio unitario válido."), Display(Name = "Precio unitario (Q)")] public decimal PrecioUnitario { get; set; }
}
