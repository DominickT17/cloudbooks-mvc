using CloudBooks.Web.ModelosVista;
using CloudBooks.Web.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudBooks.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class AdministracionCatalogoController(ServicioAdministracionCatalogo catalogo) : Controller
{
    public async Task<IActionResult> Index(string? buscar, string? estado) => View(await catalogo.ListarAsync(buscar, estado));
    [HttpGet] public async Task<IActionResult> Crear() { var modelo = new LibroAdministracionModelo { AnioEdicion = DateTime.UtcNow.Year }; await catalogo.PrepararAsync(modelo); return View("Formulario", modelo); }
    [HttpGet] public async Task<IActionResult> Editar(int id) { var modelo = await catalogo.ObtenerAsync(id); if (modelo == null) return NotFound(); await catalogo.PrepararAsync(modelo); return View("Formulario", modelo); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(LibroAdministracionModelo modelo)
    {
        if (ModelState.IsValid)
        {
            var error = await catalogo.GuardarAsync(modelo);
            if (error == null) { TempData["Exito"] = "Libro guardado correctamente."; return RedirectToAction("Index"); }
            ModelState.AddModelError("", error);
        }
        await catalogo.PrepararAsync(modelo);
        return View("Formulario", modelo);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id, bool activo)
    {
        if (!ModelState.IsValid || !await catalogo.CambiarActivoAsync(id, activo)) return NotFound();
        TempData["Exito"] = activo ? "Libro reactivado." : "Libro desactivado.";
        return RedirectToAction("Index");
    }
}
