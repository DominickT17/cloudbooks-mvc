using CloudBooks.Web.Servicios;

namespace CloudBooks.Pruebas;

public class SimuladorTarjetaPruebas
{
    [Theory]
    [InlineData("4000 0000 0000 0002", ResultadoTarjetaSimulada.Aprobado)]
    [InlineData("4000 0000 0000 0010", ResultadoTarjetaSimulada.FondosInsuficientes)]
    [InlineData("4000 0000 0000 0028", ResultadoTarjetaSimulada.Rechazado)]
    [InlineData("4000 0000 0000 0036", ResultadoTarjetaSimulada.Bloqueado)]
    [InlineData("1111 1111 1111 1111", ResultadoTarjetaSimulada.DatosInvalidos)]
    public void SoloAdmiteEscenariosFicticios(string numero, ResultadoTarjetaSimulada esperado)
    {
        var resultado = new SimuladorTarjeta().Evaluar(numero, "12/30", "123", new DateOnly(2026, 11, 1));
        Assert.Equal(esperado, resultado.Resultado);
        Assert.Equal(esperado == ResultadoTarjetaSimulada.DatosInvalidos ? null : numero[^4..], resultado.UltimosCuatro);
    }

    [Fact]
    public void FechaVencidaYDatosInvalidosNoSeAutorizan()
    {
        var simulador = new SimuladorTarjeta();
        Assert.Equal(ResultadoTarjetaSimulada.Vencido, simulador.Evaluar("4000000000000002", "10/26", "123", new DateOnly(2026, 11, 1)).Resultado);
        Assert.Equal(ResultadoTarjetaSimulada.DatosInvalidos, simulador.Evaluar("4000000000000002", "13/30", "123", new DateOnly(2026, 11, 1)).Resultado);
        Assert.Equal(ResultadoTarjetaSimulada.DatosInvalidos, simulador.Evaluar("4000000000000002", "12/30", "12", new DateOnly(2026, 11, 1)).Resultado);
    }
}
