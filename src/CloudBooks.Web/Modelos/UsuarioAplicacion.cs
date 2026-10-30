using Microsoft.AspNetCore.Identity;

namespace CloudBooks.Web.Modelos;

public class UsuarioAplicacion : IdentityUser
{
    public string NombreCompleto { get; set; } = string.Empty;
}
