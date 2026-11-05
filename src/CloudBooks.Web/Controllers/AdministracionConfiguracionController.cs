using CloudBooks.Web.Modelos;
using CloudBooks.Web.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudBooks.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class AdministracionConfiguracionController(ServicioParametros parametros) : Controller
{
    public async Task<IActionResult> Index() => View(await parametros.ObtenerAsync());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(ParametrosSistema modelo)
    {
        if (!ModelState.IsValid) return View("Index", modelo);
        var error = await parametros.GuardarAsync(modelo);
        if (error != null) { ModelState.AddModelError("", error); return View("Index", modelo); }
        TempData["Exito"] = "Parámetros actualizados para nuevas operaciones.";
        return RedirectToAction("Index");
    }
}
