using CloudBooks.Web.Servicios;
using CloudBooks.Web.Modelos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudBooks.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class AdministracionSolicitudesController(ServicioSolicitudesCotizacion solicitudes) : Controller
{
    public async Task<IActionResult> Index(string? buscar, EstadoSolicitudCotizacion? estado)
    {
        if (!ModelState.IsValid || (estado.HasValue && !Enum.IsDefined(estado.Value))) return BadRequest();
        var todas = await solicitudes.TodasAsync();
        ViewBag.Resumen = todas;
        var lista = todas.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(buscar)) lista = lista.Where(s => s.Titulo.Contains(buscar.Trim(), StringComparison.OrdinalIgnoreCase) || s.PerfilInstitucional.Institucion.Contains(buscar.Trim(), StringComparison.OrdinalIgnoreCase) || s.Codigo.Contains(buscar.Trim(), StringComparison.OrdinalIgnoreCase));
        if (estado.HasValue) lista = lista.Where(s => s.Estado == estado);
        return View(lista.ToList());
    }
    public async Task<IActionResult> Detalle(int id)
    {
        var solicitud = await solicitudes.ObtenerAsync(id);
        return solicitud == null ? NotFound() : View(solicitud);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Asignar(int id)
    {
        var error = await solicitudes.AsignarPendienteAsync(id);
        TempData[error == null ? "Exito" : "Error"] = error ?? "Asesor asignado.";
        return RedirectToAction("Detalle", new { id });
    }
}
