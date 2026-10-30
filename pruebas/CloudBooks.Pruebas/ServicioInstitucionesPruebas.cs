using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using CloudBooks.Web.ModelosVista;
using CloudBooks.Web.Servicios;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CloudBooks.Pruebas;

public class ServicioInstitucionesPruebas : IDisposable
{
    private readonly SqliteConnection conexion = new("Data Source=:memory:");
    private readonly ServiceProvider proveedor;
    private readonly IServiceScope alcance;
    private readonly ContextoAplicacion contexto;
    private readonly UserManager<UsuarioAplicacion> usuarios;
    private readonly ServicioInstituciones servicio;

    public ServicioInstitucionesPruebas()
    {
        conexion.Open();
        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AddDbContext<ContextoAplicacion>(o => o.UseSqlite(conexion));
        servicios.AddIdentityCore<UsuarioAplicacion>().AddRoles<IdentityRole>().AddEntityFrameworkStores<ContextoAplicacion>();
        proveedor = servicios.BuildServiceProvider();
        alcance = proveedor.CreateScope();
        contexto = alcance.ServiceProvider.GetRequiredService<ContextoAplicacion>();
        contexto.Database.EnsureCreated();
        usuarios = alcance.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacion>>();
        var roles = alcance.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var rol in new[] { "ClienteMinorista", "ClienteInstitucional", "Administrador", "AsesorVentas" }) roles.CreateAsync(new IdentityRole(rol)).GetAwaiter().GetResult();
        foreach (var (id, rol) in new[] { ("cliente", "ClienteMinorista"), ("admin", "Administrador"), ("otro", "ClienteMinorista"), ("asesor", "AsesorVentas") })
        {
            var usuario = new UsuarioAplicacion { Id = id, UserName = id, NombreCompleto = id };
            Assert.True(usuarios.CreateAsync(usuario).GetAwaiter().GetResult().Succeeded);
            Assert.True(usuarios.AddToRoleAsync(usuario, rol).GetAwaiter().GetResult().Succeeded);
        }
        servicio = new ServicioInstituciones(contexto, usuarios);
    }

    private static SolicitudInstitucionalModelo Datos() => new() { Institucion = "Biblioteca Horizonte", Nit = "123", TipoInstitucion = "Biblioteca", Telefono = "123", Direccion = "Zona 1", Representante = "Cliente" };

    [Fact]
    public async Task MinoristaSolicitaUnaVezYUsuarioComunNoPuedeResolver()
    {
        Assert.Null(await servicio.SolicitarAsync("cliente", Datos()));
        Assert.NotNull(await servicio.SolicitarAsync("cliente", Datos()));
        Assert.NotNull(await servicio.ResolverAsync(1, "otro", true));
        Assert.Equal(EstadoSolicitudInstitucional.Pendiente, (await contexto.SolicitudesInstitucionales.AsNoTracking().SingleAsync()).Estado);
    }

    [Fact]
    public async Task AdministradorApruebaPerfilYReemplazaRolComercial()
    {
        Assert.Null(await servicio.SolicitarAsync("cliente", Datos()));
        Assert.Null(await servicio.ResolverAsync(1, "admin", true));
        var cliente = (await usuarios.FindByIdAsync("cliente"))!;
        Assert.True(await usuarios.IsInRoleAsync(cliente, "ClienteInstitucional"));
        Assert.False(await usuarios.IsInRoleAsync(cliente, "ClienteMinorista"));
        Assert.Equal("Biblioteca Horizonte", (await contexto.PerfilesInstitucionales.AsNoTracking().SingleAsync()).Institucion);
        Assert.NotNull(await servicio.SolicitarAsync("cliente", Datos()));
        Assert.NotNull(await servicio.ResolverAsync(1, "admin", true));
    }

    [Fact]
    public async Task RechazoNoAsignaRol()
    {
        Assert.Null(await servicio.SolicitarAsync("cliente", Datos()));
        Assert.Null(await servicio.ResolverAsync(1, "admin", false));
        Assert.False(await usuarios.IsInRoleAsync((await usuarios.FindByIdAsync("cliente"))!, "ClienteInstitucional"));
        Assert.Equal(EstadoSolicitudInstitucional.Rechazada, (await contexto.SolicitudesInstitucionales.AsNoTracking().SingleAsync()).Estado);
    }

    [Fact]
    public async Task SolicitudEspecialRequiereRolYAsesorAjenoNoPuedeIniciarRevision()
    {
        var especiales = new ServicioSolicitudesCotizacion(contexto, usuarios, new ServicioAsignacionAsesores(contexto));
        var datos = new SolicitudEspecialModelo { Titulo = "Edición", Cantidad = 20, Acabado = "Tapa blanda", Papel = "Bond", FechaRequerida = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10) };
        Assert.NotNull(await especiales.CrearAsync("cliente", datos));
        Assert.Null(await servicio.SolicitarAsync("cliente", Datos()));
        Assert.Null(await servicio.ResolverAsync(1, "admin", true));
        Assert.Null(await especiales.CrearAsync("cliente", datos));
        Assert.False(await especiales.IniciarRevisionAsync(1, "otro"));
        Assert.True(await especiales.IniciarRevisionAsync(1, "asesor"));
    }

    [Fact]
    public async Task AdministradorNoPuedeAprobarSuPropiaSolicitud()
    {
        var cliente = (await usuarios.FindByIdAsync("cliente"))!;
        Assert.True((await usuarios.AddToRoleAsync(cliente, "Administrador")).Succeeded);
        Assert.Null(await servicio.SolicitarAsync("cliente", Datos()));
        Assert.NotNull(await servicio.ResolverAsync(1, "cliente", true));
        Assert.Equal(EstadoSolicitudInstitucional.Pendiente, (await contexto.SolicitudesInstitucionales.AsNoTracking().SingleAsync()).Estado);
    }

    public void Dispose() { alcance.Dispose(); proveedor.Dispose(); conexion.Dispose(); }
}
