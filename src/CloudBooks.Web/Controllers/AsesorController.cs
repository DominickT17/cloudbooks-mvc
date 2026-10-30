using System.Security.Claims;
using CloudBooks.Web.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudBooks.Web.Controllers;

[Authorize(Roles = "AsesorVentas")]
public class AsesorController(ServicioSolicitudesCotizacion solicitudes) : Controller
{
    private string UsuarioId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    public async Task<IActionResult> Index() => View(await solicitudes.PorAsesorAsync(UsuarioId));
    public async Task<IActionResult> Detalle(int id)
    {
        var solicitud = await solicitudes.ObtenerAsesorAsync(id, UsuarioId);
        return solicitud == null ? NotFound() : View(solicitud);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> IniciarRevision(int id)
    {
        if (!await solicitudes.IniciarRevisionAsync(id, UsuarioId)) return NotFound();
        return RedirectToAction("Detalle", new { id });
    }
}
