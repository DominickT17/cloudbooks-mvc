using System.Security.Claims;
using CloudBooks.Web.Servicios;
using CloudBooks.Web.ModelosVista;
using CloudBooks.Web.Modelos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudBooks.Web.Controllers;

[Authorize(Roles = "AsesorVentas")]
public class AsesorController(ServicioSolicitudesCotizacion solicitudes, ServicioCotizaciones cotizaciones) : Controller
{
    private string UsuarioId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    public async Task<IActionResult> Index() => View(await solicitudes.PorAsesorAsync(UsuarioId));
    public async Task<IActionResult> Detalle(int id)
    {
        var solicitud = await solicitudes.ObtenerAsesorAsync(id, UsuarioId);
        if (solicitud == null) return NotFound();
        ViewBag.CotizacionId = await cotizaciones.ObtenerIdDeSolicitudAsync(id, UsuarioId);
        return View(solicitud);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> IniciarRevision(int id)
    {
        if (!await solicitudes.IniciarRevisionAsync(id, UsuarioId)) return NotFound();
        return RedirectToAction("Detalle", new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Cotizar(int id)
    {
        var solicitud = await solicitudes.ObtenerAsesorAsync(id, UsuarioId);
        if (solicitud == null || solicitud.Estado != EstadoSolicitudCotizacion.EnRevision) return NotFound();
        return View(new CotizacionModelo { SolicitudId = id, Concepto = solicitud.Titulo, Cantidad = solicitud.Cantidad });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cotizar(CotizacionModelo modelo)
    {
        if (!ModelState.IsValid) return View(modelo);
        var error = await cotizaciones.EmitirAsync(UsuarioId, modelo);
        if (error != null) { ModelState.AddModelError("", error); return View(modelo); }
        TempData["Exito"] = "Cotización emitida correctamente.";
        return RedirectToAction("Detalle", new { id = modelo.SolicitudId });
    }
}
