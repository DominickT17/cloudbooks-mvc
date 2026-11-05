using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;

namespace CloudBooks.Web.Servicios;

public class ServicioIntentosPago(ContextoAplicacion contexto)
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
        contexto.IntentosPago.Add(new IntentoPago { UsuarioId = usuarioId, FechaUtc = DateTime.UtcNow, Resultado = motivo });
        await contexto.SaveChangesAsync();
    }
}
