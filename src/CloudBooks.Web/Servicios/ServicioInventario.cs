using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using Microsoft.EntityFrameworkCore;

namespace CloudBooks.Web.Servicios;

public class ServicioInventario(ContextoAplicacion contexto)
{
    public const int UmbralStockBajo = 5;
    public Task<List<Libro>> ListarAsync(string? buscar)
    {
        var consulta = contexto.Libros.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(buscar)) { var texto = buscar.Trim(); consulta = consulta.Where(l => l.Titulo.Contains(texto) || l.ISBN.Contains(texto)); }
        return consulta.OrderBy(l => l.Titulo).ToListAsync();
    }

    public async Task<string?> ActualizarAsync(int id, int stockOriginal, int stockNuevo)
    {
        if (stockNuevo < 0 || stockNuevo > 1000000) return "Ingresa un stock entre 0 y 1000000.";
        var actualizados = await contexto.Libros.Where(l => l.Id == id && l.Formato != FormatoLibro.Ebook && l.Stock == stockOriginal)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.Stock, stockNuevo));
        return actualizados == 1 ? null : "No se pudo actualizar: el libro es digital, no existe o su stock cambió. Recarga la página.";
    }
}
