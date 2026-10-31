using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using CloudBooks.Web.Servicios;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CloudBooks.Pruebas;

public class ServicioPedidosPruebas : IDisposable
{
    private readonly SqliteConnection conexion = new("Data Source=:memory:");
    private readonly ContextoAplicacion contexto;
    private readonly ServicioPedidos servicio;

    public ServicioPedidosPruebas()
    {
        conexion.Open();
        contexto = new ContextoAplicacion(new DbContextOptionsBuilder<ContextoAplicacion>().UseSqlite(conexion).Options);
        contexto.Database.EnsureCreated();
        var usuario = new UsuarioAplicacion { Id = "cliente", UserName = "cliente", NombreCompleto = "Cliente" };
        var categoria = new Categoria { Nombre = "Narrativa" };
        var editorial = new Editorial { Nombre = "Editorial" };
        var fisico = new Libro { ISBN = "123", Titulo = "Libro físico", Sinopsis = "Descripción", Editorial = editorial, Categoria = categoria, PrecioBase = 100m, Stock = 10, Formato = FormatoLibro.TapaBlanda };
        var digital = new Libro { ISBN = "456", Titulo = "Libro digital", Sinopsis = "Descripción", Editorial = editorial, Categoria = categoria, PrecioBase = 50m, Stock = 0, Formato = FormatoLibro.Ebook };
        contexto.Users.Add(usuario);
        contexto.Carritos.Add(new Carrito { Usuario = usuario, Detalles = [new DetalleCarrito { Libro = fisico, Cantidad = 2 }, new DetalleCarrito { Libro = digital, Cantidad = 1 }] });
        contexto.SaveChanges();
        contexto.ChangeTracker.Clear();
        servicio = new ServicioPedidos(contexto, new ServicioDescuentos());
    }

    [Fact]
    public async Task DiezUnidadesDeUnLibroActivanRevisionSinPrioridadAlta()
    {
        var detalle = await contexto.DetallesCarrito.Include(d => d.Libro).SingleAsync(d => d.Libro.ISBN == "456");
        detalle.Cantidad = 10;
        await contexto.SaveChangesAsync();
        contexto.ChangeTracker.Clear();
        var pedido = (await servicio.CrearAsync("cliente", false, MetodoPagoSimulado.TransferenciaSimulada)).Pedido!;
        Assert.Equal(EstadoPedido.EnRevision, pedido.Estado);
        Assert.False(pedido.AltaPrioridad);
        Assert.Contains("Diez", pedido.MotivosRevision!);
    }

    [Fact]
    public async Task CantidadAltaRequiereRevisionYRegistraAvisosAntesDeAprobar()
    {
        var detalle = await contexto.DetallesCarrito.Include(d => d.Libro).SingleAsync(d => d.Libro.ISBN == "456");
        detalle.Cantidad = 30;
        await contexto.SaveChangesAsync();
        contexto.ChangeTracker.Clear();
        var pedido = (await servicio.CrearAsync("cliente", false, MetodoPagoSimulado.TransferenciaSimulada)).Pedido!;
        Assert.Equal(EstadoPedido.EnRevision, pedido.Estado);
        Assert.True(pedido.AltaPrioridad);
        Assert.Equal(2, await contexto.NotificacionesPedido.CountAsync(n => n.PedidoId == pedido.Id));
        Assert.NotNull(await servicio.CambiarEstadoAsync(pedido.Id, EstadoPedido.Aprobado));
        Assert.Equal(10, (await contexto.Libros.AsNoTracking().SingleAsync(l => l.ISBN == "123")).Stock);
        Assert.Null(await servicio.MarcarRevisadoAsync(pedido.Id));
        Assert.Null(await servicio.CambiarEstadoAsync(pedido.Id, EstadoPedido.Aprobado));
        Assert.NotNull(await servicio.CambiarEstadoAsync(pedido.Id, EstadoPedido.Aprobado));
        Assert.Equal(8, (await contexto.Libros.AsNoTracking().SingleAsync(l => l.ISBN == "123")).Stock);
    }

    [Fact]
    public async Task TitularDistintoPermiteCancelarPorSeguridadSinDescontar()
    {
        var pedido = (await servicio.CrearAsync("cliente", false, MetodoPagoSimulado.TarjetaSimulada, "Otra persona")).Pedido!;
        Assert.Equal(EstadoPedido.EnRevision, pedido.Estado);
        Assert.Contains("titular", pedido.MotivosRevision!, StringComparison.OrdinalIgnoreCase);
        Assert.Null(await servicio.CambiarEstadoAsync(pedido.Id, EstadoPedido.CanceladoPorSeguridad));
        Assert.NotNull(await servicio.CambiarEstadoAsync(pedido.Id, EstadoPedido.Aprobado));
        Assert.Equal(10, (await contexto.Libros.AsNoTracking().SingleAsync(l => l.ISBN == "123")).Stock);
    }

