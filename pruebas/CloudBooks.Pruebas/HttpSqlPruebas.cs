using System.Net;
using System.Text.RegularExpressions;
using CloudBooks.Web.Servicios;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;
using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using Xunit.Abstractions;

namespace CloudBooks.Pruebas;

public class HttpSqlPruebas(ITestOutputHelper salida)
{
    private static WebApplicationFactory<Program> CrearAplicacion()
    {
        var cadena = Environment.GetEnvironmentVariable("CLOUDBOOKS_QA_SQL")!;
        if (!new SqlConnectionStringBuilder(cadena).InitialCatalog.StartsWith("CloudBooks_QA_", StringComparison.Ordinal)) throw new InvalidOperationException("Se requiere una base temporal de QA.");
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = cadena, ["Correo:Proveedor"] = "Simulado", ["Logging:LogLevel:Default"] = "Warning", ["Logging:LogLevel:Microsoft.EntityFrameworkCore"] = "Warning" }));
            builder.ConfigureServices(servicios => { servicios.RemoveAll<IProveedorCorreo>(); servicios.AddScoped<IProveedorCorreo, ProveedorCorreoSimulado>(); });
        });
    }

    private static string Token(string html)
    {
        var coincidencia = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(coincidencia.Success, "No se encontró el token antiforgery.");
        return WebUtility.HtmlDecode(coincidencia.Groups[1].Value);
    }

    private static async Task<HttpClient> IngresarAsync(WebApplicationFactory<Program> app, string correo)
    {
        var cliente = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        var html = await cliente.GetStringAsync("/Cuenta/Ingresar");
        var respuesta = await cliente.PostAsync("/Cuenta/Ingresar", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(html), ["Correo"] = correo, ["Contrasena"] = "CloudBooksDemo2026!" }));
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.DoesNotContain("Ingresar", respuesta.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
        return cliente;
    }

    [SqlTemporalFact]
    public async Task RutasDirectasProtegenAdministracionYSeparanRoles()
    {
        await using var app = CrearAplicacion();
        using var visitante = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        Assert.Equal(HttpStatusCode.OK, (await visitante.GetAsync("/Catalogo")).StatusCode);
        var accesoVisitante = await visitante.GetAsync("/AdministracionCorreos");
        Assert.Equal(HttpStatusCode.Redirect, accesoVisitante.StatusCode);
        Assert.Contains("/Cuenta/Ingresar", accesoVisitante.Headers.Location?.ToString());
        var sinToken = await visitante.PostAsync("/Carrito/Agregar", new FormUrlEncodedContent(new Dictionary<string, string> { ["libroId"] = "1" }));
        Assert.Equal(HttpStatusCode.BadRequest, sinToken.StatusCode);
        foreach (var (correo, propia, ajena) in new[] {
            ("cliente@cloudbooks.local", "/Pedidos", "/Administracion"),
            ("institucion@cloudbooks.local", "/Cotizaciones", "/Administracion"),
            ("asesor@cloudbooks.local", "/Asesor", "/Administracion"),
            ("admin.dominick@cloudbooks.local", "/Administracion", "/Asesor") })
        {
            using var cliente = await IngresarAsync(app, correo);
            var permitido = await cliente.GetAsync(propia);
            var denegado = await cliente.GetAsync(ajena);
            Assert.Equal(HttpStatusCode.OK, permitido.StatusCode);
            Assert.Equal(HttpStatusCode.Redirect, denegado.StatusCode);
            Assert.Contains("/Cuenta/AccesoDenegado", denegado.Headers.Location?.ToString());
            salida.WriteLine($"{correo}: {propia} = {(int)permitido.StatusCode}; {ajena} = acceso denegado.");
        }
        using var administrador = await IngresarAsync(app, "admin.yeisson@cloudbooks.local");
        foreach (var ruta in new[] { "/Administracion", "/Administracion/Pedidos", "/AdministracionCatalogo", "/AdministracionInventario", "/AdministracionInstituciones", "/AdministracionSolicitudes", "/Cotizaciones/Administracion", "/AdministracionUsuarios", "/AdministracionConfiguracion", "/Administracion/Notificaciones", "/AdministracionCorreos" })
            Assert.Equal(HttpStatusCode.OK, (await administrador.GetAsync(ruta)).StatusCode);
        salida.WriteLine("La segunda cuenta administradora accedió a las once rutas administrativas verificadas.");
    }

    [SqlTemporalFact]
    public async Task VisitanteFusionaCarritoRechazaPagoYCompletaCompraConPdfProtegido()
    {
        var cadena = Environment.GetEnvironmentVariable("CLOUDBOOKS_QA_SQL")!;
        var opciones = new DbContextOptionsBuilder<ContextoAplicacion>().UseSqlServer(cadena).Options;
        await using var db = new ContextoAplicacion(opciones);
        var libro = await db.Libros.SingleAsync(l => l.ISBN == "9789929000003");
        var usuario = await db.Users.AsNoTracking().SingleAsync(u => u.Email == "cliente@cloudbooks.local");
        var intentosPrevios = await db.IntentosPago.CountAsync(i => i.UsuarioId == usuario.Id && i.Resultado == ResultadoPago.FondosInsuficientes);
        libro.Stock = 100;
        await db.SaveChangesAsync();
        await using var app = CrearAplicacion();
        using var cliente = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/Inicio")).StatusCode);
        Assert.Contains("Ideas que construyen futuro", await cliente.GetStringAsync("/Catalogo?buscar=Ideas"));
        var detalle = await cliente.GetStringAsync($"/Catalogo/Detalle/{libro.Id}");
        var agregar = await cliente.PostAsync("/Carrito/Agregar", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(detalle), ["libroId"] = libro.Id.ToString(), ["cantidad"] = "6" }));
        Assert.Equal(HttpStatusCode.Redirect, agregar.StatusCode);
        Assert.Contains("Ideas que construyen futuro", await cliente.GetStringAsync("/Carrito"));
        var exigirAcceso = await cliente.GetAsync("/Pedidos/Confirmar");
        Assert.Equal(HttpStatusCode.Redirect, exigirAcceso.StatusCode);
        var ingresar = await cliente.GetStringAsync("/Cuenta/Ingresar");
        var autenticacion = await cliente.PostAsync("/Cuenta/Ingresar", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(ingresar), ["Correo"] = "cliente@cloudbooks.local", ["Contrasena"] = "CloudBooksDemo2026!" }));
        Assert.Equal(HttpStatusCode.Redirect, autenticacion.StatusCode);
        var confirmar = await cliente.GetStringAsync("/Pedidos/Confirmar");
        Assert.Contains("Ideas que construyen futuro", confirmar);
        Assert.Contains("Q702.24", confirmar);
        async Task<HttpResponseMessage> Pagar(string numero) => await cliente.PostAsync("/Pedidos/Crear", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(confirmar), ["metodo"] = "0", ["titular"] = "María López", ["numero"] = numero, ["vencimiento"] = "12/35", ["cvv"] = "123" }));
        Assert.Equal(HttpStatusCode.Redirect, (await Pagar("4000000000000010")).StatusCode);
        Assert.Contains("fondos suficientes", await cliente.GetStringAsync("/Pedidos/Confirmar"), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(100, await db.Libros.AsNoTracking().Where(l => l.Id == libro.Id).Select(l => l.Stock).SingleAsync());
        Assert.Equal(HttpStatusCode.Redirect, (await Pagar("4000000000000002")).StatusCode);
        var pedido = await db.Pedidos.AsNoTracking().Include(p => p.Detalles).Where(p => p.UsuarioId == usuario.Id).OrderByDescending(p => p.Id).FirstAsync();
        Assert.Equal(EstadoPedido.Aprobado, pedido.Estado);
        Assert.Equal(EstadoPago.Confirmado, pedido.EstadoPago);
        Assert.Equal(702.24m, pedido.Total);
        Assert.Equal(94, await db.Libros.AsNoTracking().Where(l => l.Id == libro.Id).Select(l => l.Stock).SingleAsync());
        Assert.Equal(intentosPrevios + 1, await db.IntentosPago.CountAsync(i => i.UsuarioId == usuario.Id && i.Resultado == ResultadoPago.FondosInsuficientes));
        Assert.True(await db.CorreosPendientes.AnyAsync(c => c.ClaveEvento == $"pedido/{pedido.Id}/aprobado/cliente"));
        var pdf = await cliente.GetAsync($"/Pedidos/Comprobante/{pedido.Id}");
        Assert.Equal(HttpStatusCode.OK, pdf.StatusCode);
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString((await pdf.Content.ReadAsByteArrayAsync())[..4]));
        using var otro = await IngresarAsync(app, "institucion@cloudbooks.local");
        Assert.Equal(HttpStatusCode.NotFound, (await otro.GetAsync($"/Pedidos/Comprobante/{pedido.Id}")).StatusCode);
        salida.WriteLine($"Compra HTTP: {pedido.Codigo}, total Q{pedido.Total:0.00}, stock 100→94; rechazo sin fondos, correo en cola y PDF propio; PDF ajeno 404.");
    }
}
