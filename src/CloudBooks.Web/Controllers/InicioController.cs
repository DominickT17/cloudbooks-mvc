using System.Diagnostics;
using CloudBooks.Web.Models;
using CloudBooks.Web.Servicios;
using CloudBooks.Web.ModelosVista;
using Microsoft.AspNetCore.Mvc;

namespace CloudBooks.Web.Controllers;

public class InicioController(ServicioCatalogo catalogo, ServicioParametros parametros) : Controller
{
    public async Task<IActionResult> Index() { ViewData["Umbral"] = (await parametros.ObtenerAsync()).UmbralStockBajo; return View((await catalogo.BuscarAsync(new CatalogoModelo())).Libros.Take(4).ToList()); }
    public IActionResult Instituciones() => View();
    public IActionResult Carrito() => RedirectToAction("Index", "Carrito");
    public IActionResult Estado(int codigo) { ViewData["Codigo"] = codigo; return View(); }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
