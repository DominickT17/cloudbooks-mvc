using CloudBooks.Web.Servicios;

namespace CloudBooks.Pruebas;

public class ServicioDescuentosPruebas
{
    private readonly ServicioDescuentos servicio = new();

    [Theory]
    [InlineData(1, 0)]
    [InlineData(5, 0)]
    [InlineData(6, 5)]
    [InlineData(20, 5)]
    [InlineData(21, 10)]
    [InlineData(50, 10)]
    [InlineData(51, 15)]
    public void AplicaDescuentoPorVolumen(int cantidad, int descuento)
    {
        var resultado = servicio.Calcular(100m, cantidad, false);
        Assert.Equal(descuento, resultado.DescuentoVolumen);
        Assert.Equal(0m, resultado.DescuentoInstitucional);
    }

    [Fact]
    public void AplicaInstitucionalDespuesDeVolumenYCalculaImpuesto()
    {
        var resultado = servicio.Calcular(1000m, 21, true);
        Assert.Equal(100m, resultado.DescuentoVolumen);
        Assert.Equal(72m, resultado.DescuentoInstitucional);
        Assert.Equal(99.36m, resultado.Impuesto);
        Assert.Equal(927.36m, resultado.Total);
    }

    [Fact]
    public void RechazaCantidadInvalida() => Assert.Throws<ArgumentOutOfRangeException>(() => servicio.Calcular(100m, 0, false));

    [Fact]
    public void RechazaSubtotalNegativo() => Assert.Throws<ArgumentOutOfRangeException>(() => servicio.Calcular(-1m, 1, false));
}
