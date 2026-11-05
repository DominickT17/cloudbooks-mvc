using System.Globalization;

namespace CloudBooks.Web.ModelosVista;

public static class FechaPresentacion
{
    private static readonly TimeZoneInfo Guatemala = TimeZoneInfo.FindSystemTimeZoneById("America/Guatemala");
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-GT");

    public static DateTime EnGuatemala(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Guatemala);
    public static string Mostrar(DateTime utc) => EnGuatemala(utc).ToString("dd MMM yyyy · HH:mm", Cultura);
}
