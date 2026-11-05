using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using CloudBooks.Web.Servicios;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace CloudBooks.Pruebas;

public class ConcurrenciaSqlPruebas(ITestOutputHelper salida)
{
    private static DbContextOptions<ContextoAplicacion> Opciones()
    {
        var cadena = Environment.GetEnvironmentVariable("CLOUDBOOKS_QA_SQL")!;
        var baseDatos = new SqlConnectionStringBuilder(cadena).InitialCatalog;
        if (!baseDatos.StartsWith("CloudBooks_QA_", StringComparison.Ordinal)) throw new InvalidOperationException("Se requiere una base temporal de QA.");
        return new DbContextOptionsBuilder<ContextoAplicacion>().UseSqlServer(cadena).Options;
    }

    [SqlTemporalFact]
    public async Task DosComprasSimultaneasNoVendenMasDeDoceUnidades()
    {
        var opciones = Opciones();
        await using var preparacion = new ContextoAplicacion(opciones);
        var categoria = await preparacion.Categorias.FirstAsync();
        var editorial = await preparacion.Editoriales.FirstAsync();
        var sufijo = Guid.NewGuid().ToString("N");
        var libro = new Libro { ISBN = sufijo[..13], Titulo = "Libro QA concurrencia", Sinopsis = "Prueba", CategoriaId = categoria.Id, EditorialId = editorial.Id, PrecioBase = 10, Stock = 12, Formato = FormatoLibro.TapaBlanda };
        var a = new UsuarioAplicacion { Id = "qa-a-" + sufijo, UserName = "qa-a-" + sufijo, NombreCompleto = "Cliente A" };
        var b = new UsuarioAplicacion { Id = "qa-b-" + sufijo, UserName = "qa-b-" + sufijo, NombreCompleto = "Cliente B" };
        preparacion.Carritos.AddRange(new Carrito { Usuario = a, Detalles = [new DetalleCarrito { Libro = libro, Cantidad = 10 }] }, new Carrito { Usuario = b, Detalles = [new DetalleCarrito { Libro = libro, Cantidad = 3 }] });
        var parametros = await preparacion.ConfiguracionSistema.SingleAsync();
        parametros.UmbralStockBajo = 0;
        await preparacion.SaveChangesAsync();
        var inicio = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<(Pedido? Pedido, string? Error)> Comprar(string usuario)
        {
            await using var contexto = new ContextoAplicacion(opciones);
            await inicio.Task;
            return await new ServicioPedidos(contexto, new ServicioDescuentos(), new ServicioParametros(contexto)).CrearAsync(usuario, false, MetodoPagoSimulado.TransferenciaSimulada);
        }
        var primera = Comprar(a.Id);
        var segunda = Comprar(b.Id);
        inicio.SetResult();
        var resultados = await Task.WhenAll(primera, segunda);
        await using var verificacion = new ContextoAplicacion(opciones);
        var stock = await verificacion.Libros.Where(l => l.Id == libro.Id).Select(l => l.Stock).SingleAsync();
        var pedidos = await verificacion.Pedidos.AsNoTracking().Where(p => p.UsuarioId == a.Id || p.UsuarioId == b.Id).Include(p => p.Detalles).ToListAsync();
        var unidades = pedidos.Where(p => p.Estado == EstadoPedido.Aprobado).SelectMany(p => p.Detalles).Sum(d => d.Cantidad);
        Assert.Single(pedidos);
        Assert.Single(resultados, r => r.Pedido != null);
        Assert.Single(resultados, r => r.Error != null);
        Assert.True(stock >= 0);
        Assert.Equal(12, stock + unidades);
        Assert.Single(await verificacion.DetallesCarrito.Where(d => d.Carrito.UsuarioId == a.Id || d.Carrito.UsuarioId == b.Id).ToListAsync());
        salida.WriteLine($"Stock inicial 12; aprobado {unidades}; stock final {stock}; pedidos {pedidos.Count}; error controlado {resultados.Single(r => r.Error != null).Error}");
    }

    [SqlTemporalFact]
    public async Task DosProcesadoresNoEntreganDosVecesElMismoCorreo()
    {
        var opciones = Opciones();
        await using var preparacion = new ContextoAplicacion(opciones);
        var correo = new CorreoPendiente { ClaveEvento = "qa/" + Guid.NewGuid().ToString("N"), Destinatario = "cliente@example.test", Asunto = "Prueba QA", Html = "Prueba", Tipo = "qa", CreadoUtc = DateTime.UtcNow, Estado = EstadoCorreo.Pendiente };
        preparacion.CorreosPendientes.Add(correo);
        await preparacion.SaveChangesAsync();
        var proveedor = new ProveedorControlado();
        await using var uno = new ContextoAplicacion(opciones);
        await using var dos = new ContextoAplicacion(opciones);
        var primera = new ProcesadorCorreos(uno, proveedor).ProcesarUnoAsync();
        await proveedor.Inicio.Task.WaitAsync(TimeSpan.FromSeconds(20));
        var segunda = await new ProcesadorCorreos(dos, proveedor).ProcesarUnoAsync();
        proveedor.Liberar.SetResult();
        Assert.True(await primera);
        Assert.False(segunda);
        Assert.Equal(1, proveedor.Llamadas);
        await using var verificacion = new ContextoAplicacion(opciones);
        var final = await verificacion.CorreosPendientes.AsNoTracking().SingleAsync(c => c.Id == correo.Id);
        Assert.Equal(EstadoCorreo.Simulado, final.Estado);
        Assert.Equal(1, final.Intentos);
        salida.WriteLine($"Dos trabajadores; invocaciones al proveedor: {proveedor.Llamadas}; intentos: {final.Intentos}; estado: {final.Estado}.");
    }

