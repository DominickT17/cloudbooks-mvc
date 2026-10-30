using System.ComponentModel.DataAnnotations;

namespace CloudBooks.Web.ModelosVista;

public class SolicitudInstitucionalModelo
{
    [Required(ErrorMessage = "Ingresa el nombre de la institución."), StringLength(160)] public string Institucion { get; set; } = "";
    [Required(ErrorMessage = "Ingresa el NIT."), StringLength(30)] public string Nit { get; set; } = "";
    [Required(ErrorMessage = "Ingresa el tipo de institución."), StringLength(100), Display(Name = "Tipo de institución")] public string TipoInstitucion { get; set; } = "";
    [Required(ErrorMessage = "Ingresa el teléfono."), StringLength(30), Display(Name = "Teléfono")] public string Telefono { get; set; } = "";
    [Required(ErrorMessage = "Ingresa la dirección."), StringLength(250), Display(Name = "Dirección")] public string Direccion { get; set; } = "";
    [Required(ErrorMessage = "Ingresa el nombre del representante."), StringLength(160)] public string Representante { get; set; } = "";
}
