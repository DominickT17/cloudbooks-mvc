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
        var correo = new CorreoPendiente { ClaveEnvio = Guid.NewGuid(), Destinatario = "cliente@example.test", Asunto = "Prueba", Html = "<p>Hola</p>", TextoPlano = "Hola en texto" };
        var resultado = await new ProveedorCorreoResend(cliente, config).EnviarAsync(correo, CancellationToken.None);
        Assert.False(resultado.Simulado);
        Assert.Equal("b59fbb1f-b839-4aef-9c9a-6680ba344291", resultado.IdProveedor);
        Assert.Equal($"cloudbooks/{correo.ClaveEnvio:N}", manejador.Clave);
        Assert.Contains("Hola en texto", manejador.Cuerpo);
    }

    [Fact]
    public void ConfiguracionPrefiereClaveResendYExigeActivacionExplicita()
    {
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Resend:ApiKey"] = "clave-prueba",
            ["Correo:ApiKey"] = "clave-anterior",
            ["Correo:Remitente"] = "CloudBooks <onboarding@resend.dev>"
        }).Build();
        Assert.Equal("clave-prueba", ConfiguracionCorreo.ObtenerClave(configuracion));
        Assert.False(ConfiguracionCorreo.UsarResend(configuracion));
        configuracion["Correo:Proveedor"] = "Resend";
        Assert.True(ConfiguracionCorreo.UsarResend(configuracion));
        configuracion["Correo:Remitente"] = "no-es-un-correo";
        Assert.False(ConfiguracionCorreo.UsarResend(configuracion));
    }

    [Fact]
    public void PlantillasDeEjemploTienenTextoYHtmlAdaptableSinDatosDeTarjeta()
    {
        var ejemplos = EjemplosCorreo.Crear("https://cloudbooks.example", null);
        Assert.Equal(13, ejemplos.Count);
        Assert.All(ejemplos, ejemplo =>
        {
            Assert.Contains("<table", ejemplo.Html);
            Assert.Contains("max-width:600px", ejemplo.Html);
            Assert.Contains("CloudBooks", ejemplo.Texto);
            Assert.DoesNotContain("<script", ejemplo.Html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("4000000000000002", ejemplo.Html);
            Assert.DoesNotContain("CVV", ejemplo.Texto, StringComparison.OrdinalIgnoreCase);
        });
        var pedido = ejemplos.Single(e => e.Nombre == "Pedido aprobado");
        Assert.Contains("Q502.88", pedido.Html);
        Assert.Contains("Q502.88", pedido.Texto);
        Assert.Contains("https://cloudbooks.example/Pedidos/Comprobante/42", pedido.Html);
        var cotizacion = ejemplos.Single(e => e.Nombre == "Cotización emitida");
        Assert.Contains("Biblioteca Horizonte", cotizacion.Html);
        Assert.Contains("/Cotizaciones/Pdf/7", cotizacion.Html);
        Assert.Contains("19/11/2026", cotizacion.Texto);
    }

    [Theory]
    [InlineData("http://localhost:5000")]
    [InlineData("https://localhost")]
    [InlineData("https://tienda.localhost")]
    [InlineData("https://127.0.0.1")]
    [InlineData("javascript:alert(1)")]
    public void SinUrlPublicaNoIncorporaLogoNiEnlacesDeAplicacion(string url)
    {
        var correo = PlantillasCorreo.Crear("pedido/42/aprobado/cliente", "aprobado", "<asunto>", "Hola <script>alert(1)</script>", url, url);
        Assert.DoesNotContain("<img", correo.Html);
        Assert.DoesNotContain("<script>", correo.Html);
        Assert.Contains("&lt;script&gt;", correo.Html);
        Assert.DoesNotContain("localhost", correo.Html);
        Assert.DoesNotContain("/Pedidos/Detalle", correo.Html);
        Assert.Contains("Hola <script>", correo.Texto);
    }

    [Fact]
    public void PagoRechazadoEnlazaAlCarritoSinInventarPedido()
    {
        var correo = PlantillasCorreo.Crear("intento/8/rechazado", "pago_rechazado", "Pago", "Fondos insuficientes", "https://cloudbooks.example", null);
        Assert.Contains("https://cloudbooks.example/Carrito", correo.Html);
        Assert.DoesNotContain("/Pedidos/Detalle", correo.Html);
        Assert.DoesNotContain("PED-", correo.Texto);
    }

    [Fact]
    public void ResumenDePedidoEscapaTitulosYConservaImportesSinAlterarTextoPlano()
    {
        const string contenido = "Hola, María:\n\nProductos:\n2 × Libro <especial>\n\nSubtotal: Q100.00\nDescuento por volumen: Q5.00\nIVA: Q11.40\nTOTAL: Q106.40";
        var correo = PlantillasCorreo.Crear("pedido/42/aprobado/cliente", "aprobado", "Pedido", contenido, null);
        Assert.Contains("PRODUCTOS</td>", correo.Html);
        Assert.Contains("Libro &lt;especial&gt;", correo.Html);
        Assert.Contains("2 × Libro <especial>", WebUtility.HtmlDecode(correo.Html));
        Assert.Contains("Q106.40", correo.Html);
        Assert.Contains(contenido, correo.Texto);
        Assert.DoesNotContain("/Pedidos/Detalle", correo.Html);
    }

    [Fact]
    public void ResumenDeCotizacionEscapaDatosYConservaTextoPlano()
    {
        const string contenido = "Hola, María:\n\nCotización disponible.\nInstitución: Colegio <Sol>\nCotización: COT-000007\nFecha: 05/11/2026 UTC\nVigencia hasta: 20/11/2026\nEstado: Emitida\nTotal: Q100.00\n\nConsulta tu cuenta.";
        var correo = PlantillasCorreo.Crear("cotizacion/7/emitida/cliente", "cotizacion_emitida", "Cotización", contenido, null);
        Assert.Contains("Colegio &lt;Sol&gt;", correo.Html);
        Assert.Contains("Q100.00", correo.Html);
        Assert.Contains("Vigencia hasta", correo.Html);
        Assert.Contains(contenido, correo.Texto);
        Assert.DoesNotContain("/Cotizaciones/Detalle", correo.Html);
    }

    [Fact]
    public void LogoSoloAdmiteUrlHttpsDeImagenRaster()
    {
        var svg = PlantillasCorreo.Crear("pedido/42/aprobado/cliente", "aprobado", "Asunto", "Mensaje", null, "https://cloudbooks.example/logo.svg");
        Assert.DoesNotContain("<img", svg.Html);
        var png = PlantillasCorreo.Crear("pedido/42/aprobado/cliente", "aprobado", "Asunto", "Mensaje", null, "https://cloudbooks.example/logo.png");
        Assert.Contains("alt=\"CloudBooks\"", png.Html);
        Assert.Contains("https://cloudbooks.example/logo.png", png.Html);
    }

    private class ManejadorResend : HttpMessageHandler
    {
        public string? Clave { get; private set; }
        public string Cuerpo { get; private set; } = "";
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage solicitud, CancellationToken cancelacion)
        {
            Clave = solicitud.Headers.GetValues("Idempotency-Key").Single();
            Cuerpo = await solicitud.Content!.ReadAsStringAsync(cancelacion);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"id\":\"b59fbb1f-b839-4aef-9c9a-6680ba344291\"}", System.Text.Encoding.UTF8, "application/json") };
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
