using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudBooks.Web.Modelos;

public enum EstadoPedido { Pendiente, Aprobado, Enviado, Cancelado, EnRevision, CanceladoPorSeguridad }
public enum MetodoPagoSimulado { TarjetaSimulada, TransferenciaSimulada }
[Flags] public enum TipoRevisionPedido { Ninguna = 0, Seguridad = 1, Comercial = 2, Inventario = 4 }

public class Pedido
{
    public int Id { get; set; }
    [NotMapped] public string Codigo => $"PED-{Id:000000}";
    [Required] public string UsuarioId { get; set; } = "";
    public UsuarioAplicacion Usuario { get; set; } = null!;
    public DateTime FechaUtc { get; set; }
    public EstadoPedido Estado { get; set; }
    [NotMapped] public string EstadoTexto => Estado switch { EstadoPedido.EnRevision => "En revisión", EstadoPedido.CanceladoPorSeguridad => "Cancelado por seguridad", _ => Estado.ToString() };
    public MetodoPagoSimulado MetodoPago { get; set; }
    [StringLength(150)] public string? TitularTarjeta { get; set; }
    [StringLength(4)] public string? UltimosCuatro { get; set; }
    [StringLength(1000)] public string? MotivosRevision { get; set; }
    public bool AltaPrioridad { get; set; }
    public bool Revisado { get; set; }
    public TipoRevisionPedido TipoRevision { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DescuentoVolumen { get; set; }
    public decimal DescuentoInstitucional { get; set; }
    public decimal Impuesto { get; set; }
    public decimal TasaImpuesto { get; set; } = ParametrosSistema.ImpuestoInicial;
    public decimal Total { get; set; }
    public List<DetallePedido> Detalles { get; set; } = [];
}
