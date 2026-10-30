using CloudBooks.Web.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudBooks.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class AdministracionInventarioController(ServicioInventario inventario, ServicioParametros parametros) : Controller
{
    public async Task<IActionResult> Index(string? buscar) { ViewBag.Umbral = (await parametros.ObtenerAsync()).UmbralStockBajo; return View(await inventario.ListarAsync(buscar)); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Actualizar(int id, int stockOriginal, int stockNuevo)
    {
        if (!ModelState.IsValid) return BadRequest();
        var error = await inventario.ActualizarAsync(id, stockOriginal, stockNuevo);
        TempData[error == null ? "Exito" : "Error"] = error ?? "Existencias actualizadas.";
        return RedirectToAction("Index");
    }
}
