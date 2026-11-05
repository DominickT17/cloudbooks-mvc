using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using CloudBooks.Web.Servicios;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Resend;
using Xunit;
using Xunit.Abstractions;

namespace CloudBooks.Pruebas;

public class ResendRealFactAttribute : FactAttribute
{
    public ResendRealFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("CLOUDBOOKS_QA_RESEND_REAL") != "1") Skip = "Envío real desactivado; requiere autorización y base temporal.";
    }
}

public class ResendRealPruebas(ITestOutputHelper salida)
{
    [ResendRealFact]
    public async Task EnviarUnCorreoAutorizadoMedianteCola()
    {
        var cadena = Environment.GetEnvironmentVariable("CLOUDBOOKS_QA_SQL") ?? throw new InvalidOperationException("Falta la base temporal de QA.");
        var cadenaSql = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(cadena);
        if (!cadenaSql.InitialCatalog.StartsWith("CloudBooks_QA_Resend_", StringComparison.Ordinal)) throw new InvalidOperationException("Se exige una base temporal de QA dedicada.");
        var config = new ConfigurationBuilder().AddUserSecrets(typeof(ContextoAplicacion).Assembly, optional: false)
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Correo:Proveedor"] = "Resend", ["Correo:Remitente"] = "CloudBooks <onboarding@resend.dev>" }).Build();
        Assert.True(ConfiguracionCorreo.UsarResend(config), "No se encontró la configuración segura de Resend.");
        await using var db = new ContextoAplicacion(new DbContextOptionsBuilder<ContextoAplicacion>().UseSqlServer(cadena).Options);
        Assert.Empty(await db.CorreosPendientes.ToListAsync());
        var correo = new CorreoPendiente { ClaveEvento = "prueba-real/" + Guid.NewGuid().ToString("N"), Tipo = "prueba_real", Destinatario = "dominickt1703@gmail.com", Asunto = "CloudBooks - Prueba de correo", Html = "<p>Esta es una prueba de correo transaccional de CloudBooks.</p>", CreadoUtc = DateTime.UtcNow, Estado = EstadoCorreo.Pendiente };
        db.CorreosPendientes.Add(correo);
        await db.SaveChangesAsync();
        var cliente = ResendClient.Create(new ResendClientOptions { ApiToken = ConfiguracionCorreo.ObtenerClave(config)! }, new HttpClient { Timeout = TimeSpan.FromSeconds(30) });
        Assert.True(await new ProcesadorCorreos(db, new ProveedorCorreoResend(cliente, config)).ProcesarUnoAsync());
        var resultado = await db.CorreosPendientes.AsNoTracking().SingleAsync();
        Assert.True(resultado.Estado == EstadoCorreo.AceptadoProveedor, $"Resend no confirmó la aceptación; estado {resultado.Estado}. {resultado.Error}");
        Assert.False(string.IsNullOrWhiteSpace(resultado.IdProveedor));
        salida.WriteLine($"Resend aceptó el mensaje. Identificador: {resultado.IdProveedor}");
    }
}
