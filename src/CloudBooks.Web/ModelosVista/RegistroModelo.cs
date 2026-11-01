using System.ComponentModel.DataAnnotations;

namespace CloudBooks.Web.ModelosVista;

public class RegistroModelo
{
    public string? Retorno { get; set; }
    [Required(ErrorMessage = "Ingresa tu nombre."), StringLength(80)] public string Nombre { get; set; } = "";
    [Required(ErrorMessage = "Ingresa tu apellido."), StringLength(80)] public string Apellido { get; set; } = "";
    [Required(ErrorMessage = "Ingresa tu correo."), EmailAddress(ErrorMessage = "Ingresa un correo válido.")] public string Correo { get; set; } = "";
    [Display(Name = "Teléfono (opcional)")] public string? Telefono { get; set; }
    [Required(ErrorMessage = "Ingresa una contraseña."), DataType(DataType.Password), StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")] public string Contrasena { get; set; } = "";
    [Required(ErrorMessage = "Confirma tu contraseña."), DataType(DataType.Password), Compare(nameof(Contrasena), ErrorMessage = "Las contraseñas no coinciden."), Display(Name = "Confirmar contraseña")] public string Confirmacion { get; set; } = "";
}
