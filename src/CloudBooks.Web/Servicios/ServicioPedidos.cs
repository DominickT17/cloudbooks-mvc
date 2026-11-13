using System.Data;
using System.Globalization;
using System.Text;
using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace CloudBooks.Web.Servicios;

public class ServicioPedidos(ContextoAplicacion contexto, ServicioDescuentos descuentos, ServicioParametros? parametros = null, INotificacionesPedido? notificaciones = null, IServicioNotificaciones? correos = null, ServicioComprobantePedido? comprobante = null, ILogger<ServicioPedidos>? logger = null)
{
    public async Task<(Pedido? Pedido, string? Error)> CrearAsync(string usuarioId, bool institucional, MetodoPagoSimulado metodo, string? titular = null, string? ultimosCuatro = null)
    {
        if (!Enum.IsDefined(metodo)) return (null, "Selecciona un método de pago válido.");
        try
        {
        await using var transaccion = await contexto.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var carrito = await contexto.Carritos.Include(c => c.Detalles).ThenInclude(d => d.Libro).FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);
        if (carrito == null || carrito.Detalles.Count == 0) return (null, "Tu carrito está vacío.");
        if (carrito.Detalles.Any(d => !d.Libro.Activo || d.Cantidad < 1 || (d.Libro.Formato != FormatoLibro.Ebook && d.Cantidad > d.Libro.Stock)))
            return (null, "Revisa tu carrito: un libro ya no está disponible o no tiene existencias suficientes.");
        var totalUnidades = carrito.Detalles.Sum(d => d.Cantidad);
        var configuracion = parametros == null ? new ParametrosSistema() : await parametros.ObtenerAsync();
        var tasa = configuracion.TasaImpuesto;
        var resumen = descuentos.Calcular(carrito.Detalles.Sum(d => d.Libro.PrecioBase * d.Cantidad), totalUnidades, institucional, tasa);
        var pedido = new Pedido { UsuarioId = usuarioId, FechaUtc = DateTime.UtcNow, Estado = EstadoPedido.Pendiente, MetodoPago = metodo, EstadoPago = EstadoPago.Autorizado, ResultadoPago = ResultadoPago.Aprobado, FechaPagoUtc = DateTime.UtcNow,
            UltimosCuatro = ultimosCuatro,
            Subtotal = resumen.Subtotal, DescuentoVolumen = resumen.DescuentoVolumen, DescuentoInstitucional = resumen.DescuentoInstitucional, Impuesto = resumen.Impuesto, TasaImpuesto = resumen.TasaImpuesto, Total = resumen.Total,
            Detalles = carrito.Detalles.Select(d => new DetallePedido { LibroId = d.LibroId, Titulo = d.Libro.Titulo, Formato = d.Libro.Formato, Cantidad = d.Cantidad, PrecioUnitario = d.Libro.PrecioBase, Subtotal = d.Libro.PrecioBase * d.Cantidad }).ToList() };
        var motivos = new List<string>();
        if (!institucional && carrito.Detalles.Any(d => d.Cantidad >= configuracion.CantidadRevisionMinorista))
        {
            pedido.TipoRevision |= TipoRevisionPedido.Comercial;
            motivos.Add("La cantidad de un libro alcanza el límite de revisión comercial minorista.");
        }
        foreach (var detalle in carrito.Detalles.Where(d => d.Libro.Formato != FormatoLibro.Ebook && d.Libro.Stock > 0 && (long)d.Cantidad * 100 >= (long)d.Libro.Stock * configuracion.PorcentajeRevisionInventario && d.Libro.Stock - d.Cantidad <= configuracion.UmbralStockBajo))
        {
            pedido.TipoRevision |= TipoRevisionPedido.Inventario;
            motivos.Add($"La compra consumiría {detalle.Cantidad} de {detalle.Libro.Stock} unidades de {detalle.Libro.Titulo} y dejaría {detalle.Libro.Stock - detalle.Cantidad} disponibles.");
        }
        var cliente = await contexto.Users.AsNoTracking().SingleAsync(u => u.Id == usuarioId);
        if (metodo == MetodoPagoSimulado.TarjetaSimulada && !string.IsNullOrWhiteSpace(titular))
        {
            pedido.TitularTarjeta = titular.Trim();
            static string Normalizar(string valor) => string.Join(' ', valor.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).Aggregate(new StringBuilder(), (texto, c) => texto.Append(char.ToUpperInvariant(c))).ToString();
            if (Normalizar(titular) != Normalizar(cliente.NombreCompleto)) { pedido.TipoRevision |= TipoRevisionPedido.Seguridad; motivos.Add("El titular no coincide con el nombre del cliente."); }
        }
        if (pedido.Total > configuracion.MontoRevision) { pedido.TipoRevision |= TipoRevisionPedido.Comercial; motivos.Add("El total supera el monto configurado para revisión comercial."); }
        if (motivos.Count > 0)
        {
            pedido.Estado = EstadoPedido.EnRevision;
            var descripcion = string.Join(" ", motivos);
            pedido.MotivosRevision = descripcion.Length <= 1000 ? descripcion : descripcion[..997] + "...";
            (notificaciones ?? new NotificacionesPedido(contexto)).RegistrarRevision(pedido);
        }
        else
        {
            foreach (var detalle in carrito.Detalles.Where(d => d.Libro.Formato != FormatoLibro.Ebook).OrderBy(d => d.LibroId))
            {
                var cambios = await contexto.Libros.Where(l => l.Id == detalle.LibroId && l.Activo && l.Stock >= detalle.Cantidad)
                    .ExecuteUpdateAsync(s => s.SetProperty(l => l.Stock, l => l.Stock - detalle.Cantidad));
                if (cambios == 0) return (null, $"No hay existencias suficientes de {detalle.Libro.Titulo}. Tu carrito se conserva.");
            }
            pedido.Estado = EstadoPedido.Aprobado;
            pedido.EstadoPago = EstadoPago.Confirmado;
            (notificaciones ?? new NotificacionesPedido(contexto)).RegistrarAprobacion(pedido);
        }
        contexto.Pedidos.Add(pedido);
        contexto.DetallesCarrito.RemoveRange(carrito.Detalles);
        await contexto.SaveChangesAsync();
        if (correos != null)
        {
            await EncolarPedidoAsync(pedido, cliente, pedido.Estado == EstadoPedido.Aprobado ? "aprobado" : "revision");
            if (pedido.Estado == EstadoPedido.EnRevision)
                await correos.EncolarAdministracionAsync($"pedido/{pedido.Id}/revision/admin", "revision", pedido.Codigo, $"CloudBooks Administración | Pedido {pedido.Codigo} requiere revisión", $"Pedido {pedido.Codigo}\nCliente: {cliente.NombreCompleto}\nTotal: Q{pedido.Total:0.00}\nRevisión: {pedido.TipoRevision}\nPrioridad: {(pedido.AltaPrioridad ? "Alta" : "Normal")}\n{pedido.MotivosRevision}");
            await contexto.SaveChangesAsync();
        }
        await transaccion.CommitAsync();
        return (pedido, null);
        }
        catch (Exception error) when (EsInterbloqueo(error))
        {
            // EF Core puede envolver el error SQL en varias capas de excepciones.
            return (null, "Otro pedido modificó las existencias al mismo tiempo. Tu carrito se conserva; revisa la disponibilidad e inténtalo de nuevo.");
        }
    }

    private static bool EsInterbloqueo(Exception error)
    {
        for (Exception? actual = error; actual != null; actual = actual.InnerException)
            if (actual is SqlException { Number: 1205 }) return true;
        return false;
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
            await using var transaccion = await contexto.Database.BeginTransactionAsync();
            var cancelados = await contexto.Pedidos.Where(p => p.Id == id && (p.Estado == EstadoPedido.Pendiente || p.Estado == EstadoPedido.EnRevision)).ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Estado, nuevo)
                .SetProperty(p => p.EstadoPago, p => p.EstadoPago == EstadoPago.Autorizado ? EstadoPago.Anulado : p.EstadoPago));
            if (cancelados == 0) return "Solo se pueden cancelar pedidos pendientes o en revisión.";
            var pedido = await contexto.Pedidos.FindAsync(id);
            (notificaciones ?? new NotificacionesPedido(contexto)).RegistrarCancelacion(pedido!);
            if (correos != null) await EncolarPedidoAsync(pedido!, await contexto.Users.AsNoTracking().SingleAsync(u => u.Id == pedido!.UsuarioId), "cancelado");
            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();
            return null;
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
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Estado, EstadoPedido.Aprobado).SetProperty(p => p.EstadoPago, p => p.EstadoPago == EstadoPago.Autorizado ? EstadoPago.Confirmado : p.EstadoPago));
        if (cambios == 0) return "Solo se pueden aprobar pedidos pendientes.";
        var detalles = await contexto.DetallesPedido.AsNoTracking().Where(d => d.PedidoId == id).OrderBy(d => d.LibroId).ToListAsync();
        foreach (var detalle in detalles.Where(d => d.Formato != FormatoLibro.Ebook))
        {
            var actualizado = await contexto.Libros.Where(l => l.Id == detalle.LibroId && l.Stock >= detalle.Cantidad)
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.Stock, l => l.Stock - detalle.Cantidad));
            if (actualizado == 0) return $"No hay existencias suficientes de {detalle.Titulo}; el pedido sigue pendiente.";
        }
        var pedido = await contexto.Pedidos.FindAsync(id);
        (notificaciones ?? new NotificacionesPedido(contexto)).RegistrarAprobacion(pedido!);
        if (correos != null) await EncolarPedidoAsync(pedido!, await contexto.Users.AsNoTracking().SingleAsync(u => u.Id == pedido!.UsuarioId), "aprobado");
        await contexto.SaveChangesAsync();
        await transaccion.CommitAsync();
        return null;
    }

    private async Task EncolarPedidoAsync(Pedido pedido, UsuarioAplicacion cliente, string tipo)
    {
        var asunto = tipo switch { "aprobado" => $"CloudBooks | Tu pedido {pedido.Codigo} fue confirmado", "revision" => $"CloudBooks | Estamos revisando tu pedido {pedido.Codigo}", _ => $"CloudBooks | Actualización de tu pedido {pedido.Codigo}" };
        var detalle = tipo switch
        {
            "aprobado" => $"Gracias por comprar en CloudBooks. Tu pedido {pedido.Codigo} fue aprobado y el pago es simulado.\nFecha: {pedido.FechaUtc:dd/MM/yyyy HH:mm} UTC\n\nProductos:\n" + string.Join("\n", (pedido.Detalles.Count > 0 ? pedido.Detalles : await contexto.DetallesPedido.AsNoTracking().Where(d => d.PedidoId == pedido.Id).ToListAsync()).Select(d => $"{d.Cantidad} × {d.Titulo}")) + $"\n\nSubtotal: Q{pedido.Subtotal:0.00}\nDescuento por volumen: Q{pedido.DescuentoVolumen:0.00}" + (pedido.DescuentoInstitucional > 0 ? $"\nDescuento institucional: Q{pedido.DescuentoInstitucional:0.00}" : "") + $"\nIVA: Q{pedido.Impuesto:0.00}\nTOTAL: Q{pedido.Total:0.00}\n\nConsulta tus pedidos y descarga el comprobante desde tu cuenta.",
            "revision" => $"Recibimos tu pedido {pedido.Codigo}. Nuestro equipo realizará una validación adicional; no necesitas repetir la compra. No se han reservado existencias.",
            _ => $"Tu pedido {pedido.Codigo} fue cancelado. Si existía una autorización simulada, fue anulada. No se realizó ningún cargo bancario real."
        };
        byte[]? pdf = null;
        if (tipo == "aprobado" && comprobante != null && !(cliente.Email?.EndsWith("@visitante.citein.invalid", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            try
            {
                var completo = await contexto.Pedidos.AsNoTracking().Include(p => p.Usuario).Include(p => p.Detalles)
                    .SingleAsync(p => p.Id == pedido.Id && p.Estado == EstadoPedido.Aprobado);
                var generado = comprobante.Generar(completo);
                if (generado.Length >= 5 && generado.AsSpan(0, 5).SequenceEqual("%PDF-"u8)) pdf = generado;
                else logger?.LogWarning("El comprobante del pedido {PedidoId} no es un PDF válido.", pedido.Id);
            }
            catch (Exception error) { logger?.LogWarning("No se pudo generar el comprobante del pedido {PedidoId}: {Tipo}", pedido.Id, error.GetType().Name); }
        }
        await correos!.EncolarAsync($"pedido/{pedido.Id}/{tipo}/cliente", tipo, pedido.Codigo, cliente.Email, asunto, $"Hola, {cliente.NombreCompleto}:\n\n{detalle}", pdf);
    }
}
