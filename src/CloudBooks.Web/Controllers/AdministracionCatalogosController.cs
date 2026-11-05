using CloudBooks.Web.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudBooks.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class AdministracionCatalogosController(ServicioCatalogos catalogos) : Controller
{
    public async Task<IActionResult> Index(string tipo = "autores")
    {
        if (!ServicioCatalogos.TipoValido(tipo)) return NotFound();
        ViewBag.Tipo = tipo;
        return View(await catalogos.ListarAsync(tipo));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(string tipo, int id, string? nombre)
    {
        if (!ServicioCatalogos.TipoValido(tipo) || id < 0) return BadRequest();
        var error = await catalogos.GuardarAsync(tipo, id, nombre ?? "");
        TempData[error == null ? "Exito" : "Error"] = error ?? "Registro guardado correctamente.";
        return RedirectToAction("Index", new { tipo });
    }
}
