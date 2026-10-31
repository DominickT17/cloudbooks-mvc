using System.Text.Json;
using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using Microsoft.EntityFrameworkCore;

namespace CloudBooks.Web.Servicios;

public class ServicioEscaparate(ContextoAplicacion contexto)
{
    private const string Clave = "VistosRecientemente";
    public void RegistrarVista(ISession sesion, int id)
    {
        var ids = LeerVistas(sesion); ids.Remove(id); ids.Insert(0, id);
        sesion.SetString(Clave, JsonSerializer.Serialize(ids.Take(5)));
    }
    private static List<int> LeerVistas(ISession sesion) => JsonSerializer.Deserialize<List<int>>(sesion.GetString(Clave) ?? "[]") ?? [];
    public async Task<(List<Libro> Libros, bool SonMasVendidos)> SeleccionarVentasAsync()
    {
        var ids = await contexto.DetallesPedido.AsNoTracking().Where(d => d.Libro.Activo && (d.Pedido.Estado == EstadoPedido.Aprobado || d.Pedido.Estado == EstadoPedido.Enviado))
            .GroupBy(d => d.LibroId).Select(g => new { Id = g.Key, Cantidad = g.Sum(d => d.Cantidad) }).OrderByDescending(x => x.Cantidad).ThenBy(x => x.Id).Take(4).ToListAsync();
        if (ids.Count < 4) return (await contexto.Libros.AsNoTracking().Where(l => l.Activo).Include(l => l.Autores).OrderBy(l => l.Titulo).Take(4).ToListAsync(), false);
        var identificadores = ids.Select(x => x.Id).ToArray();
        var libros = await contexto.Libros.AsNoTracking().Where(l => identificadores.Contains(l.Id)).Include(l => l.Autores).ToListAsync();
        return (ids.Select(x => libros.Single(l => l.Id == x.Id)).ToList(), true);
    }
    public Task<List<Libro>> RecomendadosAsync() => contexto.Libros.AsNoTracking().Where(l => l.Activo && l.Destacado).Include(l => l.Autores).OrderBy(l => l.Titulo).Take(4).ToListAsync();
    public async Task<List<Libro>> RecientesAsync(ISession sesion)
    {
        var ids = LeerVistas(sesion); var libros = await contexto.Libros.AsNoTracking().Where(l => l.Activo && ids.Contains(l.Id)).Include(l => l.Autores).ToListAsync();
        return ids.Select(id => libros.FirstOrDefault(l => l.Id == id)).OfType<Libro>().ToList();
    }
    public Task<List<Categoria>> CategoriasAsync() => contexto.Categorias.AsNoTracking().Where(c => contexto.Libros.Any(l => l.Activo && l.CategoriaId == c.Id)).OrderBy(c => c.Nombre).ToListAsync();
}
