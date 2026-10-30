using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using CloudBooks.Web.ModelosVista;
using Microsoft.EntityFrameworkCore;

namespace CloudBooks.Web.Servicios;

public class ServicioCatalogo(ContextoAplicacion contexto)
{
    public async Task<CatalogoModelo> BuscarAsync(CatalogoModelo filtros)
    {
        var consulta = contexto.Libros.AsNoTracking().Where(libro => libro.Activo);
        if (!string.IsNullOrWhiteSpace(filtros.Buscar))
        {
            var texto = filtros.Buscar.Trim();
            consulta = consulta.Where(libro => libro.Titulo.Contains(texto) || libro.ISBN.Contains(texto) || libro.Editorial.Nombre.Contains(texto) || libro.Autores.Any(autor => autor.Nombre.Contains(texto)));
        }
        if (filtros.CategoriaId.HasValue) consulta = consulta.Where(libro => libro.CategoriaId == filtros.CategoriaId);
        if (filtros.Formato.HasValue) consulta = consulta.Where(libro => libro.Formato == filtros.Formato);
        if (filtros.PrecioMinimo.HasValue) consulta = consulta.Where(libro => libro.PrecioBase >= filtros.PrecioMinimo);
        if (filtros.PrecioMaximo.HasValue) consulta = consulta.Where(libro => libro.PrecioBase <= filtros.PrecioMaximo);
        consulta = filtros.Orden switch
        {
            "menor" => consulta.OrderBy(libro => libro.PrecioBase).ThenBy(libro => libro.Titulo),
            "mayor" => consulta.OrderByDescending(libro => libro.PrecioBase).ThenBy(libro => libro.Titulo),
            _ => consulta.OrderBy(libro => libro.Titulo)
        };
        filtros.Libros = await consulta.Include(libro => libro.Autores).ToListAsync();
        filtros.Categorias = await contexto.Categorias.AsNoTracking().OrderBy(categoria => categoria.Nombre).ToListAsync();
        return filtros;
    }

    public Task<Libro?> ObtenerAsync(int id) => contexto.Libros.AsNoTracking().Include(libro => libro.Autores)
        .Include(libro => libro.Categoria).Include(libro => libro.Editorial)
        .FirstOrDefaultAsync(libro => libro.Id == id && libro.Activo);
}
