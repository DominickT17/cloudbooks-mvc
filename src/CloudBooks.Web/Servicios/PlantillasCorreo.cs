using System.Net;
using System.Text;

namespace CloudBooks.Web.Servicios;

public record PlantillaGenerada(string Html, string Texto);

public static class PlantillasCorreo
{
    public static string TextoDeHtmlAnterior(string asunto, string html)
    {
        var separado = System.Text.RegularExpressions.Regex.Replace(html, "</(?:p|div|h[1-6]|tr|table)>|<br\\s*/?>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return asunto + "\n\n" + WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Replace(separado, "<[^>]*>", " ")).Trim();
    }

    public static PlantillaGenerada Crear(string clave, string tipo, string asunto, string contenido, string? urlBase, string? urlLogo = null)
    {
        static string E(string? valor) => WebUtility.HtmlEncode(valor ?? "");
        static bool EsHttpsPublico(string? valor, out Uri? uri)
        {
            if (!Uri.TryCreate(valor, UriKind.Absolute, out uri) || uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.UserInfo) || uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith(".local", StringComparison.OrdinalIgnoreCase) || IPAddress.TryParse(uri.Host, out _) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)) { uri = null; return false; }
            return true;
        }
        var partes = clave.Split('/');
        var numero = partes.Length > 1 && int.TryParse(partes[1], out var id) && id > 0 ? id : 0;
        var admin = partes.Contains("admin", StringComparer.Ordinal);
        var (titulo, estado, color, accion) = (tipo, admin) switch
        {
            ("aprobado", false) => ("Tu pedido está confirmado", "APROBADO", "#126a53", "Ver mi pedido"),
            ("revision", false) => ("Estamos revisando tu pedido", "EN REVISIÓN", "#94610a", "Consultar mi pedido"),
            ("revision", true) => ("Un pedido requiere revisión", "ATENCIÓN", "#94610a", "Abrir en Administración"),
            ("pago_rechazado", _) => ("No pudimos procesar tu pago", "PAGO NO PROCESADO", "#a43d47", "Volver a mi carrito"),
            ("cancelado", _) => ("Actualización de tu pedido", "CANCELADO", "#6c5871", "Consultar mi pedido"),
            ("institucion_recibida", true) => ("Nueva solicitud institucional", "ATENCIÓN", "#1767a5", "Abrir en Administración"),
            ("institucion_recibida", false) => ("Solicitud institucional recibida", "SOLICITUD RECIBIDA", "#1767a5", "Consultar solicitud"),
            ("institucion_resuelta", _) => ("Actualización institucional", "SOLICITUD ACTUALIZADA", "#1767a5", "Consultar solicitud"),
            ("institucion_aprobada", _) => ("Tu solicitud fue aprobada", "APROBADA", "#126a53", "Consultar solicitud"),
            ("institucion_rechazada", _) => ("Actualización de tu solicitud", "NO APROBADA", "#6c5871", "Consultar solicitud"),
            ("solicitud_comercial", _) => ("Nueva solicitud comercial", "SOLICITUD RECIBIDA", "#1767a5", "Abrir solicitud"),
            ("solicitud_asignada", _) => ("Solicitud asignada", "ASIGNADA", "#1767a5", "Abrir solicitud"),
            ("cotizacion_emitida", true) => ("Cotización emitida", "COTIZACIÓN EMITIDA", "#1767a5", "Abrir en Administración"),
            ("cotizacion_emitida", false) => ("Tu cotización está disponible", "COTIZACIÓN EMITIDA", "#1767a5", "Consultar cotización"),
            ("cotizacion_respondida", _) => ("Respuesta a cotización", "COTIZACIÓN ACTUALIZADA", "#1767a5", "Consultar cotización"),
            ("cotizacion_aceptada", _) => ("Cotización aceptada", "ACEPTADA", "#126a53", "Consultar cotización"),
            ("cotizacion_rechazada", _) => ("Cotización rechazada", "RECHAZADA", "#6c5871", "Consultar cotización"),
            _ => ("Una actualización para ti", "NOTIFICACIÓN", "#1767a5", "Abrir CloudBooks")
        };
        if (admin && tipo != "revision") accion = "Abrir en Administración";
        var ruta = partes[0] switch
        {
            "pedido" when numero > 0 && admin => $"/Administracion/DetallePedido/{numero}",
            "pedido" when numero > 0 => $"/Pedidos/Detalle/{numero}",
            "intento" => "/Carrito",
            "cotizacion" when numero > 0 => $"/Cotizaciones/Detalle/{numero}",
            "institucion" when numero > 0 && admin => $"/AdministracionInstituciones/Detalle/{numero}",
            "institucion" => "/Instituciones/MisSolicitudes",
            "solicitud" when numero > 0 && admin => $"/AdministracionSolicitudes/Detalle/{numero}",
            "solicitud" when numero > 0 && tipo == "solicitud_asignada" => $"/Asesor/Detalle/{numero}",
            "solicitud" => "/Solicitudes",
            _ => ""
        };
        var baseSegura = EsHttpsPublico(urlBase, out var uriBase) ? uriBase!.GetLeftPart(UriPartial.Path).TrimEnd('/') : null;
        var logo = EsHttpsPublico(urlLogo, out var uriLogo) && new[] { ".png", ".jpg", ".jpeg" }.Any(extension => uriLogo!.AbsolutePath.EndsWith(extension, StringComparison.OrdinalIgnoreCase)) ? uriLogo!.AbsoluteUri : null;
        var enlace = baseSegura == null || ruta.Length == 0 ? null : baseSegura + ruta;
        var enlacePdf = baseSegura != null && numero > 0 ? tipo switch { "aprobado" when !admin => $"{baseSegura}/Pedidos/Comprobante/{numero}", "cotizacion_emitida" when !admin => $"{baseSegura}/Cotizaciones/Pdf/{numero}", _ => null } : null;
        var contenidoHtml = (tipo, admin) switch
        {
            ("aprobado", false) => ResumenPedido(contenido),
            ("cotizacion_emitida", false) => ResumenCotizacion(contenido),
            _ => string.Join("<br>", contenido.Replace("\r\n", "\n").Split('\n').Select(E))
        };
        var marca = logo == null ? "<span style=\"font-size:24px;font-weight:700;letter-spacing:-.5px;color:#ffffff\">CloudBooks</span>" : $"<img src=\"{E(logo)}\" alt=\"CloudBooks\" width=\"150\" style=\"display:block;border:0;max-width:150px;height:auto\">";
        var boton = enlace == null ? "" : $"<table role=\"presentation\" cellspacing=\"0\" cellpadding=\"0\" style=\"margin:26px 0 12px\"><tr><td bgcolor=\"#1478c9\" style=\"border-radius:6px\"><a href=\"{E(enlace)}\" style=\"display:inline-block;padding:14px 23px;color:#ffffff;font-weight:bold;font-size:14px;text-decoration:none\">{E(accion)} &rarr;</a></td></tr></table>";
        var pdf = enlacePdf == null ? "" : $"<p style=\"margin:14px 0 0;font-size:13px\"><a href=\"{E(enlacePdf)}\" style=\"color:#126bb0\">{(tipo == "aprobado" ? "Descargar comprobante" : "Descargar PDF de cotización")}</a> · Acceso con tu cuenta</p>";
        var preheader = E(titulo + " · " + (tipo == "pago_rechazado" ? "Tu carrito sigue disponible." : "Consulta los detalles en CloudBooks."));
        var html = $"""
<!doctype html><html lang="es"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>{E(asunto)}</title></head>
<body style="margin:0;padding:0;background:#edf3f8;color:#21354b;font-family:Arial,Helvetica,sans-serif"><div style="display:none;font-size:1px;line-height:1px;max-height:0;max-width:0;opacity:0;overflow:hidden">{preheader}</div>
<table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#edf3f8"><tr><td align="center" style="padding:28px 12px"><table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="width:100%;max-width:600px;background:#ffffff">
<tr><td bgcolor="#102e4b" style="padding:25px 30px">{marca}<div style="padding-top:7px;color:#b9d1e5;font-size:11px;letter-spacing:1.2px;text-transform:uppercase">Tu próxima lectura empieza aquí</div></td></tr>
<tr><td style="padding:{(admin ? "24px 24px 26px" : "34px 30px 32px")}"><span style="display:inline-block;background:#eef5fa;color:{color};font-size:11px;font-weight:bold;letter-spacing:1px;padding:8px 11px">&#9679; &nbsp;{E(estado)}</span><h1 style="margin:20px 0 18px;color:#102e4b;font-size:25px;line-height:1.25">{E(titulo)}</h1>
<table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="table-layout:fixed"><tr><td style="border-left:3px solid #bbdced;padding:0 0 0 16px;font-size:15px;line-height:1.7;color:#32485e;overflow-wrap:anywhere;word-wrap:break-word">{contenidoHtml}</td></tr></table>{boton}{pdf}
<p style="margin:28px 0 0;font-size:13px;line-height:1.6;color:#5b7081">¿Necesitas ayuda? <a href="https://wa.me/50255751509?text=Hola%2C%20necesito%20ayuda%20con%20CloudBooks." style="color:#126bb0">Contáctanos por WhatsApp</a>.</p></td></tr>
<tr><td bgcolor="#f5f9fc" style="padding:22px 30px;border-top:1px solid #e1ebf2;color:#607588;font-size:12px;line-height:1.6"><strong style="color:#183c5b">CloudBooks</strong> · Quetzaltenango, Guatemala<br>Plataforma académica · Operaciones de compra y pago simuladas.</td></tr>
</table></td></tr></table></body></html>
""";
        var texto = new StringBuilder().AppendLine("CloudBooks — " + titulo).AppendLine(estado).AppendLine().AppendLine(contenido);
        if (enlace != null) texto.AppendLine().AppendLine(accion + ": " + enlace);
        if (enlacePdf != null) texto.AppendLine("Documento (acceso con tu cuenta): " + enlacePdf);
        texto.AppendLine().AppendLine("Ayuda: https://wa.me/50255751509").Append("CloudBooks · Quetzaltenango, Guatemala · Simulación académica");
        return new PlantillaGenerada(html, texto.ToString());
    }

    private static string ResumenPedido(string contenido)
    {
        static string E(string valor) => WebUtility.HtmlEncode(valor);
        var lineas = contenido.Replace("\r\n", "\n").Split('\n');
        var inicio = Array.IndexOf(lineas, "Productos:");
        var subtotal = Array.FindIndex(lineas, l => l.StartsWith("Subtotal:", StringComparison.Ordinal));
        if (inicio < 0 || subtotal <= inicio)
            return string.Join("<br>", lineas.Select(E));

        var html = new StringBuilder(string.Join("<br>", lineas.Take(inicio).Select(E)));
        html.Append("<table role=\"presentation\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\" style=\"margin:14px 0 4px;border-collapse:collapse;table-layout:fixed\"><tr><td style=\"padding:10px 12px;background:#eef5fa;color:#183c5b;font-size:12px;font-weight:bold;letter-spacing:.5px\">PRODUCTOS</td></tr>");
        foreach (var linea in lineas.Skip(inicio + 1).Take(subtotal - inicio - 1).Where(l => !string.IsNullOrWhiteSpace(l)))
            html.Append("<tr><td style=\"padding:9px 12px;border-bottom:1px solid #e1ebf2;overflow-wrap:anywhere;word-wrap:break-word\">").Append(E(linea)).Append("</td></tr>");
        html.Append("</table><table role=\"presentation\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\" style=\"border-collapse:collapse;table-layout:fixed\">");
        var fin = subtotal;
        while (fin < lineas.Length && !string.IsNullOrWhiteSpace(lineas[fin]))
        {
            var linea = lineas[fin++];
            var separador = linea.IndexOf(':');
            if (separador < 0) continue;
            var total = linea.StartsWith("TOTAL:", StringComparison.Ordinal);
            html.Append("<tr><td style=\"padding:7px 12px;").Append(total ? "border-top:2px solid #bbdced;font-weight:bold;color:#102e4b" : "color:#536c80").Append("\">").Append(E(linea[..separador])).Append("</td><td align=\"right\" style=\"padding:7px 12px;white-space:nowrap;").Append(total ? "border-top:2px solid #bbdced;font-weight:bold;color:#102e4b" : "color:#32485e").Append("\">").Append(E(linea[(separador + 1)..].Trim())).Append("</td></tr>");
        }
        html.Append("</table>");
        if (fin < lineas.Length) html.Append("<br>").Append(string.Join("<br>", lineas.Skip(fin).Select(E)));
        return html.ToString();
    }

    private static string ResumenCotizacion(string contenido)
    {
        static string E(string valor) => WebUtility.HtmlEncode(valor);
        var lineas = contenido.Replace("\r\n", "\n").Split('\n');
        var inicio = Array.FindIndex(lineas, l => l.StartsWith("Institución:", StringComparison.Ordinal));
        if (inicio < 0) return string.Join("<br>", lineas.Select(E));
        var html = new StringBuilder(string.Join("<br>", lineas.Take(inicio).Select(E)));
        html.Append("<table role=\"presentation\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\" style=\"margin:16px 0;border-collapse:collapse;table-layout:fixed;background:#f5f9fc\">");
        var fin = inicio;
        while (fin < lineas.Length && !string.IsNullOrWhiteSpace(lineas[fin]))
        {
            var linea = lineas[fin++];
            var separador = linea.IndexOf(':');
            if (separador < 0) continue;
            var total = linea.StartsWith("Total:", StringComparison.Ordinal);
            html.Append("<tr><td style=\"width:42%;padding:8px 10px;border-bottom:1px solid #e1ebf2;color:#60788b;font-size:13px;vertical-align:top\">").Append(E(linea[..separador])).Append("</td><td style=\"padding:8px 10px;border-bottom:1px solid #e1ebf2;color:#173c58;overflow-wrap:anywhere;word-wrap:break-word;").Append(total ? "font-weight:bold" : "").Append("\">").Append(E(linea[(separador + 1)..].Trim())).Append("</td></tr>");
        }
        html.Append("</table>");
        if (fin < lineas.Length) html.Append(string.Join("<br>", lineas.Skip(fin).Select(E)));
        return html.ToString();
    }
}