    [SqlTemporalFact]
    public async Task AprobarYCancelarElMismoPedidoNoAplicanAmbasTransiciones()
    {
        var opciones = Opciones();
        await using var preparacion = new ContextoAplicacion(opciones);
        var categoria = await preparacion.Categorias.FirstAsync();
        var editorial = await preparacion.Editoriales.FirstAsync();
        var sufijo = Guid.NewGuid().ToString("N");
        var libro = new Libro { ISBN = sufijo[..13], Titulo = "Libro QA transición", Sinopsis = "Prueba", CategoriaId = categoria.Id, EditorialId = editorial.Id, PrecioBase = 10, Stock = 12, Formato = FormatoLibro.TapaBlanda };
        var usuario = new UsuarioAplicacion { Id = "qa-c-" + sufijo, UserName = "qa-c-" + sufijo, NombreCompleto = "Cliente" };
        preparacion.Carritos.Add(new Carrito { Usuario = usuario, Detalles = [new DetalleCarrito { Libro = libro, Cantidad = 3 }] });
        await preparacion.SaveChangesAsync();
        var servicio = new ServicioPedidos(preparacion, new ServicioDescuentos());
        var pedido = (await servicio.CrearAsync(usuario.Id, false, MetodoPagoSimulado.TarjetaSimulada, "Otra persona")).Pedido!;
        Assert.Equal(EstadoPedido.EnRevision, pedido.Estado);
        Assert.Null(await servicio.MarcarRevisadoAsync(pedido.Id));
        var inicio = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<string?> Cambiar(EstadoPedido estado)
        {
            await using var contexto = new ContextoAplicacion(opciones);
            await inicio.Task;
            return await new ServicioPedidos(contexto, new ServicioDescuentos()).CambiarEstadoAsync(pedido.Id, estado);
        }
        var aprobar = Cambiar(EstadoPedido.Aprobado);
        var cancelar = Cambiar(EstadoPedido.Cancelado);
        inicio.SetResult();
        var resultados = await Task.WhenAll(aprobar, cancelar);
        Assert.Single(resultados, r => r == null);
        Assert.Single(resultados, r => r != null);
        await using var verificacion = new ContextoAplicacion(opciones);
        var final = await verificacion.Pedidos.AsNoTracking().SingleAsync(p => p.Id == pedido.Id);
        var stock = await verificacion.Libros.AsNoTracking().Where(l => l.Id == libro.Id).Select(l => l.Stock).SingleAsync();
        Assert.True((final.Estado == EstadoPedido.Aprobado && final.EstadoPago == EstadoPago.Confirmado && stock == 9) ||
                    (final.Estado == EstadoPedido.Cancelado && final.EstadoPago == EstadoPago.Anulado && stock == 12));
        Assert.NotNull(await new ServicioPedidos(verificacion, new ServicioDescuentos()).CambiarEstadoAsync(pedido.Id, EstadoPedido.Aprobado));
        salida.WriteLine($"Estado: {final.Estado}; pago: {final.EstadoPago}; stock: {stock}; una transición exitosa.");
    }

