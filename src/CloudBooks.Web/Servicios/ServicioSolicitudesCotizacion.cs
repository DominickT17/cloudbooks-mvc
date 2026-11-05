using System.Data;
using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using CloudBooks.Web.ModelosVista;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CloudBooks.Web.Servicios;

public class ServicioSolicitudesCotizacion(ContextoAplicacion contexto, UserManager<UsuarioAplicacion> usuarios, ServicioAsignacionAsesores asignacion, IServicioNotificaciones? correos = null)
{
    public async Task<string?> CrearAsync(string usuarioId, SolicitudEspecialModelo datos)
    {
        var usuario = await usuarios.FindByIdAsync(usuarioId);
        if (usuario == null || !await usuarios.IsInRoleAsync(usuario, "ClienteInstitucional")) return "Solo un cliente institucional puede realizar esta solicitud.";
        if (datos.FechaRequerida == null || datos.FechaRequerida < DateOnly.FromDateTime(DateTime.UtcNow)) return "La fecha requerida no puede ser anterior a hoy.";
        if (datos.Cantidad is < 1 or > 10000 || string.IsNullOrWhiteSpace(datos.Titulo) || !new[] { "Tapa blanda", "Tapa dura", "Otro" }.Contains(datos.Acabado) || !new[] { "Bond", "Couché", "Reciclado", "Otro" }.Contains(datos.Papel)) return "Revisa los datos de la solicitud.";
        await using var transaccion = await contexto.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var perfil = await contexto.PerfilesInstitucionales.SingleOrDefaultAsync(p => p.UsuarioId == usuarioId);
        if (perfil == null) return "Tu cuenta no tiene un perfil institucional aprobado.";
        var asesorId = await asignacion.SeleccionarAsync();
        var solicitud = new SolicitudCotizacion { UsuarioId = usuarioId, PerfilInstitucionalId = perfil.Id, AsesorId = asesorId, Titulo = datos.Titulo.Trim(), Cantidad = datos.Cantidad, Acabado = datos.Acabado, Papel = datos.Papel, FechaRequerida = datos.FechaRequerida.Value, Observaciones = datos.Observaciones?.Trim(), FechaUtc = DateTime.UtcNow, Estado = EstadoSolicitudCotizacion.Solicitada };
        contexto.SolicitudesCotizacion.Add(solicitud);
        await contexto.SaveChangesAsync();
        if (correos != null)
        {
            await correos.EncolarAsync($"solicitud/{solicitud.Id}/creada/cliente", "solicitud_comercial", solicitud.Codigo, usuario.Email, "CloudBooks | Solicitud comercial recibida", $"Hola, {usuario.NombreCompleto}:\n\nRecibimos tu solicitud {solicitud.Codigo} para {solicitud.Titulo}. Nuestro equipo dará seguimiento a tu gestión.");
            await correos.EncolarAdministracionAsync($"solicitud/{solicitud.Id}/creada/admin", "solicitud_comercial", solicitud.Codigo, "CloudBooks | Nueva solicitud comercial", $"Se registró {solicitud.Codigo} para {solicitud.Titulo}.");
            if (asesorId != null) await NotificarAsignacionAsync(solicitud, asesorId);
            await contexto.SaveChangesAsync();
        }
        await transaccion.CommitAsync();
        return null;
    }

    public Task<List<SolicitudCotizacion>> PorClienteAsync(string usuarioId) => Consulta().Where(s => s.UsuarioId == usuarioId).OrderByDescending(s => s.Id).ToListAsync();
    public Task<List<SolicitudCotizacion>> PorAsesorAsync(string asesorId) => Consulta().Where(s => s.AsesorId == asesorId).OrderByDescending(s => s.Id).ToListAsync();
    public Task<List<SolicitudCotizacion>> TodasAsync() => Consulta().OrderByDescending(s => s.Id).ToListAsync();
    public Task<SolicitudCotizacion?> ObtenerAsesorAsync(int id, string asesorId) => Consulta().FirstOrDefaultAsync(s => s.Id == id && s.AsesorId == asesorId);
    public Task<SolicitudCotizacion?> ObtenerAsync(int id) => Consulta().FirstOrDefaultAsync(s => s.Id == id);

    public async Task<bool> IniciarRevisionAsync(int id, string asesorId) => await contexto.SolicitudesCotizacion.Where(s => s.Id == id && s.AsesorId == asesorId && s.Estado == EstadoSolicitudCotizacion.Solicitada)
        .ExecuteUpdateAsync(c => c.SetProperty(s => s.Estado, EstadoSolicitudCotizacion.EnRevision)) == 1;

    public async Task<string?> AsignarPendienteAsync(int id)
    {
        await using var transaccion = await contexto.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var solicitud = await contexto.SolicitudesCotizacion.SingleOrDefaultAsync(s => s.Id == id && s.AsesorId == null && s.Estado == EstadoSolicitudCotizacion.Solicitada);
        if (solicitud == null) return "La solicitud ya está asignada o no se encuentra pendiente.";
        var asesorId = await asignacion.SeleccionarAsync();
        if (asesorId == null) return "Aún no hay asesores activos disponibles.";
        solicitud.AsesorId = asesorId;
        if (correos != null) await NotificarAsignacionAsync(solicitud, asesorId);
        await contexto.SaveChangesAsync();
        await transaccion.CommitAsync();
        return null;
    }

    private async Task NotificarAsignacionAsync(SolicitudCotizacion solicitud, string asesorId)
    {
        var asesor = await usuarios.FindByIdAsync(asesorId);
        await correos!.EncolarAsync($"solicitud/{solicitud.Id}/asignada/{asesorId}", "solicitud_asignada", solicitud.Codigo, asesor?.Email, "CloudBooks | Solicitud comercial asignada", $"Se te asignó la solicitud {solicitud.Codigo} para {solicitud.Titulo}.");
    }

    private IQueryable<SolicitudCotizacion> Consulta() => contexto.SolicitudesCotizacion.AsNoTracking().Include(s => s.PerfilInstitucional).Include(s => s.Usuario).Include(s => s.Asesor);
}
