using System.ComponentModel.DataAnnotations;

namespace CloudBooks.Web.Modelos;

public class PerfilInstitucional
{
    public int Id { get; set; }
    [Required] public string UsuarioId { get; set; } = "";
    public UsuarioAplicacion Usuario { get; set; } = null!;
    [Required, StringLength(160)] public string Institucion { get; set; } = "";
    [Required, StringLength(30)] public string Nit { get; set; } = "";
    [Required, StringLength(100)] public string TipoInstitucion { get; set; } = "";
    [Required, StringLength(30)] public string Telefono { get; set; } = "";
    [Required, StringLength(250)] public string Direccion { get; set; } = "";
    [Required, StringLength(160)] public string Representante { get; set; } = "";
}
