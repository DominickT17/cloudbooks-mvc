using System.Security.Claims;
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
    // ISBN de ediciones verificadas; el orden de feria no depende de los IDs SQL.
    private static readonly string[] IsbnCitein = ["9786075502090", "9786075502076", "9786075502069", "9786073221245", "9781951693510", "9781951693909"];
    private static bool Habilitado(Libro libro) => IsbnCitein.Contains(libro.ISBN) && libro.Activo &&
        (libro.Formato == FormatoLibro.Ebook || libro.Stock > 0);
    private async Task<List<Libro>> LibrosHabilitadosAsync() => (await catalogo.ObtenerPorIsbnAsync(IsbnCitein))
        .Where(Habilitado).OrderBy(l => Array.IndexOf(IsbnCitein, l.ISBN)).ToList();

    [HttpGet("")]
    public IActionResult Index() => View();

    [HttpPost("iniciar"), ValidateAntiForgeryToken, EnableRateLimiting("citein-inicio")]
    public async Task<IActionResult> Iniciar()
    {
        // Cada visita recibe una identidad sin contraseña y una cookie separada de Identity.
        if (await db.Users.CountAsync(u => u.Email!.EndsWith("@visitante.citein.invalid")) >= 5000)
        { TempData["Error"] = "Se alcanzó el límite de sesiones académicas. Solicita ayuda al expositor."; return RedirectToAction(nameof(Index)); }
        var identificador = Guid.NewGuid().ToString("N");
        var alias = "Visitante CITEIN " + identificador[..8].ToUpperInvariant();
        var correo = identificador + "@visitante.citein.invalid";
        var usuario = new UsuarioAplicacion { UserName = correo, Email = correo, NombreCompleto = alias, EmailConfirmed = true };
        var creado = await usuarios.CreateAsync(usuario);
        if (!creado.Succeeded) { TempData["Error"] = "No fue posible iniciar la demostración. Inténtalo de nuevo."; return RedirectToAction(nameof(Index)); }
        var rol = await usuarios.AddToRoleAsync(usuario, "ClienteMinorista");
        if (!rol.Succeeded) { await usuarios.DeleteAsync(usuario); TempData["Error"] = "No fue posible iniciar la demostración."; return RedirectToAction(nameof(Index)); }
        var identidad = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, usuario.Id), new Claim(ClaimTypes.Name, alias), new Claim(ClaimTypes.Role, "ClienteMinorista")], Esquema);
        await HttpContext.SignInAsync(Esquema, new ClaimsPrincipal(identidad), new AuthenticationProperties { IsPersistent = false });
        return RedirectToAction(nameof(Libros));
    }

    [Authorize(AuthenticationSchemes = Esquema, Roles = "ClienteMinorista"), HttpGet("libros")]
    public async Task<IActionResult> Libros()
    {
        var libros = await LibrosHabilitadosAsync();
        return View(libros);
    }

    [Authorize(AuthenticationSchemes = Esquema, Roles = "ClienteMinorista"), HttpPost("elegir"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Elegir(int libroId)
    {
        var disponibles = (await LibrosHabilitadosAsync()).Any(l => l.Id == libroId);
        if (!disponibles) { TempData["Error"] = "Este libro ya no está disponible. Elige otro."; return RedirectToAction(nameof(Libros)); }
        var anterior = await carrito.ObtenerAsync(UsuarioId);
        foreach (var detalle in anterior?.Detalles ?? []) await carrito.EliminarAsync(UsuarioId, detalle.Id);
        var error = await carrito.AgregarAsync(UsuarioId, libroId, 1);
        if (error != null) { TempData["Error"] = error; return RedirectToAction(nameof(Libros)); }
        return RedirectToAction(nameof(Confirmar));
    }

    [Authorize(AuthenticationSchemes = Esquema, Roles = "ClienteMinorista"), HttpGet("confirmar")]
    public async Task<IActionResult> Confirmar()
    {
        var detalles = (await carrito.ObtenerAsync(UsuarioId))?.Detalles;
        var detalle = detalles?.Count == 1 ? detalles[0] : null;
        if (detalle == null || detalle.Cantidad != 1 || !Habilitado(detalle.Libro))
        { TempData["Error"] = "El libro ya no está disponible para la demostración. Elige uno nuevamente."; return RedirectToAction(nameof(Libros)); }
        var libro = await catalogo.ObtenerAsync(detalle.LibroId);
        if (libro == null || !Habilitado(libro)) { TempData["Error"] = "El libro ya no está disponible. Elige otro."; return RedirectToAction(nameof(Libros)); }
        var resumen = descuentos.Calcular(libro.PrecioBase, 1, false, (await parametros.ObtenerAsync()).TasaImpuesto);
        return View(new ConfirmacionCitein(libro, resumen));
    }

    [Authorize(AuthenticationSchemes = Esquema, Roles = "ClienteMinorista"), HttpPost("comprar"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Comprar()
    {
        var usuario = await usuarios.FindByIdAsync(UsuarioId);
        if (usuario == null || !usuario.Activo || !await usuarios.IsInRoleAsync(usuario, "ClienteMinorista")) return Forbid(Esquema);
        var detalles = (await carrito.ObtenerAsync(UsuarioId))?.Detalles;
        if (detalles?.Count != 1 || detalles[0].Cantidad != 1 || !Habilitado(detalles[0].Libro))
        { TempData["Error"] = "El libro ya no está disponible para la demostración. Elige uno nuevamente."; return RedirectToAction(nameof(Libros)); }
        var tarjeta = new SimuladorTarjeta().Evaluar("4000000000000002", "12/99", "123", DateOnly.FromDateTime(DateTime.UtcNow));
        if (!tarjeta.Autorizada) { TempData["Error"] = "No fue posible autorizar el pago simulado."; return RedirectToAction(nameof(Confirmar)); }
        var resultado = await pedidos.CrearAsync(UsuarioId, false, MetodoPagoSimulado.TarjetaSimulada, usuario.NombreCompleto, tarjeta.UltimosCuatro);
        if (resultado.Error != null) { TempData["Error"] = resultado.Error; return RedirectToAction(nameof(Libros)); }
        return RedirectToAction(nameof(Resultado), new { id = resultado.Pedido!.Id });
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
        var pedido = await db.Pedidos.AsNoTracking().Include(p => p.Usuario).Include(p => p.Detalles).ThenInclude(d => d.Libro)
            .Where(p => p.Usuario.Email!.EndsWith("@visitante.citein.invalid")).OrderByDescending(p => p.Id).FirstOrDefaultAsync();
        if (pedido == null) return Json(null);
        var detalle = pedido.Detalles.First();
        return Json(new { pedido.Id, pedido.Codigo, Cliente = pedido.Usuario.NombreCompleto, Libro = detalle.Titulo,
            pedido.Total, Estado = pedido.EstadoTexto, Hora = FechaPresentacion.EnGuatemala(pedido.FechaUtc).ToString("dd/MM/yyyy HH:mm:ss"),
            Existencias = detalle.Formato == FormatoLibro.Ebook ? (int?)null : detalle.Libro.Stock, DetalleUrl = Url.Action("DetallePedido", "Administracion", new { id = pedido.Id }) });
    }
}

public record ConfirmacionCitein(Libro Libro, ResumenComercial Resumen);
