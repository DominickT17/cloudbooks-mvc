using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using Microsoft.EntityFrameworkCore;

namespace CloudBooks.Web.Servicios;

public class ServicioParametros(ContextoAplicacion contexto)
{
    public Task<ParametrosSistema> ObtenerAsync() => contexto.ConfiguracionSistema.AsNoTracking().SingleAsync(p => p.Id == 1);
    public async Task<string?> GuardarAsync(ParametrosSistema datos)
    {
        if (datos.TasaImpuesto < 0 || datos.TasaImpuesto > 1 || decimal.Round(datos.TasaImpuesto, 4) != datos.TasaImpuesto || datos.UmbralStockBajo is < 0 or > 1000000 || datos.DiasVigencia is < 1 or > 365 || datos.MontoRevision <= 0 || datos.MontoRevision > 1000000000m || datos.CantidadRevisionMinorista is < 1 or > 1000000 || datos.PorcentajeRevisionInventario is < 1 or > 100) return "Revisa los rangos de los parámetros.";
        var cambios = await contexto.ConfiguracionSistema.Where(p => p.Id == 1).ExecuteUpdateAsync(s => s.SetProperty(p => p.TasaImpuesto, datos.TasaImpuesto).SetProperty(p => p.UmbralStockBajo, datos.UmbralStockBajo).SetProperty(p => p.DiasVigencia, datos.DiasVigencia).SetProperty(p => p.MontoRevision, datos.MontoRevision).SetProperty(p => p.CantidadRevisionMinorista, datos.CantidadRevisionMinorista).SetProperty(p => p.PorcentajeRevisionInventario, datos.PorcentajeRevisionInventario));
        return cambios == 1 ? null : "No se encontraron los parámetros del sistema.";
    }
}
