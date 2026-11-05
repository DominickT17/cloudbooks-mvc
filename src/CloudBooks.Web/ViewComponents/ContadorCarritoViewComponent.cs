using System.Security.Claims;
using CloudBooks.Web.Datos;
using CloudBooks.Web.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloudBooks.Web.ViewComponents;

public class ContadorCarritoViewComponent(ContextoAplicacion contexto, ServicioCarritoInvitado invitado) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var usuarioId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        var cantidad = invitado.Cantidad(HttpContext.Session);
        if (usuarioId != null) cantidad += await contexto.DetallesCarrito.Where(d => d.Carrito.UsuarioId == usuarioId).SumAsync(d => (int?)d.Cantidad) ?? 0;
        return View(cantidad);
    }
}
