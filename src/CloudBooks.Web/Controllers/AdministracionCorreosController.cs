using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CloudBooks.Web.Servicios;

namespace CloudBooks.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class AdministracionCorreosController(ContextoAplicacion contexto, IWebHostEnvironment entorno, IConfiguration configuracion) : Controller
{
    public async Task<IActionResult> Index() => View(await contexto.CorreosPendientes.AsNoTracking().OrderByDescending(c => c.Id).Take(200)
        .Select(c => new CorreoPendiente { Id = c.Id, CreadoUtc = c.CreadoUtc, Tipo = c.Tipo, Referencia = c.Referencia,
            Destinatario = c.Destinatario, Asunto = c.Asunto, Estado = c.Estado, Intentos = c.Intentos }).ToListAsync());
    [HttpGet]
    public IActionResult Ejemplos() => entorno.IsDevelopment()
        ? View(EjemplosCorreo.Crear(configuracion["Correo:UrlBase"], configuracion["Correo:LogoUrl"]))
        : NotFound();
    public async Task<IActionResult> Detalle(int id)
    {
        var correo = await contexto.CorreosPendientes.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id);
        return correo == null ? NotFound() : View(correo);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reintentar(int id)
    {
        var limite = DateTime.UtcNow.AddHours(-23);
        var candidatos = contexto.CorreosPendientes.Where(c => c.Id == id && c.Estado == EstadoCorreo.Fallido && c.PrimerIntentoUtc >= limite);
        if (configuracion.GetValue<bool>("CiteinDemo:Enabled") && ConfiguracionCorreo.UsarResend(configuracion))
        {
            var sesion = ConfiguracionCorreo.MarcaRealCitein(configuracion);
            candidatos = candidatos.Where(c => c.Proveedor == sesion);
        }
        var cambios = await candidatos.ExecuteUpdateAsync(s => s.SetProperty(c => c.Estado, EstadoCorreo.ReintentoPendiente).SetProperty(c => c.ProximoIntentoUtc, (DateTime?)null).SetProperty(c => c.Intentos, 0));
        TempData[cambios == 1 ? "Exito" : "Error"] = cambios == 1 ? "Se programó el reintento." : "No se puede reintentar: pertenece a otra sesión, el resultado es incierto o venció la ventana segura.";
        return RedirectToAction(nameof(Detalle), new { id });
    }
}
