using System.Security.Claims;
using CloudBooks.Web.ModelosVista;
using CloudBooks.Web.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudBooks.Web.Controllers;

public class CarritoController(ServicioCarrito carrito, ServicioDescuentos descuentos, ServicioParametros parametros, ServicioCarritoInvitado invitado) : Controller
{
    private string? UsuarioId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    public async Task<IActionResult> Index()
    {
        var detalles = UsuarioId == null ? new List<CloudBooks.Web.Modelos.DetalleCarrito>() : (await carrito.ObtenerAsync(UsuarioId))?.Detalles ?? [];
        detalles.AddRange(await invitado.ObtenerAsync(HttpContext.Session));
        var cantidad = detalles.Sum(d => d.Cantidad);
        var institucional = User.IsInRole("ClienteInstitucional");
        return View(new CarritoModelo { Detalles = detalles, CantidadTotal = cantidad, Institucional = institucional,
            Resumen = cantidad == 0 ? null : descuentos.Calcular(detalles.Sum(d => d.Cantidad * d.Libro.PrecioBase), cantidad, institucional, (await parametros.ObtenerAsync()).TasaImpuesto) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Agregar(int libroId, int cantidad = 1)
    {
        var error = UsuarioId == null ? await invitado.AgregarAsync(HttpContext.Session, libroId, cantidad) : await carrito.AgregarAsync(UsuarioId, libroId, cantidad);
        TempData[error == null ? "Exito" : "Error"] = error ?? "Libro agregado al carrito.";
        return RedirectToAction("Index");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Actualizar(int detalleId, int cantidad)
    {
        var error = detalleId < 0 ? await invitado.EstablecerAsync(HttpContext.Session, -detalleId, cantidad) : UsuarioId == null ? "El producto no existe en tu carrito." : await carrito.ActualizarAsync(UsuarioId, detalleId, cantidad);
        if (error != null) TempData["Error"] = error;
        return RedirectToAction("Index");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int detalleId)
    {
        if (detalleId < 0) invitado.Eliminar(HttpContext.Session, -detalleId);
        else if (UsuarioId != null) await carrito.EliminarAsync(UsuarioId, detalleId);
        TempData["Exito"] = "Producto eliminado del carrito.";
        return RedirectToAction("Index");
    }
}
