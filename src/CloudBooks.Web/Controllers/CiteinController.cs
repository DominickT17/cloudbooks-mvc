using System.Security.Claims;
using System.Collections.Concurrent;
using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using CloudBooks.Web.ModelosVista;
using CloudBooks.Web.Servicios;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace CloudBooks.Web.Controllers;

[Route("citein")]
[EnableRateLimiting("citein")]
public class CiteinController(ContextoAplicacion db, UserManager<UsuarioAplicacion> usuarios, ServicioCarrito carrito,
    ServicioCatalogo catalogo, ServicioDescuentos descuentos, ServicioParametros parametros, ServicioPedidos pedidos) : Controller
{
    private string UsuarioId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private const string Esquema = "Citein";
    // La feria utiliza una sola instancia. Los candados serializan inicio y compra por sesión.
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Candados = new();
    private static readonly ConcurrentDictionary<string, string> IdentidadesSesion = new();
    private static SemaphoreSlim Candado(string clave) => Candados.GetOrAdd(clave, _ => new SemaphoreSlim(1, 1));
    // ISBN de ediciones verificadas; el orden de feria no depende de los IDs SQL.
    private static readonly string[] IsbnCitein = ["9786075502090", "9786075502076", "9786075502069", "9786073221245", "9781951693510", "9781951693909"];
    private static bool Habilitado(Libro libro) => IsbnCitein.Contains(libro.ISBN) && libro.Activo &&
        (libro.Formato == FormatoLibro.Ebook || libro.Stock > 0);
    private async Task<List<Libro>> LibrosHabilitadosAsync() => (await catalogo.ObtenerPorIsbnAsync(IsbnCitein))
        .Where(Habilitado).OrderBy(l => Array.IndexOf(IsbnCitein, l.ISBN)).ToList();
    private Task<int?> PedidoRegistradoAsync() => db.Pedidos.AsNoTracking().Where(p => p.UsuarioId == UsuarioId)
        .OrderByDescending(p => p.Id).Select(p => (int?)p.Id).FirstOrDefaultAsync();

    [HttpGet("")]
    public IActionResult Index()
    {
        // Materializa la cookie de sesión antes de aceptar pulsaciones concurrentes.
        HttpContext.Session.SetString("CiteinInicializada", "1");
        return View();
    }

    [HttpPost("iniciar"), ValidateAntiForgeryToken, EnableRateLimiting("citein-inicio")]
    public async Task<IActionResult> Iniciar()
    {
        var sesion = HttpContext.Session.Id;
        var candado = Candado("inicio:" + sesion);
        await candado.WaitAsync();
        try
        {
            var autenticacion = await HttpContext.AuthenticateAsync(Esquema);
            var identidadId = autenticacion.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (identidadId == null) IdentidadesSesion.TryGetValue(sesion, out identidadId);
            var usuario = identidadId == null ? null : await usuarios.FindByIdAsync(identidadId);
            if (usuario == null || !usuario.Activo || !await usuarios.IsInRoleAsync(usuario, "ClienteMinorista"))
            {
                if (await db.Users.CountAsync(u => u.Email!.EndsWith("@visitante.citein.invalid")) >= 5000)
                { TempData["Error"] = "Se alcanzó el límite de sesiones académicas. Solicita ayuda al expositor."; return RedirectToAction(nameof(Index)); }
                var identificador = Guid.NewGuid().ToString("N");
                var correo = identificador + "@visitante.citein.invalid";
                usuario = new UsuarioAplicacion { UserName = correo, Email = correo, NombreCompleto = "Visitante CITEIN " + identificador[..8].ToUpperInvariant(), EmailConfirmed = true };
                var creado = await usuarios.CreateAsync(usuario);
                if (!creado.Succeeded) { TempData["Error"] = "No fue posible iniciar la demostración. Inténtalo de nuevo."; return RedirectToAction(nameof(Index)); }
                var rol = await usuarios.AddToRoleAsync(usuario, "ClienteMinorista");
                if (!rol.Succeeded) { await usuarios.DeleteAsync(usuario); TempData["Error"] = "No fue posible iniciar la demostración."; return RedirectToAction(nameof(Index)); }
            }
            IdentidadesSesion[sesion] = usuario.Id;
            var identidad = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, usuario.Id), new Claim(ClaimTypes.Name, usuario.NombreCompleto), new Claim(ClaimTypes.Role, "ClienteMinorista")], Esquema);
            await HttpContext.SignInAsync(Esquema, new ClaimsPrincipal(identidad), new AuthenticationProperties { IsPersistent = false });
            return RedirectToAction(nameof(Libros));
        }
        finally { candado.Release(); }
    }

    [Authorize(AuthenticationSchemes = Esquema, Roles = "ClienteMinorista"), HttpPost("reiniciar"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Reiniciar()
    {
        var sesion = HttpContext.Session.Id;
        var candado = Candado("inicio:" + sesion);
        await candado.WaitAsync();
        try { IdentidadesSesion.TryRemove(sesion, out _); await HttpContext.SignOutAsync(Esquema); }
        finally { candado.Release(); }
        return RedirectToAction(nameof(Index));
    }

    [Authorize(AuthenticationSchemes = Esquema, Roles = "ClienteMinorista"), HttpGet("libros")]
    public async Task<IActionResult> Libros()
    {
        var existente = await PedidoRegistradoAsync();
        if (existente.HasValue) return RedirectToAction(nameof(Resultado), new { id = existente.Value });
        var libros = await LibrosHabilitadosAsync();
        ViewBag.Todos = Request.Query["todos"] == "1";
        return View(libros);
    }

    [Authorize(AuthenticationSchemes = Esquema, Roles = "ClienteMinorista"), HttpPost("elegir"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Elegir(int libroId)
    {
        var candado = Candado("compra:" + UsuarioId);
        await candado.WaitAsync();
        try
        {
            var existente = await PedidoRegistradoAsync();
            if (existente.HasValue) return RedirectToAction(nameof(Resultado), new { id = existente.Value });
            var disponibles = (await LibrosHabilitadosAsync()).Any(l => l.Id == libroId);
            if (!disponibles) { TempData["Error"] = "Este libro ya no está disponible. Elige otro."; return RedirectToAction(nameof(Libros)); }
            var anterior = await carrito.ObtenerAsync(UsuarioId);
            foreach (var detalle in anterior?.Detalles ?? []) await carrito.EliminarAsync(UsuarioId, detalle.Id);
            var error = await carrito.AgregarAsync(UsuarioId, libroId, 1);
            if (error != null) { TempData["Error"] = error; return RedirectToAction(nameof(Libros)); }
            return RedirectToAction(nameof(Confirmar));
        }
        finally { candado.Release(); }
    }

    [Authorize(AuthenticationSchemes = Esquema, Roles = "ClienteMinorista"), HttpGet("confirmar")]
    public async Task<IActionResult> Confirmar()
    {
        var candado = Candado("compra:" + UsuarioId);
        await candado.WaitAsync();
        try
        {
            var existente = await PedidoRegistradoAsync();
            if (existente.HasValue) return RedirectToAction(nameof(Resultado), new { id = existente.Value });
            var detalles = (await carrito.ObtenerAsync(UsuarioId))?.Detalles;
            var detalle = detalles?.Count == 1 ? detalles[0] : null;
            if (detalle == null || detalle.Cantidad != 1 || !Habilitado(detalle.Libro))
            { TempData["Error"] = "El libro ya no está disponible para la demostración. Elige uno nuevamente."; return RedirectToAction(nameof(Libros)); }
            var libro = await catalogo.ObtenerAsync(detalle.LibroId);
            if (libro == null || !Habilitado(libro)) { TempData["Error"] = "El libro ya no está disponible. Elige otro."; return RedirectToAction(nameof(Libros)); }
            var resumen = descuentos.Calcular(libro.PrecioBase, 1, false, (await parametros.ObtenerAsync()).TasaImpuesto);
            return View(new ConfirmacionCitein(libro, resumen));
        }
        finally { candado.Release(); }
    }

    [Authorize(AuthenticationSchemes = Esquema, Roles = "ClienteMinorista"), HttpPost("comprar"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Comprar()
    {
        var esAjax = Request.Headers.Accept.Any(h => h != null && h.Contains("application/json", StringComparison.OrdinalIgnoreCase));
        IActionResult Fallo(string mensaje, string accion)
        {
            if (esAjax) return BadRequest(new { error = mensaje, url = Url.Action(accion) });
            TempData["Error"] = mensaje;
            return RedirectToAction(accion);
        }
        IActionResult Exito(int id) => esAjax ? Json(new { url = Url.Action(nameof(Resultado), new { id }) }) : RedirectToAction(nameof(Resultado), new { id });
        var candado = Candado("compra:" + UsuarioId);
        await candado.WaitAsync();
        try
        {
            // Una identidad académica solo registra una compra, incluso tras perder la respuesta HTTP.
            var existente = await PedidoRegistradoAsync();
            if (existente.HasValue) return Exito(existente.Value);
            var usuario = await usuarios.FindByIdAsync(UsuarioId);
            if (usuario == null || !usuario.Activo || !await usuarios.IsInRoleAsync(usuario, "ClienteMinorista")) return Forbid(Esquema);
            var detalles = (await carrito.ObtenerAsync(UsuarioId))?.Detalles;
            if (detalles?.Count != 1 || detalles[0].Cantidad != 1 || !Habilitado(detalles[0].Libro))
                return Fallo("El libro ya no está disponible para la demostración. Elige uno nuevamente.", nameof(Libros));
            var tarjeta = new SimuladorTarjeta().Evaluar("4000000000000002", "12/99", "123", DateOnly.FromDateTime(DateTime.UtcNow));
            if (!tarjeta.Autorizada) return Fallo("No fue posible autorizar el pago simulado.", nameof(Confirmar));
            var resultado = await pedidos.CrearAsync(UsuarioId, false, MetodoPagoSimulado.TarjetaSimulada, usuario.NombreCompleto, tarjeta.UltimosCuatro);
            if (resultado.Error != null) return Fallo(resultado.Error, nameof(Confirmar));
            return Exito(resultado.Pedido!.Id);
        }
        finally { candado.Release(); }
    }

    [Authorize(AuthenticationSchemes = Esquema, Roles = "ClienteMinorista"), HttpGet("compra/estado")]
    public async Task<IActionResult> EstadoCompra()
    {
        Response.Headers.CacheControl = "no-store";
        var candado = Candado("compra:" + UsuarioId);
        await candado.WaitAsync();
        try
        {
            var id = await PedidoRegistradoAsync();
            return Json(new { registrado = id.HasValue, url = id.HasValue ? Url.Action(nameof(Resultado), new { id }) : null });
        }
        finally { candado.Release(); }
    }

    [Authorize(AuthenticationSchemes = Esquema, Roles = "ClienteMinorista"), HttpGet("resultado/{id:int}")]
    public async Task<IActionResult> Resultado(int id)
    {
        var pedido = await pedidos.ObtenerAsync(id, UsuarioId);
        return pedido == null ? NotFound() : View(pedido);
    }

    [Authorize(Roles = "Administrador"), HttpGet("monitor")]
    public IActionResult Monitor() => View();

    [Authorize(Roles = "Administrador"), HttpGet("monitor/datos")]
    public async Task<IActionResult> DatosMonitor()
    {
        Response.Headers.CacheControl = "no-store";
        var consulta = db.Pedidos.AsNoTracking().Where(p => p.Usuario.Email!.EndsWith("@visitante.citein.invalid"));
        var total = await consulta.CountAsync();
        var ultimos = await consulta.Include(p => p.Usuario).Include(p => p.Detalles).ThenInclude(d => d.Libro)
            .OrderByDescending(p => p.Id).Take(5).ToListAsync();
        var lista = ultimos.Select(p =>
        {
            var detalle = p.Detalles.First();
            return new { p.Id, p.Codigo, Cliente = p.Usuario.NombreCompleto, Libro = detalle.Titulo, Imagen = detalle.Libro.ImagenPortada,
                Digital = detalle.Formato == FormatoLibro.Ebook, detalle.Cantidad, p.Total, Estado = p.EstadoTexto,
                EnRevision = p.Estado == EstadoPedido.EnRevision, Hora = FechaPresentacion.EnGuatemala(p.FechaUtc).ToString("dd/MM/yyyy HH:mm:ss"),
                Existencias = detalle.Formato == FormatoLibro.Ebook ? (int?)null : detalle.Libro.Stock,
                DetalleUrl = Url.Action("DetallePedido", "Administracion", new { id = p.Id }) };
        });
        return Json(new { total, actualizacion = FechaPresentacion.EnGuatemala(DateTime.UtcNow).ToString("dd/MM/yyyy HH:mm:ss"), pedidos = lista });
    }
}

public record ConfirmacionCitein(Libro Libro, ResumenComercial Resumen);
