using System.ComponentModel.DataAnnotations;

namespace CloudBooks.Web.Modelos;

public class ParametrosSistema
{
    public const decimal ImpuestoInicial = 0.12m;
    public const int UmbralInicial = 5;
    public const int VigenciaInicial = 15;
    public int Id { get; set; } = 1;
    [Range(typeof(decimal), "0", "1", ErrorMessage = "La tasa debe estar entre 0 y 1."), Display(Name = "Tasa de impuesto (0 a 1)")] public decimal TasaImpuesto { get; set; } = ImpuestoInicial;
    [Range(0, 1000000, ErrorMessage = "El umbral debe estar entre 0 y 1000000."), Display(Name = "Umbral de stock bajo")] public int UmbralStockBajo { get; set; } = UmbralInicial;
    [Range(1, 365, ErrorMessage = "La vigencia debe estar entre 1 y 365 días."), Display(Name = "Vigencia de cotizaciones en días")] public int DiasVigencia { get; set; } = VigenciaInicial;
    [Range(typeof(decimal), "0.01", "1000000000"), Display(Name = "Monto para revisión comercial (Q)")] public decimal MontoRevision { get; set; } = 10000m;
    [Range(1, 1000000), Display(Name = "Cantidad para revisión minorista")] public int CantidadRevisionMinorista { get; set; } = 30;
    [Range(1, 100), Display(Name = "Porcentaje para revisión de inventario")] public int PorcentajeRevisionInventario { get; set; } = 70;
}
