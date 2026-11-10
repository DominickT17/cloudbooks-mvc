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
    private static DbContextOptions<ContextoAplicacion> OpcionesQaSql()
    {
        var cadena = Environment.GetEnvironmentVariable("CLOUDBOOKS_QA_SQL")!;
        if (!new SqlConnectionStringBuilder(cadena).InitialCatalog.StartsWith("CloudBooks_QA_", StringComparison.Ordinal))
            throw new InvalidOperationException("Se requiere una base temporal de QA.");
        return new DbContextOptionsBuilder<ContextoAplicacion>().UseSqlServer(cadena).Options;
    }

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
    public async Task PostAdministrativosExigenRolYAntiforgeryYProtegenAdministradores()
    {
        var opciones = OpcionesQaSql();
        await using var db = new ContextoAplicacion(opciones);
        var admin = await db.Users.AsNoTracking().SingleAsync(u => u.Email == "admin.yeisson@cloudbooks.local");
        var rolesAntes = await db.UserRoles.AsNoTracking().Where(r => r.UserId == admin.Id).Select(r => r.RoleId).ToListAsync();
        var stockAntes = await db.Libros.AsNoTracking().Where(l => l.Id == 1).Select(l => l.Stock).SingleAsync();
        await using var app = CrearAplicacion();
        var rutas = new[] { "/Administracion/CambiarEstado", "/Administracion/MarcarRevisado", "/AdministracionUsuarios/CambiarRol", "/AdministracionInventario/Actualizar", "/AdministracionConfiguracion/Guardar", "/AdministracionInstituciones/Resolver", "/AdministracionSolicitudes/Asignar", "/AdministracionCorreos/Reintentar", "/Administracion/LeerAvisoGeneral", "/Administracion/LeerAviso", "/AdministracionCatalogo/Guardar", "/AdministracionCatalogo/CambiarEstado", "/AdministracionCatalogos/Guardar" };
        using var visitante = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        var tokenVisitante = Token(await visitante.GetStringAsync("/Cuenta/Ingresar"));
        foreach (var ruta in rutas)
        {
            var respuesta = await visitante.PostAsync(ruta, new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = tokenVisitante, ["id"] = "1" }));
            Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
            Assert.Contains("/Cuenta/Ingresar", respuesta.Headers.Location?.ToString());
        }
        foreach (var correo in new[] { "cliente@cloudbooks.local", "institucion@cloudbooks.local", "asesor@cloudbooks.local" })
        {
            using var cliente = await IngresarAsync(app, correo);
            var token = Token(await cliente.GetStringAsync("/Cuenta/Ingresar"));
            foreach (var ruta in rutas)
            {
                var respuesta = await cliente.PostAsync(ruta, new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token, ["id"] = "1", ["usuarioId"] = admin.Id, ["rol"] = "ClienteMinorista", ["stockNuevo"] = "0" }));
                Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
                Assert.Contains("/Cuenta/AccesoDenegado", respuesta.Headers.Location?.ToString());
            }
        }
        using var administrador = await IngresarAsync(app, "admin.dominick@cloudbooks.local");
        foreach (var ruta in rutas)
            Assert.Equal(HttpStatusCode.BadRequest, (await administrador.PostAsync(ruta, new FormUrlEncodedContent(new Dictionary<string, string> { ["id"] = "1" }))).StatusCode);
        var tokenAdmin = Token(await administrador.GetStringAsync("/AdministracionUsuarios"));
        var intento = await administrador.PostAsync("/AdministracionUsuarios/CambiarRol", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = tokenAdmin, ["usuarioId"] = admin.Id, ["rol"] = "ClienteMinorista" }));
        Assert.Equal(HttpStatusCode.Redirect, intento.StatusCode);
        Assert.Equal(rolesAntes, await db.UserRoles.AsNoTracking().Where(r => r.UserId == admin.Id).Select(r => r.RoleId).ToListAsync());
        Assert.Equal(stockAntes, await db.Libros.AsNoTracking().Where(l => l.Id == 1).Select(l => l.Stock).SingleAsync());
    }

    [SqlTemporalFact]
    public async Task TransferenciaIgnoraTarjetaYMetodoAusenteNoCreaPedido()
    {
        await using var db = new ContextoAplicacion(OpcionesQaSql());
        var usuario = await db.Users.AsNoTracking().SingleAsync(u => u.Email == "cliente@cloudbooks.local");
        var libro = await db.Libros.SingleAsync(l => l.ISBN == "9789929000003");
        libro.Stock = 100;
        await db.SaveChangesAsync();
        await using var app = CrearAplicacion();
        using var cliente = await IngresarAsync(app, usuario.Email!);
        var detalle = await cliente.GetStringAsync($"/Catalogo/Detalle/{libro.Id}");
        await cliente.PostAsync("/Carrito/Agregar", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(detalle), ["libroId"] = libro.Id.ToString(), ["cantidad"] = "1" }));
        var checkout = await cliente.GetStringAsync("/Pedidos/Confirmar");
        var token = Token(checkout);
        Assert.Contains("id=\"datos-tarjeta\" class=\"checkout-datos\" hidden disabled", checkout);
        Assert.Contains("id=\"datos-transferencia\" class=\"checkout-datos\" hidden", checkout);
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsync("/Pedidos/Crear", new FormUrlEncodedContent(new Dictionary<string, string> { ["metodo"] = "1" }))).StatusCode);
        var sinMetodo = await cliente.PostAsync("/Pedidos/Crear", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));
        Assert.Equal("/Pedidos/Confirmar", sinMetodo.Headers.Location?.ToString());
        Assert.Equal(0, await db.Pedidos.CountAsync(p => p.UsuarioId == usuario.Id));
        var datos = new Dictionary<string, string> { ["__RequestVerificationToken"] = token, ["metodo"] = "1", ["titular"] = "Dato que debe ignorarse", ["numero"] = "4000000000000002", ["cvv"] = "123" };
        var respuesta = await cliente.PostAsync("/Pedidos/Crear", new FormUrlEncodedContent(datos));
        Assert.StartsWith("/Pedidos/Detalle/", respuesta.Headers.Location?.ToString());
        var pedido = await db.Pedidos.AsNoTracking().SingleAsync(p => p.UsuarioId == usuario.Id);
        Assert.Equal(MetodoPagoSimulado.TransferenciaSimulada, pedido.MetodoPago);
        Assert.Null(pedido.TitularTarjeta);
        Assert.Null(pedido.UltimosCuatro);
        await cliente.PostAsync("/Pedidos/Crear", new FormUrlEncodedContent(datos));
        Assert.Equal(1, await db.Pedidos.CountAsync(p => p.UsuarioId == usuario.Id));
        Assert.Equal(99, await db.Libros.AsNoTracking().Where(l => l.Id == libro.Id).Select(l => l.Stock).SingleAsync());
    }

    [SqlTemporalFact]
    public async Task CheckoutConStockQueCambioConservaCarritoSinConfirmarOtroPedido()
    {
        var opciones = OpcionesQaSql();
        await using var db = new ContextoAplicacion(opciones);
        var libro = await db.Libros.SingleAsync(l => l.ISBN == "9789929000003");
        libro.Stock = 12;
        await db.SaveChangesAsync();
        var minorista = await db.Users.AsNoTracking().SingleAsync(u => u.Email == "cliente@cloudbooks.local");
        var institucional = await db.Users.AsNoTracking().SingleAsync(u => u.Email == "institucion@cloudbooks.local");
        await using var app = CrearAplicacion();
        using var primero = await IngresarAsync(app, minorista.Email!);
        using var segundo = await IngresarAsync(app, institucional.Email!);
        async Task Agregar(HttpClient cliente, int cantidad)
        {
            var html = await cliente.GetStringAsync($"/Catalogo/Detalle/{libro.Id}");
            var respuesta = await cliente.PostAsync("/Carrito/Agregar", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(html), ["libroId"] = libro.Id.ToString(), ["cantidad"] = cantidad.ToString() }));
            Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        }
        await Agregar(primero, 6);
        await Agregar(segundo, 8);
        var checkoutSegundo = await segundo.GetStringAsync("/Pedidos/Confirmar");
        var checkoutPrimero = await primero.GetStringAsync("/Pedidos/Confirmar");
        async Task<HttpResponseMessage> Pagar(HttpClient cliente, string html, string titular) => await cliente.PostAsync("/Pedidos/Crear", new FormUrlEncodedContent(new Dictionary<string, string> {
            ["__RequestVerificationToken"] = Token(html), ["metodo"] = "0", ["titular"] = titular, ["numero"] = "4000000000000002", ["vencimiento"] = "12/35", ["cvv"] = "123" }));
        Assert.Equal(HttpStatusCode.Redirect, (await Pagar(primero, checkoutPrimero, minorista.NombreCompleto)).StatusCode);
        Assert.Equal(6, await db.Libros.AsNoTracking().Where(l => l.Id == libro.Id).Select(l => l.Stock).SingleAsync());
        var cantidadAvisos = await db.AvisosAdministrativos.CountAsync();
        var fallo = await Pagar(segundo, checkoutSegundo, institucional.NombreCompleto);
        Assert.Equal("/Carrito", fallo.Headers.Location?.ToString());
        var carrito = await segundo.GetStringAsync("/Carrito");
        Assert.Contains("existencias", carrito, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(6, await db.Libros.AsNoTracking().Where(l => l.Id == libro.Id).Select(l => l.Stock).SingleAsync());
        Assert.Equal(8, await db.DetallesCarrito.Where(d => d.Carrito.UsuarioId == institucional.Id && d.LibroId == libro.Id).Select(d => d.Cantidad).SingleAsync());
        Assert.Equal(0, await db.Pedidos.CountAsync(p => p.UsuarioId == institucional.Id));
        Assert.Equal(cantidadAvisos, await db.AvisosAdministrativos.CountAsync());
    }

    [SqlTemporalFact]
    public async Task MatrizDeLecturaAdministrativaRequiereElRolAdministrador()
    {
        await using var app = CrearAplicacion();
        var rutas = new[] { "/Administracion", "/Administracion/Pedidos", "/AdministracionCatalogo", "/AdministracionInventario", "/AdministracionInstituciones", "/AdministracionSolicitudes", "/Cotizaciones/Administracion", "/AdministracionUsuarios", "/AdministracionConfiguracion", "/Administracion/Notificaciones", "/AdministracionCorreos", "/AdministracionCorreos/Ejemplos" };
        using var visitante = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        foreach (var ruta in rutas)
        {
            var respuesta = await visitante.GetAsync(ruta);
            Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
            Assert.Contains("/Cuenta/Ingresar", respuesta.Headers.Location?.ToString());
        }
        foreach (var correo in new[] { "cliente@cloudbooks.local", "institucion@cloudbooks.local", "asesor@cloudbooks.local", "admin.dominick@cloudbooks.local", "admin.yeisson@cloudbooks.local" })
        {
            using var cliente = await IngresarAsync(app, correo);
            var esAdmin = correo.StartsWith("admin.", StringComparison.Ordinal);
            foreach (var ruta in rutas)
            {
                var respuesta = await cliente.GetAsync(ruta);
                Assert.Equal(esAdmin ? HttpStatusCode.OK : HttpStatusCode.Redirect, respuesta.StatusCode);
                if (!esAdmin) Assert.Contains("/Cuenta/AccesoDenegado", respuesta.Headers.Location?.ToString());
            }
        }
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
        foreach (var ruta in new[] { "/Administracion", "/Administracion/Pedidos", "/AdministracionCatalogo", "/AdministracionInventario", "/AdministracionInstituciones", "/AdministracionSolicitudes", "/Cotizaciones/Administracion", "/AdministracionUsuarios", "/AdministracionConfiguracion", "/Administracion/Notificaciones", "/AdministracionCorreos", "/AdministracionCorreos/Ejemplos" })
            Assert.Equal(HttpStatusCode.OK, (await administrador.GetAsync(ruta)).StatusCode);
        var catalogoDigital = await administrador.GetStringAsync("/AdministracionCatalogo?formato=Ebook");
        Assert.Contains("Q89.00", catalogoDigital);
        Assert.DoesNotContain("Q@libro", catalogoDigital);
        Assert.DoesNotContain("Arquitectura para sistemas humanos", catalogoDigital);
        var ejemplos = WebUtility.HtmlDecode(await administrador.GetStringAsync("/AdministracionCorreos/Ejemplos"));
        foreach (var nombre in new[] { "Pedido aprobado", "Pedido en revisión", "Pago rechazado", "Pedido cancelado", "Institución recibida", "Cotización emitida", "Aviso administrativo" }) Assert.Contains(nombre, ejemplos);
        salida.WriteLine("La segunda cuenta administradora accedió a las rutas administrativas y a la vista de plantillas de demostración.");
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
        await db.Users.Where(u => u.Id == usuario.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.PhoneNumber, "55551234"));
        using var administrador = await IngresarAsync(app, "admin.dominick@cloudbooks.local");
        var detalleAdmin = await administrador.GetStringAsync($"/Administracion/DetallePedido/{pedido.Id}");
        Assert.Contains("https://wa.me/50255551234", detalleAdmin);
        Assert.DoesNotContain("antes%20de%20continuar%20con%20el%20procesamiento", detalleAdmin);
        salida.WriteLine($"Compra HTTP: {pedido.Codigo}, total Q{pedido.Total:0.00}, stock 100→94; rechazo sin fondos, correo en cola y PDF propio; PDF ajeno 404.");
    }

    [SqlTemporalFact]
    public async Task InstitucionSolicitaAprobacionYCotizacionHastaRespuestaConPdfPrivado()
    {
        var opciones = new DbContextOptionsBuilder<ContextoAplicacion>().UseSqlServer(Environment.GetEnvironmentVariable("CLOUDBOOKS_QA_SQL")!).Options;
        await using var db = new ContextoAplicacion(opciones);
        await using var app = CrearAplicacion();
        using var minorista = await IngresarAsync(app, "cliente@cloudbooks.local");
        using var administrador = await IngresarAsync(app, "admin.dominick@cloudbooks.local");
        var usuario = await db.Users.AsNoTracking().SingleAsync(u => u.Email == "cliente@cloudbooks.local");
        var solicitudHtml = await minorista.GetStringAsync("/Instituciones/Solicitar");
        var respuesta = await minorista.PostAsync("/Instituciones/Solicitar", new FormUrlEncodedContent(new Dictionary<string, string> {
            ["__RequestVerificationToken"] = Token(solicitudHtml), ["Institucion"] = "Instituto QA Horizonte", ["Nit"] = "QA-123", ["TipoInstitucion"] = "Educativa", ["Telefono"] = "55551234", ["Direccion"] = "Quetzaltenango", ["Representante"] = "María López" }));
        Assert.Equal("/Instituciones/MisSolicitudes", respuesta.Headers.Location?.ToString());
        var institucional = await db.SolicitudesInstitucionales.AsNoTracking().SingleAsync(s => s.UsuarioId == usuario.Id && s.Estado == EstadoSolicitudInstitucional.Pendiente);
        Assert.True(await db.CorreosPendientes.AnyAsync(c => c.ClaveEvento.StartsWith($"institucion/{institucional.Id}/recibida/admin/")));
        var adminHtml = await administrador.GetStringAsync($"/AdministracionInstituciones/Detalle/{institucional.Id}");
        respuesta = await administrador.PostAsync("/AdministracionInstituciones/Resolver", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(adminHtml), ["id"] = institucional.Id.ToString(), ["aprobar"] = "true" }));
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal(EstadoSolicitudInstitucional.Aprobada, await db.SolicitudesInstitucionales.AsNoTracking().Where(s => s.Id == institucional.Id).Select(s => s.Estado).SingleAsync());
        Assert.True(await db.CorreosPendientes.AnyAsync(c => c.ClaveEvento == $"institucion/{institucional.Id}/resuelta/cliente" && c.Tipo == "institucion_aprobada"));

        using var cliente = await IngresarAsync(app, "cliente@cloudbooks.local");
        var crearHtml = await cliente.GetStringAsync("/Solicitudes/Crear");
        respuesta = await cliente.PostAsync("/Solicitudes/Crear", new FormUrlEncodedContent(new Dictionary<string, string> {
            ["__RequestVerificationToken"] = Token(crearHtml), ["Titulo"] = "Obra de prueba QA", ["Cantidad"] = "20", ["Acabado"] = "Tapa blanda", ["Papel"] = "Bond", ["FechaRequerida"] = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)).ToString("yyyy-MM-dd") }));
        Assert.Equal("/Solicitudes", respuesta.Headers.Location?.ToString());
        var comercial = await db.SolicitudesCotizacion.AsNoTracking().SingleAsync(s => s.UsuarioId == usuario.Id);
        var asesor = await db.Users.AsNoTracking().SingleAsync(u => u.Email == "asesor@cloudbooks.local");
        Assert.Equal(asesor.Id, comercial.AsesorId);
        Assert.True(await db.CorreosPendientes.AnyAsync(c => c.ClaveEvento == $"solicitud/{comercial.Id}/asignada/{asesor.Id}"));
        using var asesorHttp = await IngresarAsync(app, "asesor@cloudbooks.local");
        var asesorHtml = await asesorHttp.GetStringAsync($"/Asesor/Detalle/{comercial.Id}");
        respuesta = await asesorHttp.PostAsync("/Asesor/IniciarRevision", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(asesorHtml), ["id"] = comercial.Id.ToString() }));
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        var cotizarHtml = await asesorHttp.GetStringAsync($"/Asesor/Cotizar/{comercial.Id}");
        respuesta = await asesorHttp.PostAsync("/Asesor/Cotizar", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(cotizarHtml), ["SolicitudId"] = comercial.Id.ToString(), ["Concepto"] = "Obra de prueba QA", ["Cantidad"] = "20", ["PrecioUnitario"] = "10.00" }));
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        var cotizacion = await db.Cotizaciones.AsNoTracking().SingleAsync(c => c.SolicitudCotizacionId == comercial.Id);
        var correo = await db.CorreosPendientes.AsNoTracking().SingleAsync(c => c.ClaveEvento == $"cotizacion/{cotizacion.Id}/emitida/cliente");
        Assert.Contains("Instituto QA Horizonte", correo.TextoPlano);
        Assert.Contains(cotizacion.Codigo, correo.TextoPlano);
        Assert.Contains(cotizacion.FechaVencimiento.ToString("dd/MM/yyyy"), correo.TextoPlano);
        var detalle = await cliente.GetStringAsync($"/Cotizaciones/Detalle/{cotizacion.Id}");
        Assert.Contains(cotizacion.Codigo, detalle);
        var pdf = await cliente.GetAsync($"/Cotizaciones/Pdf/{cotizacion.Id}");
        Assert.Equal(HttpStatusCode.OK, pdf.StatusCode);
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString((await pdf.Content.ReadAsByteArrayAsync())[..4]));
        using var otro = await IngresarAsync(app, "institucion@cloudbooks.local");
        Assert.Equal(HttpStatusCode.NotFound, (await otro.GetAsync($"/Cotizaciones/Pdf/{cotizacion.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otro.GetAsync($"/Cotizaciones/Detalle/{cotizacion.Id}")).StatusCode);
        var tokenAjeno = Token(await otro.GetStringAsync("/Cuenta/Ingresar"));
        var respuestaAjena = await otro.PostAsync("/Cotizaciones/Responder", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = tokenAjeno, ["id"] = cotizacion.Id.ToString(), ["aceptar"] = "false" }));
        Assert.Equal(HttpStatusCode.NotFound, respuestaAjena.StatusCode);
        Assert.Equal(EstadoSolicitudCotizacion.Cotizada, await db.Cotizaciones.AsNoTracking().Where(c => c.Id == cotizacion.Id).Select(c => c.Estado).SingleAsync());
        var tokenAsesor = Token(await asesorHttp.GetStringAsync("/Cuenta/Ingresar"));
        var respuestaAsesor = await asesorHttp.PostAsync("/Cotizaciones/Responder", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = tokenAsesor, ["id"] = cotizacion.Id.ToString(), ["aceptar"] = "true" }));
        Assert.Equal(HttpStatusCode.Redirect, respuestaAsesor.StatusCode);
        Assert.Contains("/Cuenta/AccesoDenegado", respuestaAsesor.Headers.Location?.ToString());
        respuesta = await cliente.PostAsync("/Cotizaciones/Responder", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(detalle), ["id"] = cotizacion.Id.ToString(), ["aceptar"] = "true" }));
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal(EstadoSolicitudCotizacion.Aceptada, await db.Cotizaciones.AsNoTracking().Where(c => c.Id == cotizacion.Id).Select(c => c.Estado).SingleAsync());
        Assert.Equal(EstadoSolicitudCotizacion.Aceptada, await db.SolicitudesCotizacion.AsNoTracking().Where(s => s.Id == comercial.Id).Select(s => s.Estado).SingleAsync());
        Assert.True(await db.CorreosPendientes.AnyAsync(c => c.ClaveEvento.StartsWith($"cotizacion/{cotizacion.Id}/respuesta/admin/") && c.Tipo == "cotizacion_aceptada"));
        salida.WriteLine($"Solicitud {institucional.Id} aprobada; {comercial.Codigo} asignada, cotización {cotizacion.Codigo} aceptada; PDF propio accesible y ajeno 404.");

        crearHtml = await cliente.GetStringAsync("/Solicitudes/Crear");
        respuesta = await cliente.PostAsync("/Solicitudes/Crear", new FormUrlEncodedContent(new Dictionary<string, string> {
            ["__RequestVerificationToken"] = Token(crearHtml), ["Titulo"] = "Cotización por vencer QA", ["Cantidad"] = "5", ["Acabado"] = "Tapa blanda", ["Papel"] = "Bond", ["FechaRequerida"] = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)).ToString("yyyy-MM-dd") }));
        Assert.Equal("/Solicitudes", respuesta.Headers.Location?.ToString());
        var segunda = await db.SolicitudesCotizacion.AsNoTracking().SingleAsync(s => s.UsuarioId == usuario.Id && s.Titulo == "Cotización por vencer QA");
        asesorHtml = await asesorHttp.GetStringAsync($"/Asesor/Detalle/{segunda.Id}");
        respuesta = await asesorHttp.PostAsync("/Asesor/IniciarRevision", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(asesorHtml), ["id"] = segunda.Id.ToString() }));
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        cotizarHtml = await asesorHttp.GetStringAsync($"/Asesor/Cotizar/{segunda.Id}");
        respuesta = await asesorHttp.PostAsync("/Asesor/Cotizar", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(cotizarHtml), ["SolicitudId"] = segunda.Id.ToString(), ["Concepto"] = segunda.Titulo, ["Cantidad"] = "5", ["PrecioUnitario"] = "10.00" }));
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        var vencida = await db.Cotizaciones.AsNoTracking().SingleAsync(c => c.SolicitudCotizacionId == segunda.Id);
        await db.Cotizaciones.Where(c => c.Id == vencida.Id).ExecuteUpdateAsync(s => s.SetProperty(c => c.FechaVencimiento, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))));
        detalle = await cliente.GetStringAsync($"/Cotizaciones/Detalle/{vencida.Id}");
        Assert.Equal(EstadoSolicitudCotizacion.Vencida, await db.Cotizaciones.AsNoTracking().Where(c => c.Id == vencida.Id).Select(c => c.Estado).SingleAsync());
        respuesta = await cliente.PostAsync("/Cotizaciones/Responder", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(detalle), ["id"] = vencida.Id.ToString(), ["aceptar"] = "true" }));
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal(EstadoSolicitudCotizacion.Vencida, await db.SolicitudesCotizacion.AsNoTracking().Where(s => s.Id == segunda.Id).Select(s => s.Estado).SingleAsync());

        crearHtml = await cliente.GetStringAsync("/Solicitudes/Crear");
        respuesta = await cliente.PostAsync("/Solicitudes/Crear", new FormUrlEncodedContent(new Dictionary<string, string> {
            ["__RequestVerificationToken"] = Token(crearHtml), ["Titulo"] = "Cotización para rechazo QA", ["Cantidad"] = "5", ["Acabado"] = "Tapa blanda", ["Papel"] = "Bond", ["FechaRequerida"] = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)).ToString("yyyy-MM-dd") }));
        Assert.Equal("/Solicitudes", respuesta.Headers.Location?.ToString());
        var tercera = await db.SolicitudesCotizacion.AsNoTracking().SingleAsync(s => s.UsuarioId == usuario.Id && s.Titulo == "Cotización para rechazo QA");
        asesorHtml = await asesorHttp.GetStringAsync($"/Asesor/Detalle/{tercera.Id}");
        respuesta = await asesorHttp.PostAsync("/Asesor/IniciarRevision", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(asesorHtml), ["id"] = tercera.Id.ToString() }));
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        cotizarHtml = await asesorHttp.GetStringAsync($"/Asesor/Cotizar/{tercera.Id}");
        respuesta = await asesorHttp.PostAsync("/Asesor/Cotizar", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(cotizarHtml), ["SolicitudId"] = tercera.Id.ToString(), ["Concepto"] = tercera.Titulo, ["Cantidad"] = "5", ["PrecioUnitario"] = "10.00" }));
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        var rechazada = await db.Cotizaciones.AsNoTracking().SingleAsync(c => c.SolicitudCotizacionId == tercera.Id);
        detalle = await cliente.GetStringAsync($"/Cotizaciones/Detalle/{rechazada.Id}");
        respuesta = await cliente.PostAsync("/Cotizaciones/Responder", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(detalle), ["id"] = rechazada.Id.ToString(), ["aceptar"] = "false" }));
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal(EstadoSolicitudCotizacion.Rechazada, await db.Cotizaciones.AsNoTracking().Where(c => c.Id == rechazada.Id).Select(c => c.Estado).SingleAsync());
        Assert.Equal(EstadoSolicitudCotizacion.Rechazada, await db.SolicitudesCotizacion.AsNoTracking().Where(s => s.Id == tercera.Id).Select(s => s.Estado).SingleAsync());
        Assert.True(await db.CorreosPendientes.AnyAsync(c => c.ClaveEvento.StartsWith($"cotizacion/{rechazada.Id}/respuesta/admin/") && c.Tipo == "cotizacion_rechazada"));
    }

    [SqlTemporalFact]
    public async Task RechazoInstitucionalConservaRolMinoristaYNotifica()
    {
        var opciones = new DbContextOptionsBuilder<ContextoAplicacion>().UseSqlServer(Environment.GetEnvironmentVariable("CLOUDBOOKS_QA_SQL")!).Options;
        await using var db = new ContextoAplicacion(opciones);
        await using var app = CrearAplicacion();
        using var cliente = await IngresarAsync(app, "cliente@cloudbooks.local");
        using var administrador = await IngresarAsync(app, "admin.dominick@cloudbooks.local");
        var usuario = await db.Users.AsNoTracking().SingleAsync(u => u.Email == "cliente@cloudbooks.local");
        var html = await cliente.GetStringAsync("/Instituciones/Solicitar");
        var respuesta = await cliente.PostAsync("/Instituciones/Solicitar", new FormUrlEncodedContent(new Dictionary<string, string> {
            ["__RequestVerificationToken"] = Token(html), ["Institucion"] = "Instituto QA Rechazo", ["Nit"] = "QA-456", ["TipoInstitucion"] = "Educativa", ["Telefono"] = "55551234", ["Direccion"] = "Quetzaltenango", ["Representante"] = "María López" }));
        Assert.Equal("/Instituciones/MisSolicitudes", respuesta.Headers.Location?.ToString());
        var solicitud = await db.SolicitudesInstitucionales.AsNoTracking().SingleAsync(s => s.UsuarioId == usuario.Id && s.Estado == EstadoSolicitudInstitucional.Pendiente);
        html = await administrador.GetStringAsync($"/AdministracionInstituciones/Detalle/{solicitud.Id}");
        respuesta = await administrador.PostAsync("/AdministracionInstituciones/Resolver", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(html), ["id"] = solicitud.Id.ToString(), ["aprobar"] = "false" }));
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal(EstadoSolicitudInstitucional.Rechazada, await db.SolicitudesInstitucionales.AsNoTracking().Where(s => s.Id == solicitud.Id).Select(s => s.Estado).SingleAsync());
        Assert.True(await db.CorreosPendientes.AnyAsync(c => c.ClaveEvento == $"institucion/{solicitud.Id}/resuelta/cliente" && c.Tipo == "institucion_rechazada"));
        using var nuevaSesion = await IngresarAsync(app, "cliente@cloudbooks.local");
        Assert.Equal(HttpStatusCode.OK, (await nuevaSesion.GetAsync("/Instituciones/Solicitar")).StatusCode);
        var acceso = await nuevaSesion.GetAsync("/Solicitudes/Crear");
        Assert.Equal(HttpStatusCode.Redirect, acceso.StatusCode);
        Assert.Contains("/Cuenta/AccesoDenegado", acceso.Headers.Location?.ToString());
    }
}
