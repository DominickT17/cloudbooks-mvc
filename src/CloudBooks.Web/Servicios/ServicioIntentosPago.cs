using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using Microsoft.EntityFrameworkCore;

namespace CloudBooks.Web.Servicios;

public class ServicioIntentosPago(ContextoAplicacion contexto, IServicioNotificaciones? correos = null)
{
    public async Task RegistrarRechazoAsync(string usuarioId, ResultadoTarjetaSimulada resultado)
    {
        var motivo = resultado switch
        {
            ResultadoTarjetaSimulada.FondosInsuficientes => ResultadoPago.FondosInsuficientes,
            ResultadoTarjetaSimulada.Rechazado => ResultadoPago.Rechazado,
            ResultadoTarjetaSimulada.Bloqueado => ResultadoPago.Bloqueado,
            ResultadoTarjetaSimulada.Vencido => ResultadoPago.Vencido,
            ResultadoTarjetaSimulada.DatosInvalidos => ResultadoPago.DatosInvalidos,
            _ => throw new ArgumentException("Solo se registran intentos no autorizados.", nameof(resultado))
        };
        await using var transaccion = await contexto.Database.BeginTransactionAsync();
        var intento = new IntentoPago { UsuarioId = usuarioId, FechaUtc = DateTime.UtcNow, Resultado = motivo };
        contexto.IntentosPago.Add(intento);
        await contexto.SaveChangesAsync();
        if (correos != null)
        {
            var usuario = await contexto.Users.AsNoTracking().SingleAsync(u => u.Id == usuarioId);
            var explicacion = motivo switch { ResultadoPago.FondosInsuficientes => "Fondos insuficientes", ResultadoPago.Rechazado => "Tarjeta rechazada", ResultadoPago.Bloqueado => "Tarjeta bloqueada", ResultadoPago.Vencido => "Tarjeta vencida", _ => "Datos de prueba inválidos" };
            await correos.EncolarAsync($"intento/{intento.Id}/rechazado", "pago_rechazado", $"Intento {intento.Id}", usuario.Email, "CloudBooks | No pudimos procesar tu pago", $"Hola, {usuario.NombreCompleto}:\n\nEl pago simulado no fue procesado: {explicacion}. Tu carrito sigue disponible. No hubo ningún cargo bancario real.");
            await contexto.SaveChangesAsync();
        }
        await transaccion.CommitAsync();
    }
}
