using System.Net;
using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using CloudBooks.Web.Servicios;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Resend;

namespace CloudBooks.Pruebas;

public class CiteinCorreoPruebas : IDisposable
{
    private readonly SqliteConnection conexion = new("Data Source=:memory:");
    private readonly ContextoAplicacion db;
    private readonly IConfiguration config;
    private readonly CapturaHttp captura = new();
    private readonly ProveedorCorreoResend resend;

    public CiteinCorreoPruebas()
    {
        conexion.Open();
        db = new ContextoAplicacion(new DbContextOptionsBuilder<ContextoAplicacion>().UseSqlite(conexion).Options);
        db.Database.EnsureCreated();
        config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["CiteinDemo:Enabled"] = "true", ["CiteinDemo:CorreoReal"] = "true",
            ["CiteinDemo:SesionCorreo"] = Guid.NewGuid().ToString("N"),
            ["CiteinDemo:DestinatarioAutorizado"] = "autorizado@gmail.com", ["Correo:Proveedor"] = "Resend",
            ["Correo:Remitente"] = "CloudBooks <envios@libros.gt>", ["Correo:DominioVerificado"] = "libros.gt", ["Resend:ApiKey"] = "prueba"
        }).Build();
        var api = ResendClient.Create(new ResendClientOptions { ApiToken = "prueba" }, new HttpClient(captura));
        resend = new ProveedorCorreoResend(api, config, db);
    }

    [Fact]
    public void ConfiguracionExigeActivacionSesionRemitentePropioYDestinatarioPublico()
    {
        ConfiguracionCorreo.ValidarCitein(config);
        foreach (var (clave, valor) in new[] {
            ("CiteinDemo:CorreoReal", "false"), ("CiteinDemo:SesionCorreo", ""),
            ("Resend:ApiKey", ""), ("Correo:DominioVerificado", "otro.gt"),
            ("Correo:Remitente", "onboarding@resend.dev"), ("CiteinDemo:DestinatarioAutorizado", "a@visitante.citein.invalid"),
            ("CiteinDemo:DestinatarioAutorizado", "a@cloudbooks.local") })
        {
            var previo = config[clave]; config[clave] = valor;
            Assert.Throws<InvalidOperationException>(() => ConfiguracionCorreo.ValidarCitein(config));
            config[clave] = previo;
        }
        config["Correo:Proveedor"] = "Simulado";
        config["Resend:ApiKey"] = "";
        ConfiguracionCorreo.ValidarCitein(config);
    }

    [Fact]
    public async Task ProtegeHistoricosAdminOtrosClientesYReinicioSinRed()
    {
        db.Users.Add(new UsuarioAplicacion { Id = "cliente", UserName = "cliente", Email = "autorizado@gmail.com", NombreCompleto = "Cliente" });
        db.Pedidos.Add(new Pedido { Id = 1, UsuarioId = "cliente", Estado = EstadoPedido.Aprobado });
        db.CorreosPendientes.Add(new CorreoPendiente { ClaveEvento = "historico", Destinatario = "autorizado@gmail.com", Tipo = "aprobado", Asunto = "anterior", Html = "anterior", Estado = EstadoCorreo.Pendiente, CreadoUtc = DateTime.UtcNow.AddDays(-1) });
        await db.SaveChangesAsync();
        var n = new ServicioNotificaciones(db, config);
        await n.EncolarAsync("pedido/1/aprobado/cliente", "aprobado", "PED-000001", "autorizado@gmail.com", "Nuevo", "Contenido");
        await n.EncolarAsync("pedido/1/revision/admin/abc", "revision", null, "autorizado@gmail.com", "Admin", "Contenido");
        await n.EncolarAsync("pedido/2/aprobado/cliente", "aprobado", null, "otro@gmail.com", "Otro", "Contenido");
        await n.EncolarAsync("pedido/3/aprobado/cliente", "aprobado", null, "a@visitante.citein.invalid", "Visitante", "Contenido");
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var router = new ProveedorCorreoCitein(db, config, resend, new ProveedorCorreoSimulado());
        var procesador = new ProcesadorCorreos(db, router, config);
        while (await procesador.ProcesarUnoAsync()) { }
        var lista = await db.CorreosPendientes.AsNoTracking().ToListAsync();
        Assert.Equal(EstadoCorreo.Pendiente, lista.Single(c => c.ClaveEvento == "historico").Estado);
        Assert.Equal(EstadoCorreo.AceptadoProveedor, lista.Single(c => c.ClaveEvento == "pedido/1/aprobado/cliente").Estado);
        Assert.All(lista.Where(c => c.Asunto is "Admin" or "Otro" or "Visitante"), c => Assert.Equal(EstadoCorreo.Simulado, c.Estado));
        Assert.Equal(1, captura.Llamadas);

        var anterior = ConfiguracionCorreo.MarcaRealCitein(config);
        db.CorreosPendientes.Add(new CorreoPendiente { ClaveEvento = "pedido/1/revision/cliente", Tipo = "revision", Destinatario = "autorizado@gmail.com", Asunto = "Anterior", Html = "hola", Estado = EstadoCorreo.ReintentoPendiente, Proveedor = anterior });
        await db.SaveChangesAsync();
        config["CiteinDemo:SesionCorreo"] = Guid.NewGuid().ToString("N");
        Assert.False(await procesador.ProcesarUnoAsync());
        Assert.Equal(1, captura.Llamadas);
    }

    [Fact]
    public async Task FalloDelProveedorConservaLaAutorizacionYReintentaConElMismoContenido()
    {
        db.Users.Add(new UsuarioAplicacion { Id = "u", UserName = "u", Email = "autorizado@gmail.com", NombreCompleto = "Cliente" });
        db.Pedidos.Add(new Pedido { Id = 1, UsuarioId = "u", Estado = EstadoPedido.Aprobado });
        await db.SaveChangesAsync();
        var n = new ServicioNotificaciones(db, config);
        await n.EncolarAsync("pedido/1/aprobado/cliente", "aprobado", "PED-000001", "autorizado@gmail.com", "Pedido", "Contenido", "%PDF-archivo"u8.ToArray());
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        captura.Fallar = true;
        var router = new ProveedorCorreoCitein(db, config, resend, new ProveedorCorreoSimulado());
        var procesador = new ProcesadorCorreos(db, router, config);
        Assert.True(await procesador.ProcesarUnoAsync());
        var fallo = await db.CorreosPendientes.AsNoTracking().SingleAsync();
        Assert.Equal(EstadoCorreo.ReintentoPendiente, fallo.Estado);
        Assert.Equal(ConfiguracionCorreo.MarcaRealCitein(config), fallo.Proveedor);
        var cuerpo = captura.Cuerpo;
        await db.CorreosPendientes.ExecuteUpdateAsync(s => s.SetProperty(c => c.ProximoIntentoUtc, DateTime.UtcNow.AddMinutes(-1)));
        captura.Fallar = false;
        Assert.True(await procesador.ProcesarUnoAsync());
        Assert.Equal(cuerpo, captura.Cuerpo);
        Assert.Equal(EstadoCorreo.AceptadoProveedor, (await db.CorreosPendientes.AsNoTracking().SingleAsync()).Estado);
        Assert.Equal(EstadoPedido.Aprobado, (await db.Pedidos.AsNoTracking().SingleAsync()).Estado);
    }

    [Theory]
    [InlineData("visitante@visitante.citein.invalid")]
    [InlineData("cliente@cloudbooks.local")]
    public async Task ResendNuncaEnviaDireccionesFicticias(string destinatario)
    {
        var resultado = await resend.EnviarAsync(new CorreoPendiente { Destinatario = destinatario }, CancellationToken.None);
        Assert.True(resultado.Simulado);
        Assert.Equal(0, captura.Llamadas);
    }

    [Fact]
    public async Task PdfSoloSeAdjuntaAlEventoAprobadoDelPropietarioYEstadoAprobado()
    {
        db.Users.Add(new UsuarioAplicacion { Id = "u", UserName = "u", Email = "autorizado@gmail.com", NombreCompleto = "Cliente" });
        db.Pedidos.Add(new Pedido { Id = 1, UsuarioId = "u", Estado = EstadoPedido.Aprobado });
        await db.SaveChangesAsync();
        var correo = new CorreoPendiente { ClaveEvento = "pedido/1/aprobado/cliente", Tipo = "aprobado", Destinatario = "autorizado@gmail.com", Asunto = "Prueba", Html = "<img src=\"cid:cloudbooks-logo\">", LogoCorreo = [1, 2, 3], ComprobantePdf = "%PDF-123"u8.ToArray() };
        await resend.EnviarAsync(correo, default);
        Assert.Contains("comprobante-PED-000001.pdf", captura.Cuerpo);
        Assert.Contains("cloudbooks-logo", captura.Cuerpo);
        Assert.Contains(Convert.ToBase64String(correo.ComprobantePdf), captura.Cuerpo);
        await db.Pedidos.Where(p => p.Id == 1).ExecuteUpdateAsync(s => s.SetProperty(p => p.Estado, EstadoPedido.EnRevision));
        await resend.EnviarAsync(correo, default);
        Assert.DoesNotContain("comprobante-PED-000001.pdf", captura.Cuerpo);
        correo.Tipo = "revision"; correo.ClaveEvento = "pedido/1/revision/cliente";
        await resend.EnviarAsync(correo, default);
        Assert.DoesNotContain("comprobante-PED-000001.pdf", captura.Cuerpo);
    }

    private class CapturaHttp : HttpMessageHandler
    {
        public int Llamadas { get; private set; }
        public bool Fallar { get; set; }
        public string Cuerpo { get; private set; } = "";
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Llamadas++; Cuerpo = await request.Content!.ReadAsStringAsync(token);
            if (Fallar) return new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("{\"message\":\"error de prueba\"}", System.Text.Encoding.UTF8, "application/json") };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"id\":\"b59fbb1f-b839-4aef-9c9a-6680ba344291\"}", System.Text.Encoding.UTF8, "application/json") };
        }
    }
    public void Dispose() { db.Dispose(); conexion.Dispose(); }
}
