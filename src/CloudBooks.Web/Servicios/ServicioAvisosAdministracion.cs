using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using Microsoft.EntityFrameworkCore;

namespace CloudBooks.Web.Servicios;

public class ServicioAvisosAdministracion(ContextoAplicacion contexto)
{
    public Task<List<NotificacionPedido>> ObtenerAsync() => contexto.NotificacionesPedido.AsNoTracking()
        .Include(n => n.Pedido).ThenInclude(p => p.Usuario)
        .Where(n => n.Destinatario == "Administración")
        .OrderByDescending(n => n.Id).Take(100).ToListAsync();
}
