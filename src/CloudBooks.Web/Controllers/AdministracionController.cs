using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CloudBooks.Web.Modelos;
using CloudBooks.Web.Servicios;

namespace CloudBooks.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class AdministracionController(ServicioPedidos pedidos, ServicioMetricas metricas) : Controller
{
    public async Task<IActionResult> Index() => View(await metricas.ObtenerAsync());

    public async Task<IActionResult> Pedidos(EstadoPedido? estado, bool? revision, TipoRevisionPedido? tipo, string? buscar, DateOnly? fecha)
    {
        if (!ModelState.IsValid || estado.HasValue && !Enum.IsDefined(estado.Value)) return BadRequest();
        ViewBag.Estado = estado;
        ViewBag.Revision = revision;
        if (tipo.HasValue && tipo.Value != TipoRevisionPedido.Seguridad && tipo.Value != TipoRevisionPedido.Comercial && tipo.Value != TipoRevisionPedido.Inventario) return BadRequest();
        var todos = await pedidos.ObtenerTodosAsync(null);
        ViewBag.Resumen = todos;
        var resultados = todos.AsEnumerable();
        if (estado.HasValue) resultados = resultados.Where(p => p.Estado == estado);
        if (revision == true) resultados = resultados.Where(p => p.Estado == EstadoPedido.EnRevision);
        if (tipo.HasValue) resultados = resultados.Where(p => (p.TipoRevision & tipo.Value) != 0);
        if (!string.IsNullOrWhiteSpace(buscar)) resultados = resultados.Where(p => p.Usuario.NombreCompleto.Contains(buscar.Trim(), StringComparison.OrdinalIgnoreCase) || (p.Usuario.Email?.Contains(buscar.Trim(), StringComparison.OrdinalIgnoreCase) ?? false) || p.Codigo.Contains(buscar.Trim(), StringComparison.OrdinalIgnoreCase));
        if (fecha.HasValue) resultados = resultados.Where(p => DateOnly.FromDateTime(p.FechaUtc) == fecha.Value);
        return View(resultados.ToList());
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
