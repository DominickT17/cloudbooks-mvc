using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using CloudBooks.Web.Servicios;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CloudBooks.Pruebas;

public class ServicioMetricasPruebas : IDisposable
{
    private readonly SqliteConnection conexion = new("Data Source=:memory:");
    private readonly ContextoAplicacion contexto;

    public ServicioMetricasPruebas()
    {
        conexion.Open();
        contexto = new ContextoAplicacion(new DbContextOptionsBuilder<ContextoAplicacion>().UseSqlite(conexion).Options);
        contexto.Database.EnsureCreated();
        var usuario = new UsuarioAplicacion { Id = "cliente", UserName = "cliente" };
        var libro = new Libro { ISBN = "123", Titulo = "Lecturas", Sinopsis = "Descripción", PrecioBase = 100m, Stock = 5, Categoria = new Categoria { Nombre = "Narrativa" }, Editorial = new Editorial { Nombre = "Editorial" } };
        foreach (var (estado, cantidad) in new[] { (EstadoPedido.Pendiente, 20), (EstadoPedido.Cancelado, 30), (EstadoPedido.Aprobado, 2), (EstadoPedido.Enviado, 3) })
            contexto.Pedidos.Add(new Pedido { Usuario = usuario, Estado = estado, Total = cantidad * 100m, Detalles = [new DetallePedido { Libro = libro, Titulo = libro.Titulo, Cantidad = cantidad, PrecioUnitario = 100m, Subtotal = cantidad * 100m }] });
        contexto.SaveChanges();
        contexto.ChangeTracker.Clear();
    }

    [Fact]
    public async Task VentasYRankingSoloCuentanPedidosAprobadosYEnviados()
    {
        var metricas = await new ServicioMetricas(contexto).ObtenerAsync();
        Assert.Equal(500m, metricas.Ventas);
        Assert.Equal(4, metricas.Pedidos);
        Assert.Equal(5, Assert.Single(metricas.MasVendidos).Unidades);
        Assert.Equal(5, Assert.Single(metricas.StockBajo).Stock);
    }

    public void Dispose() { contexto.Dispose(); conexion.Dispose(); }
}
