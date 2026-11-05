using CloudBooks.Web.ModelosVista;

namespace CloudBooks.Pruebas;

public class FechaPresentacionPruebas
{
    [Fact]
    public void ConvierteElInstanteUtcALaFechaDeGuatemala()
    {
        var utc = new DateTime(2026, 11, 5, 3, 15, 0, DateTimeKind.Utc);
        var local = FechaPresentacion.EnGuatemala(utc);
        Assert.Equal(new DateTime(2026, 11, 4, 21, 15, 0), local);
    }
}
