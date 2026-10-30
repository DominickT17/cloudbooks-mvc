using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using Microsoft.EntityFrameworkCore;

namespace CloudBooks.Web.Servicios;

public class ServicioAsignacionAsesores(ContextoAplicacion contexto)
{
    public async Task<string?> SeleccionarAsync()
    {
        var ahora = DateTimeOffset.UtcNow;
        return await (from usuario in contexto.Users
            join asignacion in contexto.UserRoles on usuario.Id equals asignacion.UserId
            join rol in contexto.Roles on asignacion.RoleId equals rol.Id
            where rol.NormalizedName == "ASESORVENTAS" && usuario.Activo && (usuario.LockoutEnd == null || usuario.LockoutEnd <= ahora)
            orderby contexto.SolicitudesCotizacion.Count(s => s.AsesorId == usuario.Id && (s.Estado == EstadoSolicitudCotizacion.Solicitada || s.Estado == EstadoSolicitudCotizacion.EnRevision || s.Estado == EstadoSolicitudCotizacion.Cotizada)), usuario.Id
            select usuario.Id).FirstOrDefaultAsync();
    }
}
