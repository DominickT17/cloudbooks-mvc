using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using CloudBooks.Web.ModelosVista;
using Microsoft.EntityFrameworkCore;

namespace CloudBooks.Web.Servicios;

public class ServicioAdministracionCatalogo(ContextoAplicacion contexto, IWebHostEnvironment entorno)
{
    public async Task<List<Libro>> ListarAsync(string? buscar, string? estado)
    {
        var consulta = contexto.Libros.AsNoTracking().Include(l => l.Categoria).Include(l => l.Editorial).AsQueryable();
        if (!string.IsNullOrWhiteSpace(buscar)) { var texto = buscar.Trim(); consulta = consulta.Where(l => l.Titulo.Contains(texto) || l.ISBN.Contains(texto)); }
        if (estado == "activos") consulta = consulta.Where(l => l.Activo);
        if (estado == "inactivos") consulta = consulta.Where(l => !l.Activo);
        return await consulta.OrderBy(l => l.Titulo).ToListAsync();
    }

    public async Task<LibroAdministracionModelo?> ObtenerAsync(int id)
    {
        var libro = await contexto.Libros.AsNoTracking().Include(l => l.Autores).SingleOrDefaultAsync(l => l.Id == id);
        return libro == null ? null : new LibroAdministracionModelo { Id = libro.Id, Activo = libro.Activo, ISBN = libro.ISBN, Titulo = libro.Titulo, Sinopsis = libro.Sinopsis, AnioEdicion = libro.AnioEdicion, PrecioBase = libro.PrecioBase, Stock = libro.Stock, StockOriginal = libro.Stock, ImagenPortada = libro.ImagenPortada, Formato = libro.Formato, EditorialId = libro.EditorialId, CategoriaId = libro.CategoriaId, AutoresIds = libro.Autores.Select(a => a.Id).ToArray() };
    }

    public async Task PrepararAsync(LibroAdministracionModelo modelo)
    {
        modelo.Autores = await contexto.Autores.AsNoTracking().OrderBy(a => a.Nombre).ToListAsync();
        modelo.Categorias = await contexto.Categorias.AsNoTracking().OrderBy(c => c.Nombre).ToListAsync();
        modelo.Editoriales = await contexto.Editoriales.AsNoTracking().OrderBy(e => e.Nombre).ToListAsync();
        var ruta = Path.Combine(entorno.WebRootPath, "imagenes", "libros");
        modelo.Portadas = Directory.Exists(ruta) ? Directory.GetFiles(ruta).Where(p => new[] { ".svg", ".png", ".jpg", ".jpeg", ".webp" }.Contains(Path.GetExtension(p).ToLowerInvariant())).Select(p => "/imagenes/libros/" + Path.GetFileName(p)).OrderBy(p => p).ToList() : [];
    }

    public async Task<string?> GuardarAsync(LibroAdministracionModelo modelo)
    {
        if (!Enum.IsDefined(modelo.Formato) || modelo.AutoresIds.Length == 0 || modelo.PrecioBase <= 0 || decimal.Round(modelo.PrecioBase, 2) != modelo.PrecioBase || modelo.Stock < 0) return "Revisa el formato, los autores, el precio y el stock.";
        await PrepararAsync(modelo);
        if (!modelo.Portadas.Contains(modelo.ImagenPortada)) return "Selecciona una portada local disponible.";
        if (!await contexto.Categorias.AnyAsync(c => c.Id == modelo.CategoriaId) || !await contexto.Editoriales.AnyAsync(e => e.Id == modelo.EditorialId)) return "Selecciona una categoría y editorial válidas.";
        var ids = modelo.AutoresIds.Distinct().ToArray();
        var autores = await contexto.Autores.Where(a => ids.Contains(a.Id)).ToListAsync();
        if (autores.Count != ids.Length) return "Selecciona autores válidos.";
        var isbn = modelo.ISBN.Trim();
        if (await contexto.Libros.AnyAsync(l => l.ISBN == isbn && l.Id != modelo.Id)) return "Ya existe un libro con ese ISBN.";
        Libro libro;
        if (modelo.Id == 0) { libro = new Libro(); contexto.Libros.Add(libro); }
        else
        {
            libro = (await contexto.Libros.Include(l => l.Autores).SingleOrDefaultAsync(l => l.Id == modelo.Id))!;
            if (libro == null) return "El libro no existe.";
            if (libro.Stock != modelo.StockOriginal) return "El stock cambió durante la edición. Recarga el formulario antes de guardar.";
            contexto.Entry(libro).Property(l => l.Stock).OriginalValue = modelo.StockOriginal;
        }
        libro.ISBN = isbn; libro.Titulo = modelo.Titulo.Trim(); libro.Sinopsis = modelo.Sinopsis.Trim(); libro.AnioEdicion = modelo.AnioEdicion;
        libro.PrecioBase = modelo.PrecioBase; libro.Stock = modelo.Formato == FormatoLibro.Ebook ? 0 : modelo.Stock;
        libro.ImagenPortada = modelo.ImagenPortada; libro.Formato = modelo.Formato; libro.EditorialId = modelo.EditorialId; libro.CategoriaId = modelo.CategoriaId;
        libro.Autores = autores;
        try { await contexto.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return "El stock cambió durante la edición. Recarga el formulario antes de guardar."; }
        catch (DbUpdateException error) when (error.InnerException is Microsoft.Data.SqlClient.SqlException sql && (sql.Number == 2601 || sql.Number == 2627)) { return "Ya existe un libro con ese ISBN."; }
        return null;
    }

    public async Task<bool> CambiarActivoAsync(int id, bool activo)
    {
        return await contexto.Libros.Where(l => l.Id == id).ExecuteUpdateAsync(s => s.SetProperty(l => l.Activo, activo)) == 1;
    }
}
