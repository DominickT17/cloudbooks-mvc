using System.Data;
using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CloudBooks.Web.Servicios;

public record UsuarioAdministrativo(string Id, string Nombre, string Correo, string? Telefono, bool Activo, bool TienePerfilInstitucional, List<string> Roles);

public class ServicioUsuarios(ContextoAplicacion contexto, UserManager<UsuarioAplicacion> usuarios)
{
    private static readonly string[] RolesPermitidos = ["ClienteMinorista", "ClienteInstitucional", "AsesorVentas"];

    public async Task<List<UsuarioAdministrativo>> ListarAsync(string? buscar)
    {
        var consulta = contexto.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(buscar)) { var texto = buscar.Trim(); consulta = consulta.Where(u => u.NombreCompleto.Contains(texto) || (u.Email != null && u.Email.Contains(texto))); }
        var personas = await consulta.OrderBy(u => u.NombreCompleto).Select(u => new { u.Id, u.NombreCompleto, u.Email, u.PhoneNumber, u.Activo, Perfil = contexto.PerfilesInstitucionales.Any(p => p.UsuarioId == u.Id) }).ToListAsync();
        var ids = personas.Select(u => u.Id).ToArray();
        var roles = await (from asignacion in contexto.UserRoles join rol in contexto.Roles on asignacion.RoleId equals rol.Id where ids.Contains(asignacion.UserId) select new { asignacion.UserId, rol.Name }).ToListAsync();
        return personas.Select(u => new UsuarioAdministrativo(u.Id, u.NombreCompleto, u.Email ?? "", u.PhoneNumber, u.Activo, u.Perfil, roles.Where(r => r.UserId == u.Id && r.Name != null).Select(r => r.Name!).ToList())).ToList();
    }

    public async Task<string?> CambiarRolAsync(string administradorId, string usuarioId, string rolNuevo)
    {
        var administrador = await usuarios.FindByIdAsync(administradorId);
        if (administrador == null || !await usuarios.IsInRoleAsync(administrador, "Administrador")) return "No tienes autorización para cambiar roles.";
        if (administradorId == usuarioId) return "No puedes cambiar tu propio rol.";
        if (!RolesPermitidos.Contains(rolNuevo)) return "Selecciona un rol permitido.";
        await using var transaccion = await contexto.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var usuario = await usuarios.FindByIdAsync(usuarioId);
        if (usuario == null) return "La cuenta no existe.";
        if (await usuarios.IsInRoleAsync(usuario, "Administrador")) return "No se pueden cambiar roles de administradores desde esta pantalla.";
        if (rolNuevo == "ClienteInstitucional" && !await contexto.PerfilesInstitucionales.AnyAsync(p => p.UsuarioId == usuarioId)) return "La cuenta necesita un perfil institucional aprobado.";
        foreach (var rol in RolesPermitidos)
            if (rol != rolNuevo && await usuarios.IsInRoleAsync(usuario, rol)) Comprobar(await usuarios.RemoveFromRoleAsync(usuario, rol));
        if (!await usuarios.IsInRoleAsync(usuario, rolNuevo)) Comprobar(await usuarios.AddToRoleAsync(usuario, rolNuevo));
        Comprobar(await usuarios.UpdateSecurityStampAsync(usuario));
        await transaccion.CommitAsync();
        return null;
    }

    private static void Comprobar(IdentityResult resultado)
    {
        if (!resultado.Succeeded) throw new InvalidOperationException(string.Join("; ", resultado.Errors.Select(e => e.Description)));
    }
}
