using CloudBooks.Web.Modelos;
using CloudBooks.Web.ModelosVista;
using CloudBooks.Web.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CloudBooks.Web.Controllers;

public class CuentaController(UserManager<UsuarioAplicacion> usuarios, SignInManager<UsuarioAplicacion> acceso, ServicioCarritoInvitado invitado) : Controller
{
    [HttpGet] public IActionResult Registrar(string? returnUrl) => View(new RegistroModelo { Retorno = returnUrl });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar(RegistroModelo modelo)
    {
        if (!string.IsNullOrWhiteSpace(modelo.Telefono) && ContactoWhatsapp.NormalizarGuatemala(modelo.Telefono) == null)
            ModelState.AddModelError(nameof(modelo.Telefono), "Ingresa un teléfono de Guatemala de 8 dígitos.");
        if (!ModelState.IsValid) return View(modelo);
        var usuario = new UsuarioAplicacion { UserName = modelo.Correo.Trim(), Email = modelo.Correo.Trim(), NombreCompleto = $"{modelo.Nombre.Trim()} {modelo.Apellido.Trim()}", PhoneNumber = ContactoWhatsapp.NormalizarGuatemala(modelo.Telefono) };
        var resultado = await usuarios.CreateAsync(usuario, modelo.Contrasena);
        if (!resultado.Succeeded)
        {
            foreach (var error in resultado.Errors) ModelState.AddModelError("", Mensaje(error));
            return View(modelo);
        }
        var rol = await usuarios.AddToRoleAsync(usuario, "ClienteMinorista");
        if (!rol.Succeeded)
        {
            var eliminacion = await usuarios.DeleteAsync(usuario);
            if (!eliminacion.Succeeded) throw new InvalidOperationException("No se pudo revertir la creación de la cuenta.");
            ModelState.AddModelError("", "No fue posible completar el registro. Inténtalo nuevamente.");
            return View(modelo);
        }
        await acceso.SignInAsync(usuario, isPersistent: false);
        if (!await invitado.FusionarAsync(HttpContext.Session, usuario.Id)) { TempData["Error"] = "Revisa los productos pendientes de tu carrito."; return RedirectToAction("Index", "Carrito"); }
        return Url.IsLocalUrl(modelo.Retorno) ? LocalRedirect(modelo.Retorno!) : RedirectToAction("Index", "Inicio");
    }

    [HttpGet] public IActionResult Ingresar(string? returnUrl) => View(new IngresoModelo { Retorno = returnUrl });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Ingresar(IngresoModelo modelo)
    {
        if (!ModelState.IsValid) return View(modelo);
        var usuario = await usuarios.FindByEmailAsync(modelo.Correo.Trim());
        var resultado = usuario == null ? Microsoft.AspNetCore.Identity.SignInResult.Failed : await acceso.PasswordSignInAsync(usuario, modelo.Contrasena, modelo.Recordarme, lockoutOnFailure: true);
        if (!resultado.Succeeded)
        {
            ModelState.AddModelError("", "No fue posible iniciar sesión. Revisa tus datos o inténtalo más tarde.");
            return View(modelo);
        }
        if (!await invitado.FusionarAsync(HttpContext.Session, usuario!.Id)) { TempData["Error"] = "Revisa los productos pendientes de tu carrito."; return RedirectToAction("Index", "Carrito"); }
        return Url.IsLocalUrl(modelo.Retorno) ? LocalRedirect(modelo.Retorno!) : RedirectToAction("Index", "Inicio");
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Salir()
    {
        await acceso.SignOutAsync();
        return RedirectToAction("Index", "Inicio");
    }

    [Authorize] public async Task<IActionResult> Index() => View(await usuarios.GetUserAsync(User));
    public IActionResult AccesoDenegado() => View();

    private static string Mensaje(IdentityError error) => error.Code switch
    {
        "DuplicateUserName" or "DuplicateEmail" => "Ya existe una cuenta con este correo.",
        "PasswordTooShort" => "La contraseña debe tener al menos 6 caracteres.",
        "PasswordRequiresNonAlphanumeric" => "La contraseña debe incluir un símbolo.",
        "PasswordRequiresDigit" => "La contraseña debe incluir un número.",
        "PasswordRequiresUpper" => "La contraseña debe incluir una mayúscula.",
        "PasswordRequiresLower" => "La contraseña debe incluir una minúscula.",
        _ => "No fue posible crear la cuenta. Revisa los datos ingresados."
    };
}
