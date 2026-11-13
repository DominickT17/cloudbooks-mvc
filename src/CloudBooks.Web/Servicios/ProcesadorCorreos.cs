using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using Microsoft.EntityFrameworkCore;
using Resend;

namespace CloudBooks.Web.Servicios;

public record ResultadoEntrega(bool Simulado, string? IdProveedor);
public interface IProveedorCorreo
{
    Task<ResultadoEntrega> EnviarAsync(CorreoPendiente correo, CancellationToken cancelacion);
}

public class ProveedorCorreoSimulado : IProveedorCorreo
{
    public Task<ResultadoEntrega> EnviarAsync(CorreoPendiente correo, CancellationToken cancelacion) => Task.FromResult(new ResultadoEntrega(true, null));
}

public class ProveedorCorreoResend(IResend cliente, IConfiguration configuracion, ContextoAplicacion? contexto = null) : IProveedorCorreo
{
    public async Task<ResultadoEntrega> EnviarAsync(CorreoPendiente correo, CancellationToken cancelacion)
    {
        if (!System.Net.Mail.MailAddress.TryCreate(correo.Destinatario, out var destino) ||
            destino.Host.EndsWith(".invalid", StringComparison.OrdinalIgnoreCase) || destino.Host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
            return new ResultadoEntrega(true, null);
        var texto = correo.TextoPlano ?? PlantillasCorreo.TextoDeHtmlAnterior(correo.Asunto, correo.Html);
        var mensaje = new EmailMessage { From = configuracion["Correo:Remitente"]!, Subject = correo.Asunto, HtmlBody = correo.Html, TextBody = texto };
        mensaje.To.Add(correo.Destinatario);
        if (correo.LogoCorreo is { Length: > 0 } logo && correo.Html.Contains("cid:cloudbooks-logo", StringComparison.Ordinal))
            mensaje.Attachments = [new EmailAttachment { Filename = "cloudbooks.png", Content = logo, ContentType = "image/png", ContentId = "cloudbooks-logo" }];
        if (contexto != null && correo.ComprobantePdf is { Length: >= 5 } pdf && pdf.AsSpan(0, 5).SequenceEqual("%PDF-"u8) &&
            correo.Tipo == "aprobado" && ConfiguracionCorreo.EsEventoCliente(correo.ClaveEvento, correo.Tipo, out var pedidoId) &&
            await contexto.Pedidos.AsNoTracking().AnyAsync(p => p.Id == pedidoId &&
                (p.Estado == EstadoPedido.Aprobado || p.Estado == EstadoPedido.Enviado) && p.Usuario.Email == correo.Destinatario, cancelacion))
        {
            mensaje.Attachments ??= [];
            mensaje.Attachments.Add(new EmailAttachment { Filename = $"comprobante-PED-{pedidoId:000000}.pdf", Content = pdf, ContentType = "application/pdf" });
        }
        var respuesta = await cliente.EmailSendAsync($"cloudbooks/{correo.ClaveEnvio:N}", mensaje, cancelacion);
        if (!respuesta.Success) throw new InvalidOperationException("El proveedor no aceptó el mensaje.");
        return new ResultadoEntrega(false, respuesta.Content.ToString());
    }
}

// CITEIN solo envía eventos nuevos marcados al crearse para el destinatario autorizado.
public class ProveedorCorreoCitein(ContextoAplicacion contexto, IConfiguration configuracion, ProveedorCorreoResend resend, ProveedorCorreoSimulado simulado) : IProveedorCorreo
{
    public async Task<ResultadoEntrega> EnviarAsync(CorreoPendiente correo, CancellationToken cancelacion)
    {
        if (correo.Proveedor != ConfiguracionCorreo.MarcaRealCitein(configuracion) ||
            !ConfiguracionCorreo.AutorizadoCitein(configuracion, correo.ClaveEvento, correo.Tipo, correo.Destinatario) ||
            !ConfiguracionCorreo.EsEventoCliente(correo.ClaveEvento, correo.Tipo, out var pedidoId) ||
            !await contexto.Pedidos.AsNoTracking().AnyAsync(p => p.Id == pedidoId && p.Usuario.Email == correo.Destinatario, cancelacion))
            return await simulado.EnviarAsync(correo, cancelacion);
        return await resend.EnviarAsync(correo, cancelacion);
    }
}

public class ProcesadorCorreos(ContextoAplicacion contexto, IProveedorCorreo proveedor, IConfiguration? configuracion = null)
{
    public async Task<bool> ProcesarUnoAsync(CancellationToken cancelacion = default)
    {
        var ahora = DateTime.UtcNow;
        var cola = contexto.CorreosPendientes.AsNoTracking().AsQueryable();
        if (configuracion != null && configuracion.GetValue<bool>("CiteinDemo:Enabled") && ConfiguracionCorreo.UsarResend(configuracion))
        {
            var marcaSesion = ConfiguracionCorreo.MarcaRealCitein(configuracion);
            cola = cola.Where(c => c.Proveedor == marcaSesion || c.Proveedor == ConfiguracionCorreo.MarcaSimuladoCitein);
        }
        var id = await cola
            .Where(c => ((c.Estado == EstadoCorreo.Pendiente || c.Estado == EstadoCorreo.ReintentoPendiente) && (c.ProximoIntentoUtc == null || c.ProximoIntentoUtc <= ahora)) || (c.Estado == EstadoCorreo.EnProceso && c.BloqueadoHastaUtc < ahora))
            .OrderBy(c => c.Id).Select(c => (int?)c.Id).FirstOrDefaultAsync(cancelacion);
        if (id == null) return false;
        var bloqueo = Guid.NewGuid();
        var tomado = await contexto.CorreosPendientes.Where(c => c.Id == id && (((c.Estado == EstadoCorreo.Pendiente || c.Estado == EstadoCorreo.ReintentoPendiente) && (c.ProximoIntentoUtc == null || c.ProximoIntentoUtc <= ahora)) || (c.Estado == EstadoCorreo.EnProceso && c.BloqueadoHastaUtc < ahora)))
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Estado, EstadoCorreo.EnProceso).SetProperty(c => c.Bloqueo, bloqueo).SetProperty(c => c.BloqueadoHastaUtc, ahora.AddMinutes(5)).SetProperty(c => c.UltimoIntentoUtc, ahora).SetProperty(c => c.PrimerIntentoUtc, c => c.PrimerIntentoUtc ?? ahora).SetProperty(c => c.Intentos, c => c.Intentos + 1), cancelacion);
        if (tomado == 0) return true;
        var correo = await contexto.CorreosPendientes.AsNoTracking().SingleAsync(c => c.Id == id, cancelacion);
        if (correo.PrimerIntentoUtc < ahora.AddHours(-23))
        {
            await FinalizarAsync(id.Value, bloqueo, EstadoCorreo.Fallido, "Resultado anterior incierto fuera de la ventana segura de reintento.", null, correo.Proveedor, correo.Intentos, cancelacion);
            return true;
        }
        if (correo.Intentos > 5)
        {
            await FinalizarAsync(id.Value, bloqueo, EstadoCorreo.Fallido, "Se agotaron los intentos de envío.", null, correo.Proveedor, correo.Intentos, cancelacion);
            return true;
        }
        try
        {
            var resultado = await proveedor.EnviarAsync(correo, cancelacion);
            await FinalizarAsync(id.Value, bloqueo, resultado.Simulado ? EstadoCorreo.Simulado : EstadoCorreo.AceptadoProveedor, null, resultado.IdProveedor, resultado.Simulado ? "Simulado" : "Resend", correo.Intentos, cancelacion);
        }
        catch (Exception) when (!cancelacion.IsCancellationRequested)
        {
            await FinalizarAsync(id.Value, bloqueo, correo.Intentos >= 5 ? EstadoCorreo.Fallido : EstadoCorreo.ReintentoPendiente, "No fue posible confirmar la aceptación por el proveedor.", null, correo.Proveedor, correo.Intentos, cancelacion);
        }
        return true;
    }

    private Task<int> FinalizarAsync(int id, Guid bloqueo, EstadoCorreo estado, string? error, string? idProveedor, string? proveedor, int intentos, CancellationToken cancelacion)
    {
        DateTime? proximo = estado == EstadoCorreo.ReintentoPendiente ? DateTime.UtcNow.AddMinutes(Math.Pow(2, Math.Min(intentos, 5))) : null;
        return
        contexto.CorreosPendientes.Where(c => c.Id == id && c.Estado == EstadoCorreo.EnProceso && c.Bloqueo == bloqueo)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Estado, estado).SetProperty(c => c.Error, error).SetProperty(c => c.IdProveedor, idProveedor).SetProperty(c => c.Proveedor, proveedor).SetProperty(c => c.Bloqueo, (Guid?)null).SetProperty(c => c.BloqueadoHastaUtc, (DateTime?)null).SetProperty(c => c.ProximoIntentoUtc, proximo), cancelacion);
    }
}

public class ServicioFondoCorreos(IServiceScopeFactory fabrica, ILogger<ServicioFondoCorreos> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var alcance = fabrica.CreateScope();
                var procesador = alcance.ServiceProvider.GetRequiredService<ProcesadorCorreos>();
                if (await procesador.ProcesarUnoAsync(stoppingToken)) continue;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception error) { logger.LogWarning("No se pudo procesar la cola de correos: {Tipo}", error.GetType().Name); }
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }
}
