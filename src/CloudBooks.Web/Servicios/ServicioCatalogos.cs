using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using Microsoft.EntityFrameworkCore;

namespace CloudBooks.Web.Servicios;

public record EntradaCatalogo(int Id, string Nombre);

public class ServicioCatalogos(ContextoAplicacion contexto)
{
    public static bool TipoValido(string? tipo) => tipo is "autores" or "categorias" or "editoriales";
    public Task<List<EntradaCatalogo>> ListarAsync(string tipo) => tipo switch
    {
        "autores" => contexto.Autores.AsNoTracking().OrderBy(a => a.Nombre).Select(a => new EntradaCatalogo(a.Id, a.Nombre)).ToListAsync(),
        "categorias" => contexto.Categorias.AsNoTracking().OrderBy(a => a.Nombre).Select(a => new EntradaCatalogo(a.Id, a.Nombre)).ToListAsync(),
        "editoriales" => contexto.Editoriales.AsNoTracking().OrderBy(a => a.Nombre).Select(a => new EntradaCatalogo(a.Id, a.Nombre)).ToListAsync(),
        _ => throw new ArgumentException("Catálogo no válido.")
    };

    public async Task<string?> GuardarAsync(string tipo, int id, string nombre)
    {
        nombre = nombre.Trim();
        if (nombre.Length == 0 || nombre.Length > (tipo == "categorias" ? 100 : 150)) return "Ingresa un nombre válido.";
        if ((await ListarAsync(tipo)).Any(e => e.Id != id && string.Equals(e.Nombre, nombre, StringComparison.OrdinalIgnoreCase))) return "Ya existe un registro con ese nombre.";
        switch (tipo)
        {
            case "autores":
                var autor = id == 0 ? new Autor() : await contexto.Autores.FindAsync(id); if (autor == null) return "Registro no encontrado."; autor.Nombre = nombre; if (id == 0) contexto.Autores.Add(autor); break;
            case "categorias":
                var categoria = id == 0 ? new Categoria() : await contexto.Categorias.FindAsync(id); if (categoria == null) return "Registro no encontrado."; categoria.Nombre = nombre; if (id == 0) contexto.Categorias.Add(categoria); break;
            case "editoriales":
                var editorial = id == 0 ? new Editorial() : await contexto.Editoriales.FindAsync(id); if (editorial == null) return "Registro no encontrado."; editorial.Nombre = nombre; if (id == 0) contexto.Editoriales.Add(editorial); break;
            default: return "Catálogo no válido.";
        }
        await contexto.SaveChangesAsync();
        return null;
    }
}
