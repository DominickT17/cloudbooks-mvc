using System.Security.Claims;
using CloudBooks.Web.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudBooks.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class AdministracionInstitucionesController(ServicioInstituciones instituciones) : Controller
{
    public async Task<IActionResult> Index() => View(await instituciones.TodasAsync());
    public async Task<IActionResult> Detalle(int id)
    {
        var solicitud = await instituciones.ObtenerAsync(id);
        return solicitud == null ? NotFound() : View(solicitud);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolver(int id, bool aprobar)
    {
        if (!ModelState.IsValid) return BadRequest();
        var error = await instituciones.ResolverAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier)!, aprobar);
        TempData[error == null ? "Exito" : "Error"] = error ?? "La solicitud fue resuelta.";
        return RedirectToAction("Detalle", new { id });
    }
}
