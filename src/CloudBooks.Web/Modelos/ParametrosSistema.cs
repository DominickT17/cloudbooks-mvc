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
}
