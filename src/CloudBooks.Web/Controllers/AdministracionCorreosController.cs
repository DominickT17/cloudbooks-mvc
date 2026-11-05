using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloudBooks.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class AdministracionCorreosController(ContextoAplicacion contexto) : Controller
{
    public async Task<IActionResult> Index() => View(await contexto.CorreosPendientes.AsNoTracking().OrderByDescending(c => c.Id).Take(200).ToListAsync());
    public async Task<IActionResult> Detalle(int id)
    {
        var correo = await contexto.CorreosPendientes.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id);
        return correo == null ? NotFound() : View(correo);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reintentar(int id)
    {
        var limite = DateTime.UtcNow.AddHours(-23);
        var cambios = await contexto.CorreosPendientes.Where(c => c.Id == id && c.Estado == EstadoCorreo.Fallido && c.PrimerIntentoUtc >= limite)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Estado, EstadoCorreo.ReintentoPendiente).SetProperty(c => c.ProximoIntentoUtc, (DateTime?)null).SetProperty(c => c.Intentos, 0));
        TempData[cambios == 1 ? "Exito" : "Error"] = cambios == 1 ? "Se programó el reintento." : "No se puede reintentar: el resultado es incierto o ya venció la ventana segura.";
        return RedirectToAction(nameof(Detalle), new { id });
    }
}