    [Fact]
    public async Task MontoConfiguradoMarcaRevisionYTitularIgualNoGeneraAlertaDeNombre()
    {
        var parametros = new ServicioParametros(contexto);
        Assert.Null(await parametros.GuardarAsync(new ParametrosSistema { MontoRevision = 100m }));
        var pedido = (await new ServicioPedidos(contexto, new ServicioDescuentos(), parametros).CrearAsync("cliente", false, MetodoPagoSimulado.TarjetaSimulada, " cliente ")).Pedido!;
        Assert.Equal(EstadoPedido.EnRevision, pedido.Estado);
        Assert.Contains("total", pedido.MotivosRevision!, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("titular", pedido.MotivosRevision!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CrearPendienteConservaStockYGuardaPrecios()
    {
        var resultado = await servicio.CrearAsync("cliente", false, MetodoPagoSimulado.TarjetaSimulada);
        Assert.Null(resultado.Error);
        Assert.Equal(EstadoPedido.Pendiente, resultado.Pedido!.Estado);
        Assert.Equal(250m, resultado.Pedido.Subtotal);
        Assert.Equal(280m, resultado.Pedido.Total);
        Assert.Equal(10, (await contexto.Libros.AsNoTracking().SingleAsync(l => l.ISBN == "123")).Stock);
        Assert.Empty(await contexto.DetallesCarrito.ToListAsync());
    }

    [Fact]
    public async Task AprobarDescuentaFisicoUnaVezYSinCambiarEbook()
    {
        var pedido = (await servicio.CrearAsync("cliente", false, MetodoPagoSimulado.TarjetaSimulada)).Pedido!;
        Assert.Null(await servicio.CambiarEstadoAsync(pedido.Id, EstadoPedido.Aprobado));
        Assert.NotNull(await servicio.CambiarEstadoAsync(pedido.Id, EstadoPedido.Aprobado));
        Assert.Equal(8, (await contexto.Libros.AsNoTracking().SingleAsync(l => l.ISBN == "123")).Stock);
        Assert.Equal(0, (await contexto.Libros.AsNoTracking().SingleAsync(l => l.ISBN == "456")).Stock);
        Assert.Equal(EstadoPedido.Aprobado, (await contexto.Pedidos.AsNoTracking().SingleAsync()).Estado);
    }

    [Fact]
    public async Task StockInsuficienteRevierteTodoYConservaPendiente()
    {
        var pedido = (await servicio.CrearAsync("cliente", false, MetodoPagoSimulado.TarjetaSimulada)).Pedido!;
        await contexto.Libros.Where(l => l.ISBN == "123").ExecuteUpdateAsync(s => s.SetProperty(l => l.Stock, 1));
        Assert.NotNull(await servicio.CambiarEstadoAsync(pedido.Id, EstadoPedido.Aprobado));
        Assert.Equal(1, (await contexto.Libros.AsNoTracking().SingleAsync(l => l.ISBN == "123")).Stock);
        Assert.Equal(EstadoPedido.Pendiente, (await contexto.Pedidos.AsNoTracking().SingleAsync()).Estado);
    }

    [Fact]
    public async Task CancelarPendienteNoDescuentaYNoPermiteAprobar()
    {
        var pedido = (await servicio.CrearAsync("cliente", false, MetodoPagoSimulado.TransferenciaSimulada)).Pedido!;
        Assert.Null(await servicio.CambiarEstadoAsync(pedido.Id, EstadoPedido.Cancelado));
        Assert.NotNull(await servicio.CambiarEstadoAsync(pedido.Id, EstadoPedido.Aprobado));
        Assert.Equal(10, (await contexto.Libros.AsNoTracking().SingleAsync(l => l.ISBN == "123")).Stock);
    }

    [Fact]
    public async Task UsuarioNoPuedeConsultarPedidoAjeno()
    {
        var pedido = (await servicio.CrearAsync("cliente", false, MetodoPagoSimulado.TarjetaSimulada)).Pedido!;
        Assert.Null(await servicio.ObtenerAsync(pedido.Id, "otro"));
        Assert.NotNull(await servicio.ObtenerAsync(pedido.Id, "cliente"));
    }

    [Fact]
    public async Task DesactivarLibroConservaHistorialDelPedido()
    {
        var pedido = (await servicio.CrearAsync("cliente", false, MetodoPagoSimulado.TarjetaSimulada)).Pedido!;
        var libroId = (await contexto.Libros.AsNoTracking().SingleAsync(l => l.ISBN == "123")).Id;
        var catalogo = new ServicioAdministracionCatalogo(contexto, null!);
        Assert.True(await catalogo.CambiarActivoAsync(libroId, false));
        var historico = await servicio.ObtenerAsync(pedido.Id, "cliente");
        Assert.Contains(historico!.Detalles, d => d.LibroId == libroId && d.Titulo == "Libro físico" && d.PrecioUnitario == 100m);
        Assert.False((await contexto.Libros.AsNoTracking().SingleAsync(l => l.Id == libroId)).Activo);
    }

    [Fact]
    public async Task TasaConfiguradaSeGuardaEnPedidoYNoCambiaElHistorico()
    {
        var parametros = new ServicioParametros(contexto);
        Assert.Null(await parametros.GuardarAsync(new ParametrosSistema { TasaImpuesto = 0.16m, UmbralStockBajo = 3, DiasVigencia = 30 }));
        var resultado = await new ServicioPedidos(contexto, new ServicioDescuentos(), parametros).CrearAsync("cliente", false, MetodoPagoSimulado.TarjetaSimulada);
        Assert.Equal(40m, resultado.Pedido!.Impuesto);
        Assert.Equal(0.16m, resultado.Pedido.TasaImpuesto);
        Assert.Null(await parametros.GuardarAsync(new ParametrosSistema { TasaImpuesto = 0m, UmbralStockBajo = 5, DiasVigencia = 15 }));
        Assert.Equal(0.16m, (await contexto.Pedidos.AsNoTracking().SingleAsync()).TasaImpuesto);
        Assert.NotNull(await parametros.GuardarAsync(new ParametrosSistema { TasaImpuesto = 1.1m, UmbralStockBajo = 5, DiasVigencia = 15 }));
        Assert.NotNull(await parametros.GuardarAsync(new ParametrosSistema { TasaImpuesto = 0m, UmbralStockBajo = -1, DiasVigencia = 0 }));
    }

    [Fact]
    public async Task InventarioRechazaCambiosObsoletosNegativosYDigitales()
    {
        var fisico = await contexto.Libros.AsNoTracking().SingleAsync(l => l.ISBN == "123");
        var digital = await contexto.Libros.AsNoTracking().SingleAsync(l => l.ISBN == "456");
        var inventario = new ServicioInventario(contexto);
        Assert.NotNull(await inventario.ActualizarAsync(fisico.Id, fisico.Stock, -1));
        Assert.NotNull(await inventario.ActualizarAsync(digital.Id, digital.Stock, 3));
        Assert.Null(await inventario.ActualizarAsync(fisico.Id, fisico.Stock, 8));
        Assert.NotNull(await inventario.ActualizarAsync(fisico.Id, fisico.Stock, 9));
        Assert.Equal(8, (await contexto.Libros.AsNoTracking().SingleAsync(l => l.Id == fisico.Id)).Stock);
    }

    [Fact]
    public async Task FallaEnSegundoLibroRevierteDescuentoDelPrimero()
    {
        var primero = await contexto.Libros.Include(l => l.Categoria).Include(l => l.Editorial).SingleAsync(l => l.ISBN == "123");
        var otro = new Libro { ISBN = "789", Titulo = "Otro libro", Sinopsis = "Descripción", EditorialId = primero.EditorialId, CategoriaId = primero.CategoriaId, PrecioBase = 20m, Stock = 3, Formato = FormatoLibro.TapaDura };
        var carrito = await contexto.Carritos.Include(c => c.Detalles).SingleAsync();
        carrito.Detalles.Add(new DetalleCarrito { Libro = otro, Cantidad = 2 });
        await contexto.SaveChangesAsync();
        contexto.ChangeTracker.Clear();
        var pedido = (await servicio.CrearAsync("cliente", false, MetodoPagoSimulado.TarjetaSimulada)).Pedido!;
        await contexto.Libros.Where(l => l.ISBN == "789").ExecuteUpdateAsync(s => s.SetProperty(l => l.Stock, 1));
        Assert.NotNull(await servicio.CambiarEstadoAsync(pedido.Id, EstadoPedido.Aprobado));
        Assert.Equal(10, (await contexto.Libros.AsNoTracking().SingleAsync(l => l.ISBN == "123")).Stock);
        Assert.Equal(EstadoPedido.Pendiente, (await contexto.Pedidos.AsNoTracking().SingleAsync()).Estado);
    }

    [Fact]
    public async Task CrearPedidoInstitucionalAplicaDescuentoSobreBaseYaReducida()
    {
        var detalle = await contexto.DetallesCarrito.Include(d => d.Libro).SingleAsync(d => d.Libro.ISBN == "123");
        detalle.Cantidad = 6;
        await contexto.SaveChangesAsync();
        contexto.ChangeTracker.Clear();
        var pedido = (await servicio.CrearAsync("cliente", true, MetodoPagoSimulado.TarjetaSimulada)).Pedido!;
        Assert.Equal(650m, pedido.Subtotal);
        Assert.Equal(32.50m, pedido.DescuentoVolumen);
        Assert.Equal(49.40m, pedido.DescuentoInstitucional);
        Assert.Equal(68.17m, pedido.Impuesto);
        Assert.Equal(636.27m, pedido.Total);
    }

    public void Dispose() { contexto.Dispose(); conexion.Dispose(); }
}
