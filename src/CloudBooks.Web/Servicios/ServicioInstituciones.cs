using System.Data;
using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using CloudBooks.Web.ModelosVista;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CloudBooks.Web.Servicios;

public class ServicioInstituciones(ContextoAplicacion contexto, UserManager<UsuarioAplicacion> usuarios)
{
    public async Task<string?> SolicitarAsync(string usuarioId, SolicitudInstitucionalModelo datos)
    {
        var usuario = await usuarios.FindByIdAsync(usuarioId);
        if (usuario == null || !await usuarios.IsInRoleAsync(usuario, "ClienteMinorista") || await usuarios.IsInRoleAsync(usuario, "ClienteInstitucional")) return "Solo un cliente minorista puede solicitar una cuenta institucional.";
        if (await contexto.SolicitudesInstitucionales.AnyAsync(s => s.UsuarioId == usuarioId && s.Estado == EstadoSolicitudInstitucional.Pendiente)) return "Ya tienes una solicitud pendiente.";
        contexto.SolicitudesInstitucionales.Add(new SolicitudInstitucional { UsuarioId = usuarioId, Institucion = datos.Institucion.Trim(), Nit = datos.Nit.Trim(), TipoInstitucion = datos.TipoInstitucion.Trim(), Telefono = datos.Telefono.Trim(), Direccion = datos.Direccion.Trim(), Representante = datos.Representante.Trim(), FechaUtc = DateTime.UtcNow, Estado = EstadoSolicitudInstitucional.Pendiente });
        try { await contexto.SaveChangesAsync(); }
        catch (DbUpdateException error) when (error.InnerException is Microsoft.Data.SqlClient.SqlException sql && (sql.Number == 2601 || sql.Number == 2627))
        {
            return "Ya tienes una solicitud pendiente.";
        }
        return null;
    }

    public Task<List<SolicitudInstitucional>> MiasAsync(string usuarioId) => contexto.SolicitudesInstitucionales.AsNoTracking().Where(s => s.UsuarioId == usuarioId).OrderByDescending(s => s.Id).ToListAsync();
    public Task<List<SolicitudInstitucional>> TodasAsync() => contexto.SolicitudesInstitucionales.AsNoTracking().Include(s => s.Usuario).OrderByDescending(s => s.Id).ToListAsync();
    public Task<SolicitudInstitucional?> ObtenerAsync(int id) => contexto.SolicitudesInstitucionales.AsNoTracking().Include(s => s.Usuario).FirstOrDefaultAsync(s => s.Id == id);

    public async Task<string?> ResolverAsync(int id, string administradorId, bool aprobar)
    {
        var administrador = await usuarios.FindByIdAsync(administradorId);
        if (administrador == null || !await usuarios.IsInRoleAsync(administrador, "Administrador")) return "No tienes autorización para resolver solicitudes.";
        await using var transaccion = await contexto.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var solicitud = await contexto.SolicitudesInstitucionales.FirstOrDefaultAsync(s => s.Id == id);
        if (solicitud == null || solicitud.Estado != EstadoSolicitudInstitucional.Pendiente) return "La solicitud no está pendiente.";
        if (solicitud.UsuarioId == administradorId) return "No puedes resolver tu propia solicitud.";
        if (aprobar)
        {
            var usuario = await usuarios.FindByIdAsync(solicitud.UsuarioId);
            if (usuario == null) return "No existe la cuenta solicitante.";
            var perfil = await contexto.PerfilesInstitucionales.FirstOrDefaultAsync(p => p.UsuarioId == usuario.Id);
            if (perfil == null) { perfil = new PerfilInstitucional { UsuarioId = usuario.Id }; contexto.PerfilesInstitucionales.Add(perfil); }
            perfil.Institucion = solicitud.Institucion; perfil.Nit = solicitud.Nit; perfil.TipoInstitucion = solicitud.TipoInstitucion;
            perfil.Telefono = solicitud.Telefono; perfil.Direccion = solicitud.Direccion; perfil.Representante = solicitud.Representante;
            if (!await usuarios.IsInRoleAsync(usuario, "ClienteInstitucional")) Comprobar(await usuarios.AddToRoleAsync(usuario, "ClienteInstitucional"));
            if (await usuarios.IsInRoleAsync(usuario, "ClienteMinorista")) Comprobar(await usuarios.RemoveFromRoleAsync(usuario, "ClienteMinorista"));
            Comprobar(await usuarios.UpdateSecurityStampAsync(usuario));
        }
        solicitud.Estado = aprobar ? EstadoSolicitudInstitucional.Aprobada : EstadoSolicitudInstitucional.Rechazada;
        await contexto.SaveChangesAsync();
        await transaccion.CommitAsync();
        return null;
    }

    private static void Comprobar(IdentityResult resultado)
    {
        if (!resultado.Succeeded) throw new InvalidOperationException(string.Join("; ", resultado.Errors.Select(e => e.Description)));
    }
}
