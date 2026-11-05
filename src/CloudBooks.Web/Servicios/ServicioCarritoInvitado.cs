using System.Text.Json;
using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using Microsoft.EntityFrameworkCore;

namespace CloudBooks.Web.Servicios;

// El identificador de libro y la cantidad son los únicos datos guardados en sesión.
public class ServicioCarritoInvitado(ContextoAplicacion contexto, ServicioCarrito carrito)
{
    private const string Clave = "CarritoInvitado";
    private static Dictionary<int, int> Leer(ISession sesion)
    {
        var json = sesion.GetString(Clave);
        return json == null ? [] : JsonSerializer.Deserialize<Dictionary<int, int>>(json) ?? [];
    }
    private static void Guardar(ISession sesion, Dictionary<int, int> items)
    {
        if (items.Count == 0) sesion.Remove(Clave);
        else sesion.SetString(Clave, JsonSerializer.Serialize(items));
    }
    public int Cantidad(ISession sesion) => Leer(sesion).Values.Sum();
    public async Task<List<DetalleCarrito>> ObtenerAsync(ISession sesion)
    {
        var items = Leer(sesion);
        var libros = await contexto.Libros.AsNoTracking().Where(l => items.Keys.Contains(l.Id)).ToListAsync();
        return libros.Select(l => new DetalleCarrito { Id = -l.Id, LibroId = l.Id, Libro = l, Cantidad = items[l.Id] }).ToList();
    }
    public async Task<string?> AgregarAsync(ISession sesion, int libroId, int cantidad)
    {
        var items = Leer(sesion);
        if (cantidad < 1 || cantidad > 10000) return "La cantidad debe estar entre 1 y 10000.";
        long final = (long)items.GetValueOrDefault(libroId) + cantidad;
        if (final > 10000) return "La cantidad debe estar entre 1 y 10000.";
        return await EstablecerAsync(sesion, libroId, (int)final);
    }
    public async Task<string?> EstablecerAsync(ISession sesion, int libroId, int cantidad)
    {
        var items = Leer(sesion);
        if (cantidad < 1 || cantidad > 10000 || items.Where(x => x.Key != libroId).Sum(x => (long)x.Value) + cantidad > 10000) return "La cantidad supera el límite del carrito.";
        var libro = await contexto.Libros.AsNoTracking().FirstOrDefaultAsync(l => l.Id == libroId && l.Activo);
        if (libro == null) return "El libro no está disponible.";
        if (libro.Formato != FormatoLibro.Ebook && cantidad > libro.Stock) return "La cantidad supera las existencias disponibles.";
        items[libroId] = cantidad;
        Guardar(sesion, items);
        return null;
    }
    public void Eliminar(ISession sesion, int libroId) { var items = Leer(sesion); items.Remove(libroId); Guardar(sesion, items); }
    public async Task<bool> FusionarAsync(ISession sesion, string usuarioId)
    {
        var items = Leer(sesion);
        foreach (var item in items.ToArray())
        {
            if (await carrito.AgregarAsync(usuarioId, item.Key, item.Value) == null)
            {
                items.Remove(item.Key);
                Guardar(sesion, items);
            }
        }
        return items.Count == 0;
    }
}
