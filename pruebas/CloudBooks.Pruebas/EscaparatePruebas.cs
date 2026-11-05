using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using CloudBooks.Web.Servicios;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CloudBooks.Pruebas;
public class EscaparatePruebas : IDisposable
{
    private readonly SqliteConnection conexion = new("Data Source=:memory:");
    private readonly ContextoAplicacion contexto;
    private readonly ServicioEscaparate servicio;
    private readonly List<Libro> libros = [];
    public EscaparatePruebas()
    {
        conexion.Open(); contexto = new ContextoAplicacion(new DbContextOptionsBuilder<ContextoAplicacion>().UseSqlite(conexion).Options); contexto.Database.EnsureCreated();
        var categoria = new Categoria { Nombre = "Tecnología" }; var editorial = new Editorial { Nombre = "Editorial" };
        for (int i = 1; i <= 6; i++) libros.Add(new Libro { ISBN = i.ToString(), Titulo = $"Libro {i}", Sinopsis = "Sinopsis", Categoria = categoria, Editorial = editorial, PrecioBase = 10, Stock = 10, Destacado = i == 2 });
        contexto.Libros.AddRange(libros); contexto.Users.Add(new UsuarioAplicacion { Id = "cliente", UserName = "cliente", NombreCompleto = "Cliente" }); contexto.SaveChanges(); servicio = new ServicioEscaparate(contexto);
    }
    private void Pedido(EstadoPedido estado, params (int indice, int cantidad)[] lineas)
    {
        contexto.Pedidos.Add(new Pedido { UsuarioId = "cliente", Estado = estado, FechaUtc = DateTime.UtcNow, Detalles = lineas.Select(x => new DetallePedido { LibroId = libros[x.indice].Id, Titulo = libros[x.indice].Titulo, Cantidad = x.cantidad, PrecioUnitario = 10, Subtotal = 10 * x.cantidad }).ToList() }); contexto.SaveChanges();
    }
    [Fact] public async Task VentasExcluyenPendientesYCanceladosYExigenCuatroTitulos()
    {
        Pedido(EstadoPedido.Aprobado, (0, 2), (1, 1), (2, 1)); Pedido(EstadoPedido.Cancelado, (3, 100));
        Assert.False((await servicio.SeleccionarVentasAsync()).SonMasVendidos);
        Pedido(EstadoPedido.Enviado, (3, 3)); Pedido(EstadoPedido.Pendiente, (4, 100));
        var seleccion = await servicio.SeleccionarVentasAsync(); Assert.True(seleccion.SonMasVendidos); Assert.Equal(libros[3].Id, seleccion.Libros[0].Id); Assert.DoesNotContain(seleccion.Libros, l => l.Id == libros[4].Id);
    }
    [Fact] public async Task RecomendadosUsanMarcaAdministrativaYActivos()
    {
        Assert.Equal(libros[1].Id, Assert.Single(await servicio.RecomendadosAsync()).Id);
        libros[1].Activo = false; await contexto.SaveChangesAsync(); Assert.Empty(await servicio.RecomendadosAsync());
    }
    [Fact] public async Task RecientesSonUnicosLimitadosYExcluyenInactivos()
    {
        var sesion = new Sesion(); foreach (var libro in libros) servicio.RegistrarVista(sesion, libro.Id); servicio.RegistrarVista(sesion, libros[2].Id);
        var recientes = await servicio.RecientesAsync(sesion); Assert.Equal(5, recientes.Count); Assert.Equal(libros[2].Id, recientes[0].Id); Assert.Equal(5, recientes.Select(l => l.Id).Distinct().Count());
        libros[2].Activo = false; await contexto.SaveChangesAsync(); Assert.DoesNotContain(await servicio.RecientesAsync(sesion), l => l.Id == libros[2].Id);
    }
    public void Dispose() { contexto.Dispose(); conexion.Dispose(); }
    private class Sesion : ISession
    {
        readonly Dictionary<string, byte[]> datos = []; public bool IsAvailable => true; public string Id => "prueba"; public IEnumerable<string> Keys => datos.Keys;
        public void Clear() => datos.Clear(); public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask; public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => datos.Remove(key); public void Set(string key, byte[] value) => datos[key] = value; public bool TryGetValue(string key, out byte[] value) => datos.TryGetValue(key, out value!);
    }
}
