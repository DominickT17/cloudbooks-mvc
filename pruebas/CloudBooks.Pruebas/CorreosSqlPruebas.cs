using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using CloudBooks.Web.Servicios;
using CloudBooks.Web.ModelosVista;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace CloudBooks.Pruebas;

public class SqlTemporalFactAttribute : FactAttribute
{
    public SqlTemporalFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CLOUDBOOKS_QA_SQL"))) Skip = "Requiere una base SQL Server temporal configurada para QA.";
    }
}

public class CorreosSqlPruebas
{
    [SqlTemporalFact]
    public async Task PedidoYColaPersistenAunqueFalleElProveedor()
    {
        var cadena = Environment.GetEnvironmentVariable("CLOUDBOOKS_QA_SQL")!;
        if (!new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(cadena).InitialCatalog.StartsWith("CloudBooks_QA_", StringComparison.Ordinal))
            throw new InvalidOperationException("Se requiere una base temporal de QA.");
        await using var contexto = new ContextoAplicacion(new DbContextOptionsBuilder<ContextoAplicacion>().UseSqlServer(cadena).Options);
        var libro = await contexto.Libros.FirstAsync(l => l.Activo && l.Formato != FormatoLibro.Ebook);
        libro.Stock = 50;
        var usuario = new UsuarioAplicacion { Id = Guid.NewGuid().ToString(), UserName = "qa-" + Guid.NewGuid(), NombreCompleto = "Cliente QA", Email = "cliente@example.test" };
        contexto.Users.Add(usuario);
        contexto.Carritos.Add(new Carrito { Usuario = usuario, Detalles = [new DetalleCarrito { LibroId = libro.Id, Cantidad = 1 }] });
        await contexto.SaveChangesAsync();
        contexto.ChangeTracker.Clear();
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var correos = new ServicioNotificaciones(contexto, config);
        var resultado = await new ServicioPedidos(contexto, new ServicioDescuentos(), correos: correos).CrearAsync(usuario.Id, false, MetodoPagoSimulado.TarjetaSimulada, "Cliente QA");
        Assert.Null(resultado.Error);
        contexto.ChangeTracker.Clear();
        Assert.Equal(EstadoPedido.Aprobado, (await contexto.Pedidos.AsNoTracking().SingleAsync(p => p.Id == resultado.Pedido!.Id)).Estado);
        Assert.Equal(49, (await contexto.Libros.AsNoTracking().SingleAsync(l => l.Id == libro.Id)).Stock);
        Assert.True(await new ProcesadorCorreos(contexto, new ProveedorFallido()).ProcesarUnoAsync());
        Assert.Equal(EstadoCorreo.ReintentoPendiente, (await contexto.CorreosPendientes.AsNoTracking().SingleAsync()).Estado);
        Assert.Equal(EstadoPago.Confirmado, (await contexto.Pedidos.AsNoTracking().SingleAsync(p => p.Id == resultado.Pedido!.Id)).EstadoPago);
        await contexto.CorreosPendientes.ExecuteUpdateAsync(s => s.SetProperty(c => c.ProximoIntentoUtc, DateTime.UtcNow.AddMinutes(-1)));
        Assert.True(await new ProcesadorCorreos(contexto, new ProveedorCorreoSimulado()).ProcesarUnoAsync());
        Assert.Equal(EstadoCorreo.Simulado, (await contexto.CorreosPendientes.AsNoTracking().SingleAsync()).Estado);

        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AddDbContext<ContextoAplicacion>(o => o.UseSqlServer(cadena));
        servicios.AddIdentityCore<UsuarioAplicacion>().AddRoles<IdentityRole>().AddEntityFrameworkStores<ContextoAplicacion>();
        using var proveedor = servicios.BuildServiceProvider();
        using var alcance = proveedor.CreateScope();
        var db = alcance.ServiceProvider.GetRequiredService<ContextoAplicacion>();
        var usuarios = alcance.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacion>>();
        var clienteDemo = (await usuarios.FindByEmailAsync("cliente@cloudbooks.local"))!;
        var admin = (await usuarios.FindByEmailAsync("admin.yeisson@cloudbooks.local"))!;
        var asesor = (await usuarios.FindByEmailAsync("asesor@cloudbooks.local"))!;
        var avisos = new ServicioNotificaciones(db, config);
        var instituciones = new ServicioInstituciones(db, usuarios, avisos);
        var datos = new SolicitudInstitucionalModelo { Institucion = "Biblioteca QA", Nit = "123", TipoInstitucion = "Biblioteca", Telefono = "12345678", Direccion = "Zona 1", Representante = "Cliente" };
        Assert.Null(await instituciones.SolicitarAsync(clienteDemo.Id, datos));
        var solicitud = await db.SolicitudesInstitucionales.AsNoTracking().SingleAsync(s => s.UsuarioId == clienteDemo.Id);
        Assert.Null(await instituciones.ResolverAsync(solicitud.Id, admin.Id, true));
        var especiales = new ServicioSolicitudesCotizacion(db, usuarios, new ServicioAsignacionAsesores(db), avisos);
        Assert.Null(await especiales.CrearAsync(clienteDemo.Id, new SolicitudEspecialModelo { Titulo = "Edición QA", Cantidad = 5, Acabado = "Tapa blanda", Papel = "Bond", FechaRequerida = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10) }));
        var especial = await db.SolicitudesCotizacion.AsNoTracking().SingleAsync(s => s.UsuarioId == clienteDemo.Id);
        Assert.Equal(asesor.Id, especial.AsesorId);
        Assert.True(await especiales.IniciarRevisionAsync(especial.Id, asesor.Id));
        var cotizaciones = new ServicioCotizaciones(db, new ServicioDescuentos(), correos: avisos);
        Assert.Null(await cotizaciones.EmitirAsync(asesor.Id, new CotizacionModelo { SolicitudId = especial.Id, Concepto = "Edición QA", Cantidad = 5, PrecioUnitario = 100m }));
        Assert.NotNull(await cotizaciones.EmitirAsync(asesor.Id, new CotizacionModelo { SolicitudId = especial.Id, Concepto = "Edición QA", Cantidad = 5, PrecioUnitario = 100m }));
        Assert.Equal(1, await db.CorreosPendientes.CountAsync(c => c.Tipo == "cotizacion_emitida" && c.Destinatario == clienteDemo.Email));
        Assert.True(await db.AvisosAdministrativos.AnyAsync(a => a.ClaveEvento.StartsWith("institucion/")));
    }

    private class ProveedorFallido : IProveedorCorreo
    {
        public Task<ResultadoEntrega> EnviarAsync(CorreoPendiente correo, CancellationToken cancelacion) => Task.FromException<ResultadoEntrega>(new InvalidOperationException());
    }
}
