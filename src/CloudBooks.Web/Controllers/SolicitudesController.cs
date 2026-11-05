using System.Security.Claims;
using CloudBooks.Web.ModelosVista;
using CloudBooks.Web.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudBooks.Web.Controllers;

[Authorize(Roles = "ClienteInstitucional")]
public class SolicitudesController(ServicioSolicitudesCotizacion solicitudes) : Controller
{
    private string UsuarioId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    public async Task<IActionResult> Index() => View(await solicitudes.PorClienteAsync(UsuarioId));
    [HttpGet] public IActionResult Crear() => View(new SolicitudEspecialModelo());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(SolicitudEspecialModelo datos)
    {
        if (!ModelState.IsValid) return View(datos);
        var error = await solicitudes.CrearAsync(UsuarioId, datos);
        if (error != null) { ModelState.AddModelError("", error); return View(datos); }
        TempData["Exito"] = "Solicitud registrada. Si no hay asesores disponibles, quedará pendiente de asignación.";
        return RedirectToAction("Index");
    }
}
