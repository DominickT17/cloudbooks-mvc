using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using CloudBooks.Web.ModelosVista;
using CloudBooks.Web.Servicios;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CloudBooks.Pruebas;

public class ServicioCotizacionesPruebas : IDisposable
{
    private readonly SqliteConnection conexion = new("Data Source=:memory:");
    private readonly ContextoAplicacion contexto;
    private readonly ServicioCotizaciones servicio;

    public ServicioCotizacionesPruebas()
    {
        conexion.Open();
        contexto = new ContextoAplicacion(new DbContextOptionsBuilder<ContextoAplicacion>().UseSqlite(conexion).Options);
        contexto.Database.EnsureCreated();
        var cliente = new UsuarioAplicacion { Id = "cliente", UserName = "cliente", NombreCompleto = "Cliente" };
        var asesor = new UsuarioAplicacion { Id = "asesor", UserName = "asesor", NombreCompleto = "Asesor" };
        var perfil = new PerfilInstitucional { Usuario = cliente, Institucion = "Institución", Nit = "123", TipoInstitucion = "Biblioteca", Telefono = "123", Direccion = "Dirección", Representante = "Cliente" };
        contexto.SolicitudesCotizacion.Add(new SolicitudCotizacion { Usuario = cliente, Asesor = asesor, PerfilInstitucional = perfil, Titulo = "Edición", Cantidad = 25, Acabado = "Tapa dura", Papel = "Bond", FechaRequerida = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30), FechaUtc = DateTime.UtcNow, Estado = EstadoSolicitudCotizacion.EnRevision });
        contexto.SaveChanges();
        contexto.ChangeTracker.Clear();
        servicio = new ServicioCotizaciones(contexto, new ServicioDescuentos());
    }

    private CotizacionModelo Datos() => new() { SolicitudId = 1, Concepto = "Edición", Cantidad = 25, PrecioUnitario = 100m };

    [Fact]
    public async Task EmisionCalculaTotalesYRechazaAsesorAjeno()
    {
        Assert.NotNull(await servicio.EmitirAsync("otro", Datos()));
        Assert.Null(await servicio.EmitirAsync("asesor", Datos()));
        var cotizacion = await contexto.Cotizaciones.AsNoTracking().SingleAsync();
        Assert.Equal(2500m, cotizacion.Subtotal);
        Assert.Equal(250m, cotizacion.DescuentoVolumen);
        Assert.Equal(180m, cotizacion.DescuentoInstitucional);
        Assert.Equal(248.40m, cotizacion.Impuesto);
        Assert.Equal(2318.40m, cotizacion.Total);
        Assert.NotNull(await servicio.EmitirAsync("asesor", Datos()));
    }

    [Fact]
    public async Task SoloPropietarioOAsesorAccedenYPropietarioAceptaUnaVez()
    {
        Assert.Null(await servicio.EmitirAsync("asesor", Datos()));
        Assert.Null(await servicio.ObtenerAutorizadaAsync(1, "otro", false));
        Assert.NotNull(await servicio.ObtenerAutorizadaAsync(1, "cliente", false));
        Assert.NotNull(await servicio.ObtenerAutorizadaAsync(1, "asesor", false));
        Assert.NotNull(await servicio.ResponderAsync(1, "otro", true));
        Assert.Null(await servicio.ResponderAsync(1, "cliente", true));
        Assert.NotNull(await servicio.ResponderAsync(1, "cliente", true));
        Assert.Equal(EstadoSolicitudCotizacion.Aceptada, (await contexto.SolicitudesCotizacion.AsNoTracking().SingleAsync()).Estado);
    }

    [Fact]
    public async Task VencidaNoPuedeAceptarse()
    {
        Assert.Null(await servicio.EmitirAsync("asesor", Datos()));
        await contexto.Cotizaciones.ExecuteUpdateAsync(c => c.SetProperty(x => x.FechaVencimiento, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1)));
        Assert.NotNull(await servicio.ResponderAsync(1, "cliente", true));
        Assert.Equal(EstadoSolicitudCotizacion.Vencida, (await contexto.Cotizaciones.AsNoTracking().SingleAsync()).Estado);
        Assert.Equal(EstadoSolicitudCotizacion.Vencida, (await contexto.SolicitudesCotizacion.AsNoTracking().SingleAsync()).Estado);
    }

    [Fact]
    public async Task ClientePuedeRechazar()
    {
        Assert.Null(await servicio.EmitirAsync("asesor", Datos()));
        Assert.Null(await servicio.ResponderAsync(1, "cliente", false));
        Assert.Equal(EstadoSolicitudCotizacion.Rechazada, (await contexto.Cotizaciones.AsNoTracking().SingleAsync()).Estado);
    }

    [Fact]
    public async Task CotizacionRespetaVigenciaYTasaConfiguradas()
    {
        var parametros = new ServicioParametros(contexto);
        Assert.Null(await parametros.GuardarAsync(new ParametrosSistema { TasaImpuesto = 0.16m, UmbralStockBajo = 5, DiasVigencia = 30 }));
        var cotizaciones = new ServicioCotizaciones(contexto, new ServicioDescuentos(), parametros);
        Assert.Null(await cotizaciones.EmitirAsync("asesor", Datos()));
        var cotizacion = await contexto.Cotizaciones.AsNoTracking().SingleAsync();
        Assert.Equal(0.16m, cotizacion.TasaImpuesto);
        Assert.Equal(331.20m, cotizacion.Impuesto);
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30), cotizacion.FechaVencimiento);
    }

    public void Dispose() { contexto.Dispose(); conexion.Dispose(); }
}
