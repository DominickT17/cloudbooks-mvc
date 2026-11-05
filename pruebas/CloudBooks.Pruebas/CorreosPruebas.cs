using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using CloudBooks.Web.Servicios;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Resend;
using System.Net;

namespace CloudBooks.Pruebas;

public class CorreosPruebas : IDisposable
{
    private readonly SqliteConnection conexion = new("Data Source=:memory:");
    private readonly ContextoAplicacion contexto;
    private readonly ServicioNotificaciones notificaciones;
    public CorreosPruebas()
    {
        conexion.Open();
        contexto = new ContextoAplicacion(new DbContextOptionsBuilder<ContextoAplicacion>().UseSqlite(conexion).Options);
        contexto.Database.EnsureCreated();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Correo:UrlBase"] = "https://cloudbooks.example" }).Build();
        notificaciones = new ServicioNotificaciones(contexto, config);
    }

    [Fact]
    public async Task SimuladoEsPersistenteYNoSeVuelveAProcesar()
    {
        await notificaciones.EncolarAsync("evento/1", "prueba", "Pedido 1", "cliente@example.test", "Pedido", "Hola <cliente>");
        await contexto.SaveChangesAsync();
        contexto.ChangeTracker.Clear();
        var procesador = new ProcesadorCorreos(contexto, new ProveedorCorreoSimulado());
        Assert.True(await procesador.ProcesarUnoAsync());
        Assert.False(await procesador.ProcesarUnoAsync());
        var correo = await contexto.CorreosPendientes.AsNoTracking().SingleAsync();
        Assert.Equal(EstadoCorreo.Simulado, correo.Estado);
        Assert.Equal(1, correo.Intentos);
        Assert.Contains("&lt;cliente&gt;", correo.Html);
        Assert.Null(correo.IdProveedor);
    }

    [Fact]
    public async Task FalloSeReintentaYNoDuplicaEvento()
    {
        await notificaciones.EncolarAsync("evento/2", "prueba", null, "cliente@example.test", "Pedido", "Contenido");
        await contexto.SaveChangesAsync();
        contexto.ChangeTracker.Clear();
        await notificaciones.EncolarAsync("evento/2", "prueba", null, "cliente@example.test", "Pedido", "Contenido");
        await contexto.SaveChangesAsync();
        var proveedor = new ProveedorDePrueba();
        var procesador = new ProcesadorCorreos(contexto, proveedor);
        Assert.True(await procesador.ProcesarUnoAsync());
        var fallo = await contexto.CorreosPendientes.AsNoTracking().SingleAsync();
        Assert.Equal(EstadoCorreo.ReintentoPendiente, fallo.Estado);
        Assert.Equal(1, fallo.Intentos);
        await contexto.CorreosPendientes.ExecuteUpdateAsync(s => s.SetProperty(c => c.ProximoIntentoUtc, DateTime.UtcNow.AddMinutes(-1)));
        proveedor.Fallar = false;
        Assert.True(await procesador.ProcesarUnoAsync());
        var aceptado = await contexto.CorreosPendientes.AsNoTracking().SingleAsync();
        Assert.Equal(EstadoCorreo.AceptadoProveedor, aceptado.Estado);
        Assert.Equal(2, aceptado.Intentos);
        Assert.Equal("id-prueba", aceptado.IdProveedor);
    }

    [Fact]
    public async Task RecuperaBloqueoVencidoYDetieneResultadoInciertoAntiguo()
    {
        contexto.CorreosPendientes.Add(new CorreoPendiente { ClaveEvento = "evento/3", Tipo = "prueba", Destinatario = "cliente@example.test", Asunto = "Prueba", Html = "Contenido", Estado = EstadoCorreo.EnProceso, CreadoUtc = DateTime.UtcNow.AddDays(-2), PrimerIntentoUtc = DateTime.UtcNow.AddDays(-2), BloqueadoHastaUtc = DateTime.UtcNow.AddMinutes(-1), Bloqueo = Guid.NewGuid() });
        await contexto.SaveChangesAsync();
        var proveedor = new ProveedorDePrueba { Fallar = false };
        Assert.True(await new ProcesadorCorreos(contexto, proveedor).ProcesarUnoAsync());
        Assert.Equal(EstadoCorreo.Fallido, (await contexto.CorreosPendientes.AsNoTracking().SingleAsync()).Estado);
        Assert.Equal(0, proveedor.Llamadas);
    }

    [Fact]
    public async Task ResendUsaClavePersistenteYRegistraIdDeRespuestaSinRed()
    {
        var manejador = new ManejadorResend();
        var cliente = ResendClient.Create(new ResendClientOptions { ApiToken = "clave-prueba" }, new HttpClient(manejador));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Correo:Remitente"] = "CloudBooks <demo@example.test>" }).Build();
        var correo = new CorreoPendiente { ClaveEnvio = Guid.NewGuid(), Destinatario = "cliente@example.test", Asunto = "Prueba", Html = "<p>Hola</p>" };
        var resultado = await new ProveedorCorreoResend(cliente, config).EnviarAsync(correo, CancellationToken.None);
        Assert.False(resultado.Simulado);
        Assert.Equal("b59fbb1f-b839-4aef-9c9a-6680ba344291", resultado.IdProveedor);
        Assert.Equal($"cloudbooks/{correo.ClaveEnvio:N}", manejador.Clave);
    }

    private class ManejadorResend : HttpMessageHandler
    {
        public string? Clave { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage solicitud, CancellationToken cancelacion)
        {
            Clave = solicitud.Headers.GetValues("Idempotency-Key").Single();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"id\":\"b59fbb1f-b839-4aef-9c9a-6680ba344291\"}", System.Text.Encoding.UTF8, "application/json") });
        }
    }

    private class ProveedorDePrueba : IProveedorCorreo
    {
        public bool Fallar { get; set; } = true;
        public int Llamadas { get; private set; }
        public Task<ResultadoEntrega> EnviarAsync(CorreoPendiente correo, CancellationToken cancelacion)
        {
            Llamadas++;
            return Fallar ? Task.FromException<ResultadoEntrega>(new InvalidOperationException()) : Task.FromResult(new ResultadoEntrega(false, "id-prueba"));
        }
    }
    public void Dispose() { contexto.Dispose(); conexion.Dispose(); }
}
