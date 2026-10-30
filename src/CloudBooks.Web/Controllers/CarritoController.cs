using System.Security.Claims;
using CloudBooks.Web.ModelosVista;
using CloudBooks.Web.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudBooks.Web.Controllers;

[Authorize]
public class CarritoController(ServicioCarrito carrito, ServicioDescuentos descuentos, ServicioParametros parametros) : Controller
{
    private string UsuarioId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index()
    {
        var detalles = (await carrito.ObtenerAsync(UsuarioId))?.Detalles ?? [];
        var cantidad = detalles.Sum(d => d.Cantidad);
        var institucional = User.IsInRole("ClienteInstitucional");
        return View(new CarritoModelo { Detalles = detalles, CantidadTotal = cantidad, Institucional = institucional,
            Resumen = cantidad == 0 ? null : descuentos.Calcular(detalles.Sum(d => d.Cantidad * d.Libro.PrecioBase), cantidad, institucional, (await parametros.ObtenerAsync()).TasaImpuesto) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Agregar(int libroId, int cantidad = 1)
    {
        var error = await carrito.AgregarAsync(UsuarioId, libroId, cantidad);
        TempData[error == null ? "Exito" : "Error"] = error ?? "Libro agregado al carrito.";
        return RedirectToAction("Index");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Actualizar(int detalleId, int cantidad)
    {
        var error = await carrito.ActualizarAsync(UsuarioId, detalleId, cantidad);
        if (error != null) TempData["Error"] = error;
        return RedirectToAction("Index");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int detalleId)
    {
        await carrito.EliminarAsync(UsuarioId, detalleId);
        return RedirectToAction("Index");
    }
}
