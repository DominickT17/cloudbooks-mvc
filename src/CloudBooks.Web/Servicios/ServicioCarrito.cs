using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using Microsoft.EntityFrameworkCore;

namespace CloudBooks.Web.Servicios;

public class ServicioCarrito(ContextoAplicacion contexto)
{
    public Task<Carrito?> ObtenerAsync(string usuarioId) => contexto.Carritos.AsNoTracking()
        .Include(c => c.Detalles).ThenInclude(d => d.Libro)
        .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);

    public async Task<string?> AgregarAsync(string usuarioId, int libroId, int cantidad)
    {
        if (cantidad < 1 || cantidad > 10000) return "La cantidad debe estar entre 1 y 10000.";
        var libro = await contexto.Libros.FirstOrDefaultAsync(l => l.Id == libroId && l.Activo);
        if (libro == null) return "El libro no está disponible.";
        var carrito = await contexto.Carritos.Include(c => c.Detalles).FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);
        var detalle = carrito?.Detalles.FirstOrDefault(d => d.LibroId == libroId);
        long cantidadFinal = (long)(detalle?.Cantidad ?? 0) + cantidad;
        if (cantidadFinal > 10000 || (carrito?.Detalles.Sum(d => (long)d.Cantidad) ?? 0) + cantidad > 10000 || libro.Formato != FormatoLibro.Ebook && cantidadFinal > libro.Stock)
            return "La cantidad supera las existencias disponibles.";
        if (carrito == null) { carrito = new Carrito { UsuarioId = usuarioId }; contexto.Carritos.Add(carrito); }
        if (detalle == null) carrito.Detalles.Add(new DetalleCarrito { LibroId = libroId, Cantidad = (int)cantidadFinal });
        else detalle.Cantidad = (int)cantidadFinal;
        await contexto.SaveChangesAsync();
        return null;
    }

    public async Task<string?> ActualizarAsync(string usuarioId, int detalleId, int cantidad)
    {
        var detalle = await contexto.DetallesCarrito.Include(d => d.Carrito).Include(d => d.Libro)
            .FirstOrDefaultAsync(d => d.Id == detalleId && d.Carrito.UsuarioId == usuarioId);
        if (detalle == null) return "El producto no existe en tu carrito.";
        if (cantidad < 1 || cantidad > 10000) return "La cantidad debe estar entre 1 y 10000.";
        var totalOtros = await contexto.DetallesCarrito.Where(d => d.CarritoId == detalle.CarritoId && d.Id != detalleId).SumAsync(d => (long?)d.Cantidad) ?? 0;
        if (totalOtros + cantidad > 10000) return "El carrito no puede superar 10000 unidades.";
        if (!detalle.Libro.Activo) return "El libro ya no está disponible; puedes retirarlo del carrito.";
        if (detalle.Libro.Formato != FormatoLibro.Ebook && cantidad > detalle.Libro.Stock) return "La cantidad supera las existencias disponibles.";
        detalle.Cantidad = cantidad;
        await contexto.SaveChangesAsync();
        return null;
    }

    public async Task EliminarAsync(string usuarioId, int detalleId)
    {
        var detalle = await contexto.DetallesCarrito.FirstOrDefaultAsync(d => d.Id == detalleId && d.Carrito.UsuarioId == usuarioId);
        if (detalle != null) { contexto.DetallesCarrito.Remove(detalle); await contexto.SaveChangesAsync(); }
    }
}
