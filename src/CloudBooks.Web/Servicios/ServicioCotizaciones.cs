using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using CloudBooks.Web.ModelosVista;
using Microsoft.EntityFrameworkCore;

namespace CloudBooks.Web.Servicios;

public class ServicioCotizaciones(ContextoAplicacion contexto, ServicioDescuentos descuentos)
{
    public const int DiasVigencia = 15;
    private static DateOnly Hoy => DateOnly.FromDateTime(DateTime.UtcNow);

    public async Task<string?> EmitirAsync(string asesorId, CotizacionModelo datos)
    {
        if (string.IsNullOrWhiteSpace(datos.Concepto) || datos.Concepto.Length > 200 || datos.Cantidad is < 1 or > 10000 || datos.PrecioUnitario is < 0.01m or > 1000000m || decimal.Round(datos.PrecioUnitario, 2) != datos.PrecioUnitario) return "Revisa el concepto, la cantidad y el precio con máximo dos decimales.";
        await using var transaccion = await contexto.Database.BeginTransactionAsync();
        var actualizado = await contexto.SolicitudesCotizacion.Where(s => s.Id == datos.SolicitudId && s.AsesorId == asesorId && s.Estado == EstadoSolicitudCotizacion.EnRevision)
            .ExecuteUpdateAsync(c => c.SetProperty(s => s.Estado, EstadoSolicitudCotizacion.Cotizada));
        if (actualizado != 1) return "La solicitud no está asignada a ti o no está en revisión.";
        var subtotal = datos.Cantidad * datos.PrecioUnitario;
        var resumen = descuentos.Calcular(subtotal, datos.Cantidad, true);
        contexto.Cotizaciones.Add(new Cotizacion { SolicitudCotizacionId = datos.SolicitudId, FechaUtc = DateTime.UtcNow, FechaVencimiento = Hoy.AddDays(DiasVigencia), Estado = EstadoSolicitudCotizacion.Cotizada,
            Subtotal = resumen.Subtotal, DescuentoVolumen = resumen.DescuentoVolumen, DescuentoInstitucional = resumen.DescuentoInstitucional, Impuesto = resumen.Impuesto, Total = resumen.Total,
            Detalles = [new DetalleCotizacion { Concepto = datos.Concepto.Trim(), Cantidad = datos.Cantidad, PrecioUnitario = datos.PrecioUnitario, Subtotal = subtotal }] });
        await contexto.SaveChangesAsync();
        await transaccion.CommitAsync();
        return null;
    }

    public async Task ExpirarAsync()
    {
        await using var transaccion = await contexto.Database.BeginTransactionAsync();
        var vencidas = contexto.Cotizaciones.Where(c => c.Estado == EstadoSolicitudCotizacion.Cotizada && c.FechaVencimiento < Hoy);
        var solicitudes = await vencidas.Select(c => c.SolicitudCotizacionId).ToListAsync();
        if (solicitudes.Count > 0)
        {
            await vencidas.ExecuteUpdateAsync(c => c.SetProperty(x => x.Estado, EstadoSolicitudCotizacion.Vencida));
            await contexto.SolicitudesCotizacion.Where(s => solicitudes.Contains(s.Id) && s.Estado == EstadoSolicitudCotizacion.Cotizada && contexto.Cotizaciones.Any(c => c.SolicitudCotizacionId == s.Id && c.Estado == EstadoSolicitudCotizacion.Vencida)).ExecuteUpdateAsync(c => c.SetProperty(s => s.Estado, EstadoSolicitudCotizacion.Vencida));
        }
        await transaccion.CommitAsync();
    }

    private IQueryable<Cotizacion> Consulta() => contexto.Cotizaciones.AsNoTracking().Include(c => c.Detalles).Include(c => c.Solicitud).ThenInclude(s => s.PerfilInstitucional).Include(c => c.Solicitud).ThenInclude(s => s.Usuario).Include(c => c.Solicitud).ThenInclude(s => s.Asesor);
    public async Task<List<Cotizacion>> PorClienteAsync(string usuarioId) { await ExpirarAsync(); return await Consulta().Where(c => c.Solicitud.UsuarioId == usuarioId).OrderByDescending(c => c.Id).ToListAsync(); }
    public async Task<List<Cotizacion>> TodasAsync() { await ExpirarAsync(); return await Consulta().OrderByDescending(c => c.Id).ToListAsync(); }
    public Task<int?> ObtenerIdDeSolicitudAsync(int solicitudId, string asesorId) => contexto.Cotizaciones.AsNoTracking().Where(c => c.SolicitudCotizacionId == solicitudId && c.Solicitud.AsesorId == asesorId).Select(c => (int?)c.Id).FirstOrDefaultAsync();
    public async Task<Cotizacion?> ObtenerAutorizadaAsync(int id, string usuarioId, bool administrador)
    {
        await ExpirarAsync();
        return await Consulta().FirstOrDefaultAsync(c => c.Id == id && (administrador || c.Solicitud.UsuarioId == usuarioId || c.Solicitud.AsesorId == usuarioId));
    }

    public async Task<string?> ResponderAsync(int id, string usuarioId, bool aceptar)
    {
        await ExpirarAsync();
        await using var transaccion = await contexto.Database.BeginTransactionAsync();
        var nuevo = aceptar ? EstadoSolicitudCotizacion.Aceptada : EstadoSolicitudCotizacion.Rechazada;
        var cambio = await contexto.Cotizaciones.Where(c => c.Id == id && c.Solicitud.UsuarioId == usuarioId && c.Estado == EstadoSolicitudCotizacion.Cotizada && c.FechaVencimiento >= Hoy)
            .ExecuteUpdateAsync(c => c.SetProperty(x => x.Estado, nuevo));
        if (cambio == 0) return "La cotización no está vigente o ya fue respondida.";
        var solicitudId = await contexto.Cotizaciones.Where(c => c.Id == id).Select(c => c.SolicitudCotizacionId).SingleAsync();
        var actualizada = await contexto.SolicitudesCotizacion.Where(s => s.Id == solicitudId && s.Estado == EstadoSolicitudCotizacion.Cotizada).ExecuteUpdateAsync(c => c.SetProperty(s => s.Estado, nuevo));
        if (actualizada != 1) throw new InvalidOperationException("No se pudo actualizar el estado de la solicitud.");
        await transaccion.CommitAsync();
        return null;
    }
}
