using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using CloudBooks.Web.Servicios;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CloudBooks.Pruebas;

public class CatalogoCiteinPruebas
{
    [Fact]
    public async Task ConsultaPorIsbnIncluyeAutoresYCategoriasYExcluyeOtrosOInactivos()
    {
        await using var conexion = new SqliteConnection("Data Source=:memory:");
        await conexion.OpenAsync();
        await using var contexto = new ContextoAplicacion(new DbContextOptionsBuilder<ContextoAplicacion>().UseSqlite(conexion).Options);
        await contexto.Database.EnsureCreatedAsync();
        var categoria = new Categoria { Nombre = "Matemáticas" };
        var editorial = new Editorial { Nombre = "Editorial académica" };
        contexto.Libros.AddRange(
            new Libro { ISBN = "9786075502090", Titulo = "Álgebra", Sinopsis = "Prueba", Categoria = categoria, Editorial = editorial, Autores = [new Autor { Nombre = "Aurelio Baldor" }], Activo = true },
            new Libro { ISBN = "9781951693510", Titulo = "Cálculo volumen 1", Sinopsis = "Prueba", Categoria = categoria, Editorial = editorial, Formato = FormatoLibro.Ebook, Stock = 0, Autores = [new Autor { Nombre = "Gilbert Strang" }], Activo = true },
            new Libro { ISBN = "inactivo", Titulo = "Inactivo", Sinopsis = "Prueba", Categoria = categoria, Editorial = editorial, Activo = false },
            new Libro { ISBN = "otro", Titulo = "No seleccionado", Sinopsis = "Prueba", Categoria = categoria, Editorial = editorial, Activo = true });
        await contexto.SaveChangesAsync();
        contexto.ChangeTracker.Clear();

        var libros = await new ServicioCatalogo(contexto).ObtenerPorIsbnAsync(["9786075502090", "9781951693510", "inactivo"]);
        Assert.Equal(2, libros.Count);
        Assert.Contains(libros, l => l.ISBN == "9781951693510" && l.Formato == FormatoLibro.Ebook && l.Stock == 0);
        var algebra = Assert.Single(libros, l => l.ISBN == "9786075502090");
        Assert.Equal("Matemáticas", algebra.Categoria.Nombre);
        Assert.Equal("Aurelio Baldor", Assert.Single(algebra.Autores).Nombre);
        Assert.DoesNotContain(libros, l => l.ISBN is "otro" or "inactivo");
    }
}
