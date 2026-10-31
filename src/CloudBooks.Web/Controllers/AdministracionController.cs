using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CloudBooks.Web.Modelos;
using CloudBooks.Web.Servicios;

namespace CloudBooks.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class AdministracionController(ServicioPedidos pedidos, ServicioMetricas metricas) : Controller
{
    public async Task<IActionResult> Index() => View(await metricas.ObtenerAsync());

    public async Task<IActionResult> Pedidos(EstadoPedido? estado, bool? revision)
    {
        if (!ModelState.IsValid || estado.HasValue && !Enum.IsDefined(estado.Value)) return BadRequest();
        ViewBag.Estado = estado;
        ViewBag.Revision = revision;
        var resultados = await pedidos.ObtenerTodosAsync(estado);
        return View(revision == true ? resultados.Where(p => p.Estado == EstadoPedido.EnRevision).ToList() : resultados);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarRevisado(int id)
    {
        var error = await pedidos.MarcarRevisadoAsync(id);
        TempData[error == null ? "Exito" : "Error"] = error ?? "Revisión registrada. Ya puedes aprobar el pedido.";
        return RedirectToAction("DetallePedido", new { id });
    }

    public async Task<IActionResult> DetallePedido(int id)
    {
        var pedido = await pedidos.ObtenerAsync(id);
        return pedido == null ? NotFound() : View(pedido);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id, EstadoPedido estado)
    {
        if (!ModelState.IsValid || !Enum.IsDefined(estado)) return BadRequest();
        var error = await pedidos.CambiarEstadoAsync(id, estado);
        TempData[error == null ? "Exito" : "Error"] = error ?? "Estado del pedido actualizado.";
        return RedirectToAction("DetallePedido", new { id });
    }
}
