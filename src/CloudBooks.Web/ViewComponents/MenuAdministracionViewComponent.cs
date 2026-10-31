using CloudBooks.Web.Servicios;
using Microsoft.AspNetCore.Mvc;
namespace CloudBooks.Web.ViewComponents;
public class MenuAdministracionViewComponent(ServicioPendientesAdministrativos pendientes) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync() => View(await pendientes.ObtenerAsync());
}
