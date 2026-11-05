using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using CloudBooks.Web.Servicios;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CloudBooks.Pruebas;

public class ServicioAsignacionAsesoresPruebas : IDisposable
{
    private readonly SqliteConnection conexion = new("Data Source=:memory:");
    private readonly ContextoAplicacion contexto;

    public ServicioAsignacionAsesoresPruebas()
    {
        conexion.Open();
        contexto = new ContextoAplicacion(new DbContextOptionsBuilder<ContextoAplicacion>().UseSqlite(conexion).Options);
        contexto.Database.EnsureCreated();
        contexto.Roles.Add(new IdentityRole { Id = "rol", Name = "AsesorVentas", NormalizedName = "ASESORVENTAS" });
        contexto.Users.AddRange(new UsuarioAplicacion { Id = "a", UserName = "asesor-a", Activo = true }, new UsuarioAplicacion { Id = "b", UserName = "asesor-b", Activo = true }, new UsuarioAplicacion { Id = "cliente", UserName = "cliente" });
        contexto.UserRoles.AddRange(new IdentityUserRole<string> { UserId = "a", RoleId = "rol" }, new IdentityUserRole<string> { UserId = "b", RoleId = "rol" });
        contexto.PerfilesInstitucionales.Add(new PerfilInstitucional { UsuarioId = "cliente", Institucion = "Institución", Nit = "123", TipoInstitucion = "Biblioteca", Telefono = "123", Direccion = "Dirección", Representante = "Cliente" });
        contexto.SaveChanges();
        contexto.ChangeTracker.Clear();
    }

    [Fact]
    public async Task EmpateEligeMenorIdYMenorCargaTienePrioridad()
    {
        var servicio = new ServicioAsignacionAsesores(contexto);
        Assert.Equal("a", await servicio.SeleccionarAsync());
        var perfil = await contexto.PerfilesInstitucionales.SingleAsync();
        contexto.SolicitudesCotizacion.Add(new SolicitudCotizacion { UsuarioId = "cliente", PerfilInstitucionalId = perfil.Id, AsesorId = "a", Titulo = "Edición", Cantidad = 1, Acabado = "Otro", Papel = "Otro", FechaRequerida = DateOnly.FromDateTime(DateTime.UtcNow), Estado = EstadoSolicitudCotizacion.EnRevision });
        await contexto.SaveChangesAsync();
        Assert.Equal("b", await servicio.SeleccionarAsync());
    }

    [Fact]
    public async Task SinAsesoresActivosNoAsigna()
    {
        await contexto.Users.Where(u => u.Id == "a" || u.Id == "b").ExecuteUpdateAsync(s => s.SetProperty(u => u.Activo, false));
        Assert.Null(await new ServicioAsignacionAsesores(contexto).SeleccionarAsync());
    }

    public void Dispose() { contexto.Dispose(); conexion.Dispose(); }
}
