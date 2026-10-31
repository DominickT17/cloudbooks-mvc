using System.Data;
using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using Microsoft.EntityFrameworkCore;

namespace CloudBooks.Web.Servicios;

public class ServicioPedidos(ContextoAplicacion contexto, ServicioDescuentos descuentos, ServicioParametros? parametros = null, INotificacionesPedido? notificaciones = null)
{
    public async Task<(Pedido? Pedido, string? Error)> CrearAsync(string usuarioId, bool institucional, MetodoPagoSimulado metodo, string? titular = null)
    {
        if (!Enum.IsDefined(metodo)) return (null, "Selecciona un método de pago válido.");
        await using var transaccion = await contexto.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var carrito = await contexto.Carritos.Include(c => c.Detalles).ThenInclude(d => d.Libro).FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);
        if (carrito == null || carrito.Detalles.Count == 0) return (null, "Tu carrito está vacío.");
        if (carrito.Detalles.Any(d => !d.Libro.Activo || d.Cantidad < 1 || (d.Libro.Formato != FormatoLibro.Ebook && d.Cantidad > d.Libro.Stock)))
            return (null, "Revisa tu carrito: un libro ya no está disponible o no tiene existencias suficientes.");
        var totalUnidades = carrito.Detalles.Sum(d => d.Cantidad);
        var configuracion = parametros == null ? new ParametrosSistema() : await parametros.ObtenerAsync();
        var tasa = configuracion.TasaImpuesto;
        var resumen = descuentos.Calcular(carrito.Detalles.Sum(d => d.Libro.PrecioBase * d.Cantidad), totalUnidades, institucional, tasa);
        var pedido = new Pedido { UsuarioId = usuarioId, FechaUtc = DateTime.UtcNow, Estado = EstadoPedido.Pendiente, MetodoPago = metodo,
            Subtotal = resumen.Subtotal, DescuentoVolumen = resumen.DescuentoVolumen, DescuentoInstitucional = resumen.DescuentoInstitucional, Impuesto = resumen.Impuesto, TasaImpuesto = resumen.TasaImpuesto, Total = resumen.Total,
            Detalles = carrito.Detalles.Select(d => new DetallePedido { LibroId = d.LibroId, Titulo = d.Libro.Titulo, Formato = d.Libro.Formato, Cantidad = d.Cantidad, PrecioUnitario = d.Libro.PrecioBase, Subtotal = d.Libro.PrecioBase * d.Cantidad }).ToList() };
        var motivos = new List<string>();
        if (carrito.Detalles.Any(d => d.Cantidad >= 10)) motivos.Add("Diez o más unidades de un mismo libro.");
        pedido.AltaPrioridad = carrito.Detalles.Any(d => d.Cantidad >= 30);
        var cliente = await contexto.Users.AsNoTracking().SingleAsync(u => u.Id == usuarioId);
        if (metodo == MetodoPagoSimulado.TarjetaSimulada && !string.IsNullOrWhiteSpace(titular))
        {
            pedido.TitularTarjeta = titular.Trim();
            static string Normalizar(string valor) => string.Join(' ', valor.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();
            if (Normalizar(titular) != Normalizar(cliente.NombreCompleto)) motivos.Add("El titular no coincide con el nombre del cliente.");
        }
        if (pedido.Total > configuracion.MontoRevision) motivos.Add("El total supera el monto configurado para revisión.");
        if (motivos.Count > 0)
        {
            pedido.Estado = EstadoPedido.EnRevision;
            pedido.MotivosRevision = string.Join(" ", motivos);
            (notificaciones ?? new NotificacionesPedido(contexto)).RegistrarRevision(pedido);
        }
        contexto.Pedidos.Add(pedido);
        contexto.DetallesCarrito.RemoveRange(carrito.Detalles);
        await contexto.SaveChangesAsync();
        await transaccion.CommitAsync();
        return (pedido, null);
    }

    public Task<List<Pedido>> ObtenerPorUsuarioAsync(string usuarioId) => contexto.Pedidos.AsNoTracking().Where(p => p.UsuarioId == usuarioId).OrderByDescending(p => p.Id).ToListAsync();
    public Task<Pedido?> ObtenerAsync(int id, string? usuarioId = null) => contexto.Pedidos.AsNoTracking().Include(p => p.Detalles).ThenInclude(d => d.Libro).Include(p => p.Usuario)
        .FirstOrDefaultAsync(p => p.Id == id && (usuarioId == null || p.UsuarioId == usuarioId));
    public Task<List<Pedido>> ObtenerTodosAsync(EstadoPedido? estado) => contexto.Pedidos.AsNoTracking().Include(p => p.Usuario)
        .Where(p => estado == null || p.Estado == estado).OrderByDescending(p => p.Id).ToListAsync();

    public async Task<string?> CambiarEstadoAsync(int id, EstadoPedido nuevo)
    {
        if (nuevo == EstadoPedido.Aprobado) return await AprobarAsync(id);
        if (nuevo == EstadoPedido.Cancelado || nuevo == EstadoPedido.CanceladoPorSeguridad)
        {
            var cancelados = await contexto.Pedidos.Where(p => p.Id == id && (p.Estado == EstadoPedido.Pendiente || p.Estado == EstadoPedido.EnRevision)).ExecuteUpdateAsync(s => s.SetProperty(p => p.Estado, nuevo));
            return cancelados == 1 ? null : "Solo se pueden cancelar pedidos pendientes o en revisión.";
        }
        var anterior = nuevo switch { EstadoPedido.Enviado => EstadoPedido.Aprobado, _ => (EstadoPedido?)null };
        if (anterior == null) return "Transición de estado no válida.";
        var cambios = await contexto.Pedidos.Where(p => p.Id == id && p.Estado == anterior).ExecuteUpdateAsync(s => s.SetProperty(p => p.Estado, nuevo));
        return cambios == 1 ? null : "El pedido no se encuentra en un estado que permita esta acción.";
    }

    public async Task<string?> MarcarRevisadoAsync(int id)
    {
        var cambios = await contexto.Pedidos.Where(p => p.Id == id && p.Estado == EstadoPedido.EnRevision && !p.Revisado).ExecuteUpdateAsync(s => s.SetProperty(p => p.Revisado, true));
        return cambios == 1 ? null : "El pedido no está pendiente de revisión.";
    }

    private async Task<string?> AprobarAsync(int id)
    {
        await using var transaccion = await contexto.Database.BeginTransactionAsync();
        var cambios = await contexto.Pedidos.Where(p => p.Id == id && (p.Estado == EstadoPedido.Pendiente || (p.Estado == EstadoPedido.EnRevision && p.Revisado)))
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Estado, EstadoPedido.Aprobado));
        if (cambios == 0) return "Solo se pueden aprobar pedidos pendientes.";
        var detalles = await contexto.DetallesPedido.AsNoTracking().Where(d => d.PedidoId == id).OrderBy(d => d.LibroId).ToListAsync();
        foreach (var detalle in detalles.Where(d => d.Formato != FormatoLibro.Ebook))
        {
            var actualizado = await contexto.Libros.Where(l => l.Id == detalle.LibroId && l.Stock >= detalle.Cantidad)
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.Stock, l => l.Stock - detalle.Cantidad));
            if (actualizado == 0) return $"No hay existencias suficientes de {detalle.Titulo}; el pedido sigue pendiente.";
        }
        await transaccion.CommitAsync();
        return null;
    }
}
