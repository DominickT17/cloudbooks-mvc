using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using Microsoft.EntityFrameworkCore;

namespace CloudBooks.Web.Servicios;

public class ServicioAsignacionAsesores(ContextoAplicacion contexto)
{
    public async Task<string?> SeleccionarAsync()
    {
        var ahora = DateTimeOffset.UtcNow;
        var asesores = await (from usuario in contexto.Users
            join asignacion in contexto.UserRoles on usuario.Id equals asignacion.UserId
            join rol in contexto.Roles on asignacion.RoleId equals rol.Id
            where rol.NormalizedName == "ASESORVENTAS" && usuario.Activo
            select new { usuario.Id, usuario.LockoutEnd, Carga = contexto.SolicitudesCotizacion.Count(s => s.AsesorId == usuario.Id && (s.Estado == EstadoSolicitudCotizacion.Solicitada || s.Estado == EstadoSolicitudCotizacion.EnRevision || s.Estado == EstadoSolicitudCotizacion.Cotizada)) }).ToListAsync();
        return asesores.Where(a => a.LockoutEnd == null || a.LockoutEnd <= ahora).OrderBy(a => a.Carga).ThenBy(a => a.Id, StringComparer.Ordinal).Select(a => a.Id).FirstOrDefault();
    }
}
