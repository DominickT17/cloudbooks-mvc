using System.ComponentModel.DataAnnotations;

namespace CloudBooks.Web.ModelosVista;

public class IngresoModelo
{
    [Required(ErrorMessage = "Ingresa tu correo."), EmailAddress(ErrorMessage = "Ingresa un correo válido.")] public string Correo { get; set; } = "";
    [Required(ErrorMessage = "Ingresa tu contraseña."), DataType(DataType.Password)] public string Contrasena { get; set; } = "";
    [Display(Name = "Recordarme")] public bool Recordarme { get; set; }
    public string? Retorno { get; set; }
}
