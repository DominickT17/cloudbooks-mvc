using CloudBooks.Web.Modelos;
using CloudBooks.Web.Servicios;

namespace CloudBooks.Web.ModelosVista;

public class CarritoModelo
{
    public List<DetalleCarrito> Detalles { get; set; } = [];
    public ResumenComercial? Resumen { get; set; }
    public int CantidadTotal { get; set; }
    public bool Institucional { get; set; }
}
