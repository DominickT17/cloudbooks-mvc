using System.Security.Claims;
using CloudBooks.Web.Servicios;
using CloudBooks.Web.Modelos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudBooks.Web.Controllers;

[Authorize(Roles = "ClienteInstitucional,AsesorVentas,Administrador")]
public class CotizacionesController(ServicioCotizaciones cotizaciones, ServicioPdfCotizaciones pdf) : Controller
{
    private string UsuarioId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    [Authorize(Roles = "ClienteInstitucional")]
    public async Task<IActionResult> Index() => View(await cotizaciones.PorClienteAsync(UsuarioId));
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Administracion(string? buscar, EstadoSolicitudCotizacion? estado)
    {
        if (!ModelState.IsValid || (estado.HasValue && !Enum.IsDefined(estado.Value))) return BadRequest();
        var todas = await cotizaciones.TodasAsync();
        ViewBag.Resumen = todas;
        var lista = todas.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(buscar)) lista = lista.Where(c => c.Codigo.Contains(buscar.Trim(), StringComparison.OrdinalIgnoreCase) || c.Solicitud.PerfilInstitucional.Institucion.Contains(buscar.Trim(), StringComparison.OrdinalIgnoreCase));
        if (estado.HasValue) lista = lista.Where(c => c.Estado == estado);
        return View("Index", lista.ToList());
    }

    public async Task<IActionResult> Detalle(int id)
    {
        var cotizacion = await cotizaciones.ObtenerAutorizadaAsync(id, UsuarioId, User.IsInRole("Administrador"));
        return cotizacion == null ? NotFound() : View(cotizacion);
    }

    public async Task<IActionResult> Pdf(int id)
    {
        var cotizacion = await cotizaciones.ObtenerAutorizadaAsync(id, UsuarioId, User.IsInRole("Administrador"));
        return cotizacion == null ? NotFound() : File(pdf.Generar(cotizacion), "application/pdf", $"Cotizacion-{cotizacion.Codigo}.pdf");
    }

    [Authorize(Roles = "ClienteInstitucional"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Responder(int id, bool aceptar)
    {
        if (!ModelState.IsValid) return BadRequest();
        var propia = await cotizaciones.ObtenerAutorizadaAsync(id, UsuarioId, false);
        if (propia == null || propia.Solicitud.UsuarioId != UsuarioId) return NotFound();
        var error = await cotizaciones.ResponderAsync(id, UsuarioId, aceptar);
        TempData[error == null ? "Exito" : "Error"] = error ?? "Respuesta registrada.";
        return RedirectToAction("Detalle", new { id });
    }
}
