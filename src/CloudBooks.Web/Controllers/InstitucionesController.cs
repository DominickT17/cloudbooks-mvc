using System.Security.Claims;
using CloudBooks.Web.ModelosVista;
using CloudBooks.Web.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudBooks.Web.Controllers;

[Authorize]
public class InstitucionesController(ServicioInstituciones instituciones) : Controller
{
    private string UsuarioId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> MisSolicitudes() => View(await instituciones.MiasAsync(UsuarioId));
    [Authorize(Roles = "ClienteMinorista"), HttpGet] public IActionResult Solicitar() => View(new SolicitudInstitucionalModelo());

    [Authorize(Roles = "ClienteMinorista"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Solicitar(SolicitudInstitucionalModelo datos)
    {
        if (!ModelState.IsValid) return View(datos);
        var error = await instituciones.SolicitarAsync(UsuarioId, datos);
        if (error != null) { ModelState.AddModelError("", error); return View(datos); }
        TempData["Exito"] = "Tu solicitud fue recibida y está pendiente de revisión.";
        return RedirectToAction("MisSolicitudes");
    }
}
