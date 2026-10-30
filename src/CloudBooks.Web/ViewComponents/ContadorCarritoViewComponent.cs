using System.Security.Claims;
using CloudBooks.Web.Datos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloudBooks.Web.ViewComponents;

public class ContadorCarritoViewComponent(ContextoAplicacion contexto) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var usuarioId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        var cantidad = usuarioId == null ? 0 : await contexto.DetallesCarrito.Where(d => d.Carrito.UsuarioId == usuarioId).SumAsync(d => (int?)d.Cantidad) ?? 0;
        return View(cantidad);
    }
}
