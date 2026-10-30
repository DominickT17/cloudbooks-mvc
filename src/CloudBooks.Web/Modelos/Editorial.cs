using System.ComponentModel.DataAnnotations;

namespace CloudBooks.Web.Modelos;

public class Editorial
{
    public int Id { get; set; }
    [Required, StringLength(150)] public string Nombre { get; set; } = string.Empty;
}
