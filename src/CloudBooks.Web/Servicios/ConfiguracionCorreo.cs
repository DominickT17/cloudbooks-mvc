namespace CloudBooks.Web.Servicios;

public static class ConfiguracionCorreo
{
    public static string? ObtenerClave(IConfiguration configuracion) =>
        string.IsNullOrWhiteSpace(configuracion["Resend:ApiKey"]) ? configuracion["Correo:ApiKey"] : configuracion["Resend:ApiKey"];

    public static bool UsarResend(IConfiguration configuracion) =>
        string.Equals(configuracion["Correo:Proveedor"], "Resend", StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(ObtenerClave(configuracion))
        && System.Net.Mail.MailAddress.TryCreate(configuracion["Correo:Remitente"], out _);
}
