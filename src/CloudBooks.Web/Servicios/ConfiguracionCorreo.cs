using System.Net.Mail;

namespace CloudBooks.Web.Servicios;

public static class ConfiguracionCorreo
{
    public static string? MarcaRealCitein(IConfiguration configuracion) =>
        Guid.TryParseExact(configuracion["CiteinDemo:SesionCorreo"], "N", out var sesion) && sesion != Guid.Empty
            ? "CR1" + Convert.ToBase64String(sesion.ToByteArray()).TrimEnd('=').Replace('+', '-').Replace('/', '_') : null;
    public const string MarcaSimuladoCitein = "CiteinSimuladoV1";
    public static string? ObtenerClave(IConfiguration configuracion) =>
        string.IsNullOrWhiteSpace(configuracion["Resend:ApiKey"]) ? configuracion["Correo:ApiKey"] : configuracion["Resend:ApiKey"];
    public static bool UsarResend(IConfiguration configuracion) =>
        string.Equals(configuracion["Correo:Proveedor"], "Resend", StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(ObtenerClave(configuracion))
        && MailAddress.TryCreate(configuracion["Correo:Remitente"], out _);
    public static bool DireccionPublica(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor) || !MailAddress.TryCreate(valor, out var correo) ||
            !string.Equals(valor.Trim(), correo.Address, StringComparison.OrdinalIgnoreCase)) return false;
        var dominio = correo.Host.ToLowerInvariant();
        return Uri.CheckHostName(dominio) == UriHostNameType.Dns && dominio.Contains('.') &&
            !new[] { ".invalid", ".local", ".localhost", ".test", ".example" }.Any(dominio.EndsWith);
    }
    public static void ValidarCitein(IConfiguration config)
    {
        var proveedor = config["Correo:Proveedor"] ?? "Simulado";
        if (proveedor.Equals("Simulado", StringComparison.OrdinalIgnoreCase)) return;
        if (!proveedor.Equals("Resend", StringComparison.OrdinalIgnoreCase) || !config.GetValue<bool>("CiteinDemo:CorreoReal"))
            throw new InvalidOperationException("CITEIN requiere Simulado o una activación explícita de correo real.");
        var dominio = config["Correo:DominioVerificado"]?.Trim();
        if (!UsarResend(config) || !MailAddress.TryCreate(config["Correo:Remitente"], out var remitente) ||
            !DireccionPublica(remitente.Address) || string.IsNullOrWhiteSpace(dominio) ||
            !string.Equals(remitente.Host, dominio, StringComparison.OrdinalIgnoreCase) ||
            dominio.Equals("resend.dev", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Resend requiere clave, remitente y Correo:DominioVerificado coincidente con un dominio propio verificado.");
        if (MarcaRealCitein(config) == null) throw new InvalidOperationException("CITEIN requiere una sesión de correo real nueva.");
        if (!DireccionPublica(config["CiteinDemo:DestinatarioAutorizado"]))
            throw new InvalidOperationException("CITEIN requiere una dirección pública autorizada para esta ejecución.");
    }
    public static bool EsEventoCliente(string clave, string tipo, out int pedidoId)
    {
        pedidoId = 0;
        var partes = clave.Split('/');
        return partes.Length == 4 && partes[0] == "pedido" && int.TryParse(partes[1], out pedidoId) && pedidoId > 0 &&
            partes[2] == tipo && partes[3] == "cliente" && tipo is "aprobado" or "revision" or "cancelado";
    }
    public static bool AutorizadoCitein(IConfiguration config, string clave, string tipo, string destinatario) =>
        config.GetValue<bool>("CiteinDemo:Enabled") && config.GetValue<bool>("CiteinDemo:CorreoReal") && UsarResend(config) && MarcaRealCitein(config) != null &&
        DireccionPublica(destinatario) && string.Equals(destinatario.Trim(), config["CiteinDemo:DestinatarioAutorizado"]?.Trim(), StringComparison.OrdinalIgnoreCase) &&
        EsEventoCliente(clave, tipo, out _);
}