    [SqlTemporalFact]
    public async Task DosAprobacionesSimultaneasDescuentanUnaSolaVez()
    {
        var opciones = Opciones();
        await using var preparacion = new ContextoAplicacion(opciones);
        var categoria = await preparacion.Categorias.FirstAsync();
        var editorial = await preparacion.Editoriales.FirstAsync();
        var sufijo = Guid.NewGuid().ToString("N");
        var libro = new Libro { ISBN = sufijo[..13], Titulo = "Libro QA doble aprobación", Sinopsis = "Prueba", CategoriaId = categoria.Id, EditorialId = editorial.Id, PrecioBase = 10, Stock = 20, Formato = FormatoLibro.TapaBlanda };
        var usuario = new UsuarioAplicacion { Id = "qa-d-" + sufijo, UserName = "qa-d-" + sufijo, NombreCompleto = "Cliente" };
        preparacion.Carritos.Add(new Carrito { Usuario = usuario, Detalles = [new DetalleCarrito { Libro = libro, Cantidad = 3 }] });
        await preparacion.SaveChangesAsync();
        var servicio = new ServicioPedidos(preparacion, new ServicioDescuentos());
        var pedido = (await servicio.CrearAsync(usuario.Id, false, MetodoPagoSimulado.TarjetaSimulada, "Otra persona")).Pedido!;
        Assert.Equal(EstadoPedido.EnRevision, pedido.Estado);
        Assert.Null(await servicio.MarcarRevisadoAsync(pedido.Id));
        var inicio = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<string?> Aprobar()
        {
            await using var contexto = new ContextoAplicacion(opciones);
            await inicio.Task;
            return await new ServicioPedidos(contexto, new ServicioDescuentos()).CambiarEstadoAsync(pedido.Id, EstadoPedido.Aprobado);
        }
        var uno = Aprobar();
        var dos = Aprobar();
        inicio.SetResult();
        var resultados = await Task.WhenAll(uno, dos);
        Assert.Single(resultados, r => r == null);
        Assert.Single(resultados, r => r != null);
        await using var verificacion = new ContextoAplicacion(opciones);
        Assert.Equal(17, await verificacion.Libros.Where(l => l.Id == libro.Id).Select(l => l.Stock).SingleAsync());
        Assert.Equal(EstadoPedido.Aprobado, await verificacion.Pedidos.Where(p => p.Id == pedido.Id).Select(p => p.Estado).SingleAsync());
        Assert.NotNull(await new ServicioPedidos(verificacion, new ServicioDescuentos()).CambiarEstadoAsync(pedido.Id, EstadoPedido.Aprobado));
        Assert.Equal(17, await verificacion.Libros.Where(l => l.Id == libro.Id).Select(l => l.Stock).SingleAsync());
    }

    [SqlTemporalFact]
    public async Task FalloEnSegundoProductoRevierteStockYPermiteReintento()
    {
        var opciones = Opciones();
        await using var contexto = new ContextoAplicacion(opciones);
        var categoria = await contexto.Categorias.FirstAsync();
        var editorial = await contexto.Editoriales.FirstAsync();
        var sufijo = Guid.NewGuid().ToString("N");
        Libro LibroQa(string isbn) => new() { ISBN = isbn, Titulo = "Libro QA rollback", Sinopsis = "Prueba", CategoriaId = categoria.Id, EditorialId = editorial.Id, PrecioBase = 10, Stock = 20, Formato = FormatoLibro.TapaBlanda };
        var primero = LibroQa(sufijo[..13]);
        var segundo = LibroQa(sufijo[13..26]);
        var usuario = new UsuarioAplicacion { Id = "qa-e-" + sufijo, UserName = "qa-e-" + sufijo, NombreCompleto = "Cliente" };
        contexto.Carritos.Add(new Carrito { Usuario = usuario, Detalles = [new DetalleCarrito { Libro = primero, Cantidad = 3 }, new DetalleCarrito { Libro = segundo, Cantidad = 3 }] });
        await contexto.SaveChangesAsync();
        var servicio = new ServicioPedidos(contexto, new ServicioDescuentos());
        var pedido = (await servicio.CrearAsync(usuario.Id, false, MetodoPagoSimulado.TarjetaSimulada, "Otra persona")).Pedido!;
        Assert.Equal(EstadoPedido.EnRevision, pedido.Estado);
        Assert.Null(await servicio.MarcarRevisadoAsync(pedido.Id));
        var ids = new[] { primero.Id, segundo.Id }.Order().ToArray();
        await contexto.Libros.Where(l => l.Id == ids[1]).ExecuteUpdateAsync(s => s.SetProperty(l => l.Stock, 1));
        Assert.NotNull(await servicio.CambiarEstadoAsync(pedido.Id, EstadoPedido.Aprobado));
        contexto.ChangeTracker.Clear();
        Assert.Equal(20, await contexto.Libros.Where(l => l.Id == ids[0]).Select(l => l.Stock).SingleAsync());
        Assert.Equal(EstadoPedido.EnRevision, await contexto.Pedidos.Where(p => p.Id == pedido.Id).Select(p => p.Estado).SingleAsync());
        await contexto.Libros.Where(l => l.Id == ids[1]).ExecuteUpdateAsync(s => s.SetProperty(l => l.Stock, 20));
        Assert.Null(await servicio.CambiarEstadoAsync(pedido.Id, EstadoPedido.Aprobado));
        Assert.Equal(new[] { 17, 17 }, await contexto.Libros.Where(l => ids.Contains(l.Id)).OrderBy(l => l.Id).Select(l => l.Stock).ToArrayAsync());
    }

    private class ProveedorControlado : IProveedorCorreo
    {
        public TaskCompletionSource Inicio { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Liberar { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Llamadas;
        public async Task<ResultadoEntrega> EnviarAsync(CorreoPendiente correo, CancellationToken cancelacion)
        {
            Interlocked.Increment(ref Llamadas);
            Inicio.TrySetResult();
            await Liberar.Task.WaitAsync(cancelacion);
            return new ResultadoEntrega(true, null);
        }
    }
}
