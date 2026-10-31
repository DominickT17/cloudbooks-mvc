using System.ComponentModel.DataAnnotations;

namespace CloudBooks.Web.Modelos;

public class NotificacionPedido
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public Pedido Pedido { get; set; } = null!;
    [Required, StringLength(30)] public string Destinatario { get; set; } = "";
    [Required, StringLength(500)] public string Mensaje { get; set; } = "";
    public DateTime FechaUtc { get; set; }
}
