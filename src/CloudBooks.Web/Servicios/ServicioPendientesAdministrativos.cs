using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using Microsoft.EntityFrameworkCore;
namespace CloudBooks.Web.Servicios;
public record PendientesAdministrativos(int Pedidos, int Instituciones, int Cotizaciones);
public class ServicioPendientesAdministrativos(ContextoAplicacion contexto)
{
    public async Task<PendientesAdministrativos> ObtenerAsync()
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        return new PendientesAdministrativos(
            await contexto.Pedidos.CountAsync(p => p.Estado == EstadoPedido.Pendiente || p.Estado == EstadoPedido.EnRevision),
            await contexto.SolicitudesInstitucionales.CountAsync(s => s.Estado == EstadoSolicitudInstitucional.Pendiente),
            await contexto.Cotizaciones.CountAsync(c => c.Estado == EstadoSolicitudCotizacion.Cotizada && c.FechaVencimiento >= hoy));
    }
}
