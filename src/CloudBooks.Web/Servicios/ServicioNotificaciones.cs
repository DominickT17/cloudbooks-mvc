using System.Net.Mail;
using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using Microsoft.EntityFrameworkCore;

namespace CloudBooks.Web.Servicios;

public interface IServicioNotificaciones
{
    Task EncolarAsync(string clave, string tipo, string? referencia, string? destinatario, string asunto, string contenido, byte[]? comprobantePdf = null);
    Task EncolarAdministracionAsync(string clave, string tipo, string? referencia, string asunto, string contenido);
}

public class ServicioNotificaciones(ContextoAplicacion contexto, IConfiguration configuracion, IWebHostEnvironment? entorno = null, ILogger<ServicioNotificaciones>? logger = null) : IServicioNotificaciones
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
    public async Task EncolarAsync(string clave, string tipo, string? referencia, string? destinatario, string asunto, string contenido, byte[]? comprobantePdf = null)
    {
        if (string.IsNullOrWhiteSpace(destinatario) || !MailAddress.TryCreate(destinatario, out _)) return;
        if (await contexto.CorreosPendientes.AnyAsync(c => c.ClaveEvento == clave)) return;
        var autorizado = ConfiguracionCorreo.AutorizadoCitein(configuracion, clave, tipo, destinatario);
        byte[]? logo = null;
        if (autorizado && entorno != null)
        {
            try { logo = await File.ReadAllBytesAsync(Path.Combine(entorno.WebRootPath, "imagenes", "marca", "cloudbooks.png")); }
            catch (Exception error) { logger?.LogWarning("No se pudo incluir el logo del correo: {Tipo}", error.GetType().Name); }
        }
        var plantilla = PlantillasCorreo.Crear(clave, tipo, asunto, contenido, configuracion["Correo:UrlBase"], configuracion["Correo:LogoUrl"], logo != null);
        contexto.CorreosPendientes.Add(new CorreoPendiente { ClaveEvento = clave, Tipo = tipo, Referencia = referencia, Destinatario = destinatario, Asunto = asunto, Html = plantilla.Html, TextoPlano = plantilla.Texto, CreadoUtc = DateTime.UtcNow, Estado = EstadoCorreo.Pendiente, ComprobantePdf = comprobantePdf, LogoCorreo = logo, Proveedor = configuracion.GetValue<bool>("CiteinDemo:Enabled") ? (autorizado ? ConfiguracionCorreo.MarcaRealCitein(configuracion) : ConfiguracionCorreo.MarcaSimuladoCitein) : null });
    }
}
