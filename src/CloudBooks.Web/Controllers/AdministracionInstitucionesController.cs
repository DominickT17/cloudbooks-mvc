using System.Security.Claims;
using CloudBooks.Web.Servicios;
using CloudBooks.Web.Modelos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudBooks.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class AdministracionInstitucionesController(ServicioInstituciones instituciones) : Controller
{
    public async Task<IActionResult> Index(string? buscar, EstadoSolicitudInstitucional? estado)
    {
        if (!ModelState.IsValid || (estado.HasValue && !Enum.IsDefined(estado.Value))) return BadRequest();
        var todas = await instituciones.TodasAsync();
        ViewBag.Resumen = todas;
        var lista = todas.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(buscar)) lista = lista.Where(s => s.Institucion.Contains(buscar.Trim(), StringComparison.OrdinalIgnoreCase) || s.Usuario.NombreCompleto.Contains(buscar.Trim(), StringComparison.OrdinalIgnoreCase));
        if (estado.HasValue) lista = lista.Where(s => s.Estado == estado);
        return View(lista.ToList());
    }
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
