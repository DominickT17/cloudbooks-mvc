using System.Net;
using System.Net.Mail;
using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using Microsoft.EntityFrameworkCore;

namespace CloudBooks.Web.Servicios;

public interface IServicioNotificaciones
{
    Task EncolarAsync(string clave, string tipo, string? referencia, string? destinatario, string asunto, string contenido);
    Task EncolarAdministracionAsync(string clave, string tipo, string? referencia, string asunto, string contenido);
}

public class ServicioNotificaciones(ContextoAplicacion contexto, IConfiguration configuracion) : IServicioNotificaciones
{
    public async Task EncolarAdministracionAsync(string clave, string tipo, string? referencia, string asunto, string contenido)
    {
        if (tipo != "revision" && !await contexto.AvisosAdministrativos.AnyAsync(a => a.ClaveEvento == clave))
            contexto.AvisosAdministrativos.Add(new AvisoAdministrativo { ClaveEvento = clave, Titulo = asunto, Mensaje = contenido.Length <= 500 ? contenido : contenido[..500], Referencia = referencia, FechaUtc = DateTime.UtcNow });
        var direcciones = (configuracion["Correo:Administradores"] ?? "dominickt1703@gmail.com").Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (var direccion in direcciones)
        {
            var sufijo = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(direccion.ToLowerInvariant())))[..16];
            await EncolarAsync($"{clave}/{sufijo}", tipo, referencia, direccion, asunto, contenido);
        }
    }
    public async Task EncolarAsync(string clave, string tipo, string? referencia, string? destinatario, string asunto, string contenido)
    {
        if (string.IsNullOrWhiteSpace(destinatario) || !MailAddress.TryCreate(destinatario, out _)) return;
        if (await contexto.CorreosPendientes.AnyAsync(c => c.ClaveEvento == clave)) return;
        var baseUrl = configuracion["Correo:UrlBase"]?.TrimEnd('/');
        var enlace = Uri.TryCreate(baseUrl, UriKind.Absolute, out var url) && (url.Scheme == "https" || url.Scheme == "http") ? WebUtility.HtmlEncode(baseUrl) : "";
        var partes = clave.Split('/');
        var numero = partes.Length > 1 && int.TryParse(partes[1], out var id) ? id : 0;
        var ruta = partes[0] switch { "pedido" when clave.Contains("/admin") => $"/Administracion/DetallePedido/{numero}", "pedido" => $"/Pedidos/Detalle/{numero}", "cotizacion" => $"/Cotizaciones/Detalle/{numero}", "institucion" when clave.Contains("/admin") => $"/AdministracionInstituciones/Detalle/{numero}", "institucion" => "/Instituciones/MisSolicitudes", "solicitud" when clave.Contains("/admin") => $"/AdministracionSolicitudes/Detalle/{numero}", "solicitud" => $"/Asesor/Detalle/{numero}", _ => "" };
        var html = $"<!doctype html><html lang=\"es\"><body style=\"margin:0;background:#f1f6fb;font-family:Arial,sans-serif;color:#25364b\"><div style=\"max-width:600px;margin:auto;background:white;padding:30px\">" +
            (enlace.Length == 0 ? "<h1 style=\"color:#167ce0\">CloudBooks</h1>" : $"<img src=\"{enlace}/imagenes/marca/cloudbooks.png\" alt=\"CloudBooks\" width=\"160\" />") +
            $"<h2>{WebUtility.HtmlEncode(asunto)}</h2><p style=\"line-height:1.6;white-space:pre-line\">{WebUtility.HtmlEncode(contenido)}</p>" +
            (enlace.Length == 0 ? "" : $"<p><a href=\"{enlace}{ruta}\" style=\"background:#167ce0;color:white;padding:12px;display:inline-block\">Abrir CloudBooks</a></p>") +
            (enlace.Length > 0 && tipo == "aprobado" ? $"<p><a href=\"{enlace}/Pedidos/Comprobante/{numero}\">Descargar comprobante</a></p>" : "") +
            "<p><a href=\"https://wa.me/50255751509\">Contactar por WhatsApp</a></p><small>CloudBooks · Simulación académica</small></div></body></html>";
        contexto.CorreosPendientes.Add(new CorreoPendiente { ClaveEvento = clave, Tipo = tipo, Referencia = referencia, Destinatario = destinatario, Asunto = asunto, Html = html, CreadoUtc = DateTime.UtcNow, Estado = EstadoCorreo.Pendiente });
    }
}
