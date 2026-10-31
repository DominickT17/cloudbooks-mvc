using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using CloudBooks.Web.Servicios;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CloudBooks.Pruebas;

public class CarritoInvitadoPruebas : IDisposable
{
    private readonly SqliteConnection conexion = new("Data Source=:memory:");
    private readonly ContextoAplicacion contexto;
    private readonly ServicioCarritoInvitado invitado;
    private readonly SesionPrueba sesion = new();
    private readonly int libroId;
    public CarritoInvitadoPruebas()
    {
        conexion.Open();
        contexto = new ContextoAplicacion(new DbContextOptionsBuilder<ContextoAplicacion>().UseSqlite(conexion).Options);
        contexto.Database.EnsureCreated();
        var libro = new Libro { ISBN = "123", Titulo = "Libro", Sinopsis = "Descripción", Editorial = new Editorial { Nombre = "Editorial" }, Categoria = new Categoria { Nombre = "Categoría" }, PrecioBase = 50, Stock = 5, Formato = FormatoLibro.TapaBlanda };
        contexto.Users.Add(new UsuarioAplicacion { Id = "cliente", UserName = "cliente", NombreCompleto = "Cliente" });
        contexto.Libros.Add(libro); contexto.SaveChanges(); libroId = libro.Id;
        invitado = new ServicioCarritoInvitado(contexto, new ServicioCarrito(contexto));
    }
    [Fact] public async Task VisitanteAgregaConsultaActualizaYElimina()
    {
        Assert.Null(await invitado.AgregarAsync(sesion, libroId, 2));
        Assert.Equal(2, invitado.Cantidad(sesion));
        Assert.Equal(2, Assert.Single(await invitado.ObtenerAsync(sesion)).Cantidad);
        Assert.NotNull(await invitado.EstablecerAsync(sesion, libroId, 6));
        Assert.Null(await invitado.EstablecerAsync(sesion, libroId, 3));
        Assert.Equal(3, invitado.Cantidad(sesion));
        invitado.Eliminar(sesion, libroId); Assert.Empty(await invitado.ObtenerAsync(sesion));
    }
    [Fact] public async Task FusionSumaCarritoExistenteSinDuplicar()
    {
        Assert.Null(await new ServicioCarrito(contexto).AgregarAsync("cliente", libroId, 1));
        Assert.Null(await invitado.AgregarAsync(sesion, libroId, 2));
        Assert.True(await invitado.FusionarAsync(sesion, "cliente"));
        Assert.Equal(0, invitado.Cantidad(sesion));
        Assert.True(await invitado.FusionarAsync(sesion, "cliente"));
        Assert.Equal(3, Assert.Single((await new ServicioCarrito(contexto).ObtenerAsync("cliente"))!.Detalles).Cantidad);
    }
    [Fact] public async Task FusionConStockInsuficienteConservaLosProductos()
    {
        Assert.Null(await invitado.AgregarAsync(sesion, libroId, 4));
        Assert.Null(await new ServicioCarrito(contexto).AgregarAsync("cliente", libroId, 2));
        Assert.False(await invitado.FusionarAsync(sesion, "cliente"));
        Assert.Equal(4, invitado.Cantidad(sesion));
        Assert.Equal(2, Assert.Single((await new ServicioCarrito(contexto).ObtenerAsync("cliente"))!.Detalles).Cantidad);
    }
    public void Dispose() { contexto.Dispose(); conexion.Dispose(); }
    private class SesionPrueba : ISession
    {
        private readonly Dictionary<string, byte[]> datos = [];
        public bool IsAvailable => true; public string Id => "prueba"; public IEnumerable<string> Keys => datos.Keys;
        public void Clear() => datos.Clear(); public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => datos.Remove(key); public void Set(string key, byte[] value) => datos[key] = value;
        public bool TryGetValue(string key, out byte[] value) => datos.TryGetValue(key, out value!);
    }
}
