using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;

namespace CloudBooks.Web.Servicios;

public interface INotificacionesPedido
{
    void RegistrarRevision(Pedido pedido);
    void RegistrarAprobacion(Pedido pedido);
}

// Registro local; una integración externa podrá implementarse sin cambiar el flujo del pedido.
public class NotificacionesPedido(ContextoAplicacion contexto) : INotificacionesPedido
{
    public void RegistrarAprobacion(Pedido pedido) => contexto.NotificacionesPedido.Add(new NotificacionPedido { Pedido = pedido, Destinatario = "Cliente", FechaUtc = DateTime.UtcNow, Mensaje = "Tu pedido fue aprobado y está listo para prepararse." });
    public void RegistrarRevision(Pedido pedido)
    {
        contexto.NotificacionesPedido.AddRange(
            new NotificacionPedido { Pedido = pedido, Destinatario = "Cliente", FechaUtc = DateTime.UtcNow, Mensaje = "Tu pedido requiere una revisión. Nuestro equipo lo validará pronto." },
            new NotificacionPedido { Pedido = pedido, Destinatario = "Administración", FechaUtc = DateTime.UtcNow, Mensaje = "Un pedido requiere validación adicional antes de continuar." });
    }
}
