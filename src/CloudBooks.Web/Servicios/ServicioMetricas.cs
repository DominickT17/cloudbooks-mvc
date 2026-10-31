using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using CloudBooks.Web.ModelosVista;
using Microsoft.EntityFrameworkCore;

namespace CloudBooks.Web.Servicios;

public class ServicioMetricas(ContextoAplicacion contexto, ServicioParametros? parametros = null)
{
    public async Task<DashboardModelo> ObtenerAsync()
    {
        var porEstado = await contexto.Pedidos.AsNoTracking().GroupBy(p => p.Estado).Select(g => new { Estado = g.Key, Cantidad = g.Count() }).ToListAsync();
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var umbral = parametros == null ? ParametrosSistema.UmbralInicial : (await parametros.ObtenerAsync()).UmbralStockBajo;
        var clientes = from asignacion in contexto.UserRoles join rol in contexto.Roles on asignacion.RoleId equals rol.Id
            where rol.NormalizedName == "CLIENTEMINORISTA" || rol.NormalizedName == "CLIENTEINSTITUCIONAL" select asignacion.UserId;
        var institucionales = from asignacion in contexto.UserRoles join rol in contexto.Roles on asignacion.RoleId equals rol.Id
            where rol.NormalizedName == "CLIENTEINSTITUCIONAL" select asignacion.UserId;
        var masVendidos = await contexto.DetallesPedido.AsNoTracking().Where(d => d.Pedido.Estado == EstadoPedido.Aprobado || d.Pedido.Estado == EstadoPedido.Enviado)
            .GroupBy(d => new { d.LibroId, d.Libro.Titulo }).Select(g => new { g.Key.LibroId, g.Key.Titulo, Unidades = g.Sum(d => d.Cantidad) })
            .OrderByDescending(x => x.Unidades).ThenBy(x => x.Titulo).Take(5).ToListAsync();
        return new DashboardModelo
        {
            Ventas = await contexto.Pedidos.Where(p => p.Estado == EstadoPedido.Aprobado || p.Estado == EstadoPedido.Enviado).SumAsync(p => (decimal?)p.Total) ?? 0m,
            Pedidos = porEstado.Sum(e => e.Cantidad),
            Pendientes = porEstado.FirstOrDefault(e => e.Estado == EstadoPedido.Pendiente)?.Cantidad ?? 0,
            Aprobados = porEstado.FirstOrDefault(e => e.Estado == EstadoPedido.Aprobado)?.Cantidad ?? 0,
            Enviados = porEstado.FirstOrDefault(e => e.Estado == EstadoPedido.Enviado)?.Cantidad ?? 0,
            Cancelados = porEstado.FirstOrDefault(e => e.Estado == EstadoPedido.Cancelado)?.Cantidad ?? 0,
            SolicitudesInstitucionalesPendientes = await contexto.SolicitudesInstitucionales.CountAsync(s => s.Estado == EstadoSolicitudInstitucional.Pendiente),
            CotizacionesPendientes = await contexto.Cotizaciones.CountAsync(c => c.Estado == EstadoSolicitudCotizacion.Cotizada && c.FechaVencimiento >= hoy),
            Clientes = await clientes.Distinct().CountAsync(), Institucionales = await institucionales.Distinct().CountAsync(),
            StockBajo = await contexto.Libros.AsNoTracking().Where(l => l.Activo && l.Formato != FormatoLibro.Ebook && l.Stock <= umbral).OrderBy(l => l.Stock).ThenBy(l => l.Titulo).ToListAsync(),
            MasVendidos = masVendidos.Select(x => new LibroVendido(x.LibroId, x.Titulo, x.Unidades)).ToList()
        };
    }
}
