using System.Diagnostics;
using CloudBooks.Web.Models;
using CloudBooks.Web.Servicios;
using CloudBooks.Web.ModelosVista;
using Microsoft.AspNetCore.Mvc;

namespace CloudBooks.Web.Controllers;

public class InicioController(ServicioEscaparate escaparate, ServicioParametros parametros) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewData["Umbral"] = (await parametros.ObtenerAsync()).UmbralStockBajo;
        var ventas = await escaparate.SeleccionarVentasAsync();
        return View(new InicioModelo { Libros = ventas.Libros, SonMasVendidos = ventas.SonMasVendidos, Recomendados = await escaparate.RecomendadosAsync(), Recientes = await escaparate.RecientesAsync(HttpContext.Session), Categorias = await escaparate.CategoriasAsync() });
    }
    public IActionResult Instituciones() => View();
    public IActionResult Carrito() => RedirectToAction("Index", "Carrito");
    public IActionResult Estado(int codigo) { ViewData["Codigo"] = codigo; return View(); }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
