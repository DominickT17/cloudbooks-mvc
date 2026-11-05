namespace CloudBooks.Web.Servicios;

public static class ContactoWhatsapp
{
    public static string? NormalizarGuatemala(string? telefono)
    {
        if (string.IsNullOrWhiteSpace(telefono)) return null;
        var limpio = telefono.Trim().Replace(" ", "").Replace("-", "");
        if (limpio.StartsWith("+502", StringComparison.Ordinal)) limpio = limpio[4..];
        else if (limpio.StartsWith("502", StringComparison.Ordinal) && limpio.Length == 11) limpio = limpio[3..];
        return limpio.Length == 8 && limpio.All(c => c is >= '0' and <= '9') ? "+502" + limpio : null;
    }

    public static string? CrearEnlace(string? telefono, string mensaje)
    {
        var numero = NormalizarGuatemala(telefono);
        return numero == null ? null : $"https://wa.me/{numero[1..]}?text={Uri.EscapeDataString(mensaje)}";
    }
}
