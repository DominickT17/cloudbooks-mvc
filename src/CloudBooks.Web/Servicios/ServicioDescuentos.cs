namespace CloudBooks.Web.Servicios;

public record ResumenComercial(decimal Subtotal, decimal DescuentoVolumen, decimal DescuentoInstitucional, decimal Impuesto, decimal Total);

public class ServicioDescuentos
{
    public const decimal TasaImpuesto = 0.12m;

    public ResumenComercial Calcular(decimal subtotal, int cantidad, bool institucional)
    {
        if (subtotal < 0) throw new ArgumentOutOfRangeException(nameof(subtotal));
        if (cantidad < 1) throw new ArgumentOutOfRangeException(nameof(cantidad));

        decimal tasaVolumen = ObtenerTasaVolumen(cantidad);
        decimal descuentoVolumen = Redondear(subtotal * tasaVolumen);
        decimal descuentoInstitucional = institucional ? Redondear((subtotal - descuentoVolumen) * 0.08m) : 0m;
        decimal baseImponible = subtotal - descuentoVolumen - descuentoInstitucional;
        decimal impuesto = Redondear(baseImponible * TasaImpuesto);
        return new ResumenComercial(subtotal, descuentoVolumen, descuentoInstitucional, impuesto, baseImponible + impuesto);
    }

    private static decimal Redondear(decimal valor) => decimal.Round(valor, 2, MidpointRounding.AwayFromZero);

    public static decimal ObtenerTasaVolumen(int cantidad) => cantidad switch
    {
        < 1 => throw new ArgumentOutOfRangeException(nameof(cantidad)),
        <= 5 => 0m,
        <= 20 => 0.05m,
        <= 50 => 0.10m,
        _ => 0.15m
    };
}
