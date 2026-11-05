using System.Security.Claims;
using CloudBooks.Web.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudBooks.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class AdministracionUsuariosController(ServicioUsuarios usuarios) : Controller
{
    public async Task<IActionResult> Index(string? buscar, string? rol, bool? activo, string? tipo)
    {
        var resultados = await usuarios.ListarAsync(buscar);
        if (!string.IsNullOrWhiteSpace(rol)) resultados = resultados.Where(u => u.Roles.Contains(rol)).ToList();
        if (activo.HasValue) resultados = resultados.Where(u => u.Activo == activo.Value).ToList();
        if (tipo == "institucional") resultados = resultados.Where(u => u.TienePerfilInstitucional).ToList();
        if (tipo == "minorista") resultados = resultados.Where(u => u.Roles.Contains("ClienteMinorista")).ToList();
        return View(resultados);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarRol(string usuarioId, string rol)
    {
        if (!ModelState.IsValid) return BadRequest();
        var error = await usuarios.CambiarRolAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!, usuarioId, rol);
        TempData[error == null ? "Exito" : "Error"] = error ?? "Rol actualizado. La cuenta debe iniciar sesión nuevamente.";
        return RedirectToAction("Index");
    }
}
