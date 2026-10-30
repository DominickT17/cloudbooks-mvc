using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudBooks.Web.Modelos;

public enum EstadoPedido { Pendiente, Aprobado, Enviado, Cancelado }
public enum MetodoPagoSimulado { TarjetaSimulada, TransferenciaSimulada }

public class Pedido
{
    public int Id { get; set; }
    [NotMapped] public string Codigo => $"PED-{Id:000000}";
    [Required] public string UsuarioId { get; set; } = "";
    public UsuarioAplicacion Usuario { get; set; } = null!;
    public DateTime FechaUtc { get; set; }
    public EstadoPedido Estado { get; set; }
    public MetodoPagoSimulado MetodoPago { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DescuentoVolumen { get; set; }
    public decimal DescuentoInstitucional { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Total { get; set; }
    public List<DetallePedido> Detalles { get; set; } = [];
}
