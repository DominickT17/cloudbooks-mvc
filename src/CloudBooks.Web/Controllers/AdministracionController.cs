using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CloudBooks.Web.Modelos;
using CloudBooks.Web.Servicios;

namespace CloudBooks.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class AdministracionController(ServicioPedidos pedidos) : Controller
{
    public IActionResult Index() => View();

    public async Task<IActionResult> Pedidos(EstadoPedido? estado)
    {
        if (!ModelState.IsValid || estado.HasValue && !Enum.IsDefined(estado.Value)) return BadRequest();
        ViewBag.Estado = estado;
        return View(await pedidos.ObtenerTodosAsync(estado));
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
