using System.Globalization;

namespace CloudBooks.Web.Servicios;

public enum ResultadoTarjetaSimulada { Aprobado, FondosInsuficientes, Rechazado, Bloqueado, Vencido, DatosInvalidos }
public record RespuestaTarjetaSimulada(ResultadoTarjetaSimulada Resultado, string? UltimosCuatro)
{
    public bool Autorizada => Resultado == ResultadoTarjetaSimulada.Aprobado;
}

// Solo reconoce identificadores ficticios de demostración; no conserva los datos recibidos.
public class SimuladorTarjeta
{
    public RespuestaTarjetaSimulada Evaluar(string? numero, string? vencimiento, string? cvv, DateOnly fechaActual)
    {
        var digitos = (numero ?? "").Replace(" ", "");
        if (digitos.Length != 16 || digitos.Any(c => c is < '0' or > '9') ||
            cvv == null || cvv.Length is < 3 or > 4 || cvv.Any(c => c is < '0' or > '9') ||
            vencimiento == null || vencimiento.Length != 5 || vencimiento[2] != '/' ||
            !int.TryParse(vencimiento[..2], NumberStyles.None, CultureInfo.InvariantCulture, out var mes) ||
            !int.TryParse(vencimiento[3..], NumberStyles.None, CultureInfo.InvariantCulture, out var anio) || mes is < 1 or > 12)
            return new(ResultadoTarjetaSimulada.DatosInvalidos, null);
        var ultimos = digitos[^4..];
        if (new DateOnly(2000 + anio, mes, 1).AddMonths(1) <= fechaActual)
            return new(ResultadoTarjetaSimulada.Vencido, ultimos);
        var resultado = digitos switch
        {
            "4000000000000002" => ResultadoTarjetaSimulada.Aprobado,
            "4000000000000010" => ResultadoTarjetaSimulada.FondosInsuficientes,
            "4000000000000028" => ResultadoTarjetaSimulada.Rechazado,
            "4000000000000036" => ResultadoTarjetaSimulada.Bloqueado,
            _ => ResultadoTarjetaSimulada.DatosInvalidos
        };
        return new(resultado, resultado == ResultadoTarjetaSimulada.DatosInvalidos ? null : ultimos);
    }
}
