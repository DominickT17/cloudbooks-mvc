using CloudBooks.Web.Servicios;
using CloudBooks.Web.Modelos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudBooks.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class AdministracionInventarioController(ServicioInventario inventario, ServicioParametros parametros) : Controller
{
    public async Task<IActionResult> Index(string? buscar, string? tipo)
    {
        var umbral = (await parametros.ObtenerAsync()).UmbralStockBajo;
        var libros = await inventario.ListarAsync(buscar);
        ViewBag.Umbral = umbral;
        ViewBag.Resumen = libros;
        return View(tipo switch
        {
            "disponibles" => libros.Where(l => l.Formato != FormatoLibro.Ebook && l.Stock > umbral).ToList(),
            "bajo" => libros.Where(l => l.Formato != FormatoLibro.Ebook && l.Stock > 0 && l.Stock <= umbral).ToList(),
            "agotados" => libros.Where(l => l.Formato != FormatoLibro.Ebook && l.Stock == 0).ToList(),
            "digitales" => libros.Where(l => l.Formato == FormatoLibro.Ebook).ToList(),
            _ => libros
        });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Actualizar(int id, int stockOriginal, int stockNuevo)
    {
        if (!ModelState.IsValid) return BadRequest();
        var error = await inventario.ActualizarAsync(id, stockOriginal, stockNuevo);
        TempData[error == null ? "Exito" : "Error"] = error ?? "Existencias actualizadas.";
        return RedirectToAction("Index");
    }
}
