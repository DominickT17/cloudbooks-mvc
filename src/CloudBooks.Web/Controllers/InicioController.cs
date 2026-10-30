using System.Diagnostics;
using CloudBooks.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CloudBooks.Web.Controllers;

public class InicioController : Controller
{
    public IActionResult Index() => View();
    public IActionResult Instituciones() => View();
    public IActionResult Carrito() => RedirectToAction("Index", "Carrito");
    public IActionResult Estado(int codigo) { ViewData["Codigo"] = codigo; return View(); }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
