using CloudBooks.Web.ModelosVista;
using CloudBooks.Web.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace CloudBooks.Web.Controllers;

public class CatalogoController(ServicioCatalogo catalogo, ServicioParametros parametros) : Controller
{
    public async Task<IActionResult> Index([FromQuery] CatalogoModelo filtros)
    {
        if (filtros.PrecioMinimo < 0 || filtros.PrecioMaximo < 0 || filtros.PrecioMinimo > filtros.PrecioMaximo)
            ModelState.AddModelError("", "Revisa el rango de precios: debe ser positivo y el mínimo no puede superar el máximo.");
        if (!ModelState.IsValid)
        {
            filtros = await catalogo.BuscarAsync(new CatalogoModelo());
            filtros.Libros.Clear();
            return View(filtros);
        }
        return View(await catalogo.BuscarAsync(filtros));
    }

    public async Task<IActionResult> Detalle(int id)
    {
        var libro = await catalogo.ObtenerAsync(id);
        ViewBag.Umbral = (await parametros.ObtenerAsync()).UmbralStockBajo;
        return libro == null ? NotFound() : View(libro);
    }
}
