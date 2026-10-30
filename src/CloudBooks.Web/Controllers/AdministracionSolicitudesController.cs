using CloudBooks.Web.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudBooks.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class AdministracionSolicitudesController(ServicioSolicitudesCotizacion solicitudes) : Controller
{
    public async Task<IActionResult> Index() => View(await solicitudes.TodasAsync());
    public async Task<IActionResult> Detalle(int id)
    {
        var solicitud = await solicitudes.ObtenerAsync(id);
        return solicitud == null ? NotFound() : View(solicitud);
    }
}
