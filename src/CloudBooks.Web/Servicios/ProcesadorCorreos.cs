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

public class ProveedorCorreoResend(IResend cliente, IConfiguration configuracion) : IProveedorCorreo
{
    public async Task<ResultadoEntrega> EnviarAsync(CorreoPendiente correo, CancellationToken cancelacion)
    {
        var mensaje = new EmailMessage { From = configuracion["Correo:Remitente"]!, Subject = correo.Asunto, HtmlBody = correo.Html };
        mensaje.To.Add(correo.Destinatario);
        var respuesta = await cliente.EmailSendAsync($"cloudbooks/{correo.ClaveEnvio:N}", mensaje, cancelacion);
        if (!respuesta.Success) throw new InvalidOperationException("El proveedor no aceptó el mensaje.");
        return new ResultadoEntrega(false, respuesta.Content.ToString());
    }
}

public class ProcesadorCorreos(ContextoAplicacion contexto, IProveedorCorreo proveedor)
{
    public async Task<bool> ProcesarUnoAsync(CancellationToken cancelacion = default)
    {
        var ahora = DateTime.UtcNow;
        var id = await contexto.CorreosPendientes.AsNoTracking()
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
            await FinalizarAsync(id.Value, bloqueo, EstadoCorreo.Fallido, "Resultado anterior incierto fuera de la ventana segura de reintento.", null, null, correo.Intentos, cancelacion);
            return true;
        }
        if (correo.Intentos > 5)
        {
            await FinalizarAsync(id.Value, bloqueo, EstadoCorreo.Fallido, "Se agotaron los intentos de envío.", null, null, correo.Intentos, cancelacion);
            return true;
        }
        try
        {
            var resultado = await proveedor.EnviarAsync(correo, cancelacion);
            await FinalizarAsync(id.Value, bloqueo, resultado.Simulado ? EstadoCorreo.Simulado : EstadoCorreo.AceptadoProveedor, null, resultado.IdProveedor, resultado.Simulado ? "Simulado" : "Resend", correo.Intentos, cancelacion);
        }
        catch (Exception) when (!cancelacion.IsCancellationRequested)
        {
            await FinalizarAsync(id.Value, bloqueo, correo.Intentos >= 5 ? EstadoCorreo.Fallido : EstadoCorreo.ReintentoPendiente, "No fue posible confirmar la entrega al proveedor.", null, null, correo.Intentos, cancelacion);
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
