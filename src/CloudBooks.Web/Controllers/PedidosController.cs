using System.Security.Claims;
using CloudBooks.Web.Modelos;
using CloudBooks.Web.ModelosVista;
using CloudBooks.Web.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudBooks.Web.Controllers;

[Authorize]
public class PedidosController(ServicioPedidos pedidos, ServicioCarrito carrito, ServicioDescuentos descuentos, ServicioParametros parametros, ServicioCarritoInvitado invitado) : Controller
{
    private string UsuarioId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Confirmar()
    {
        if (invitado.Cantidad(HttpContext.Session) > 0 && !await invitado.FusionarAsync(HttpContext.Session, UsuarioId))
        {
            TempData["Error"] = "Revisa los productos pendientes de tu carrito: alguno supera las existencias disponibles.";
            return RedirectToAction("Index", "Carrito");
        }
        var detalles = (await carrito.ObtenerAsync(UsuarioId))?.Detalles ?? [];
        if (detalles.Count == 0) return RedirectToAction("Index", "Carrito");
        var cantidad = detalles.Sum(d => d.Cantidad);
        var institucional = User.IsInRole("ClienteInstitucional");
        return View(new CarritoModelo { Detalles = detalles, CantidadTotal = cantidad, Institucional = institucional,
            Resumen = descuentos.Calcular(detalles.Sum(d => d.Libro.PrecioBase * d.Cantidad), cantidad, institucional, (await parametros.ObtenerAsync()).TasaImpuesto) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(MetodoPagoSimulado? metodo, string? titular)
    {
        if (invitado.Cantidad(HttpContext.Session) > 0) { TempData["Error"] = "Revisa los productos pendientes de tu carrito."; return RedirectToAction("Index", "Carrito"); }
        if (!ModelState.IsValid || metodo == null || !Enum.IsDefined(metodo.Value))
        {
            TempData["Error"] = "Selecciona un método de pago simulado válido.";
            return RedirectToAction("Confirmar");
        }
        if (metodo == MetodoPagoSimulado.TarjetaSimulada && (string.IsNullOrWhiteSpace(titular) || titular.Trim().Length > 150))
        {
            TempData["Error"] = "Indica el nombre del titular de la tarjeta simulada (máximo 150 caracteres).";
            return RedirectToAction("Confirmar");
        }
        var resultado = await pedidos.CrearAsync(UsuarioId, User.IsInRole("ClienteInstitucional"), metodo.Value, titular);
        if (resultado.Error != null) { TempData["Error"] = resultado.Error; return RedirectToAction("Index", "Carrito"); }
        TempData["Exito"] = "Tu pedido fue creado. El pago es una simulación.";
        return RedirectToAction("Detalle", new { id = resultado.Pedido!.Id });
    }

    public async Task<IActionResult> Index() => View(await pedidos.ObtenerPorUsuarioAsync(UsuarioId));
    public async Task<IActionResult> Detalle(int id)
    {
        var pedido = await pedidos.ObtenerAsync(id, UsuarioId);
        return pedido == null ? NotFound() : View(pedido);
    }
}
