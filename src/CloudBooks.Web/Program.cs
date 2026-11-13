using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using CloudBooks.Web.Servicios;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Resend;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Data.SqlClient;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication().AddCookie("Citein", opciones =>
{
    opciones.Cookie.Name = ".CloudBooks.Citein";
    opciones.Cookie.HttpOnly = true;
    opciones.Cookie.SameSite = SameSiteMode.Lax;
    opciones.ExpireTimeSpan = TimeSpan.FromHours(4);
    opciones.SlidingExpiration = false;
    opciones.LoginPath = "/citein";
});
builder.Services.AddRateLimiter(opciones => opciones.AddPolicy("citein", contexto =>
    RateLimitPartition.GetFixedWindowLimiter(contexto.Connection.RemoteIpAddress?.ToString() ?? "local", _ =>
        new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true })));
builder.Services.AddRateLimiter(opciones => opciones.AddPolicy("citein-inicio", contexto =>
    RateLimitPartition.GetFixedWindowLimiter(contexto.Connection.RemoteIpAddress?.ToString() ?? "local", _ =>
        new FixedWindowRateLimiterOptions { PermitLimit = 12, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true })));
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opciones => { opciones.IdleTimeout = TimeSpan.FromDays(7); opciones.Cookie.HttpOnly = true; opciones.Cookie.IsEssential = true; });
builder.Services.AddScoped<ServicioCarritoInvitado>();
builder.Services.AddScoped<ServicioCatalogo>();
builder.Services.AddScoped<ServicioEscaparate>();
builder.Services.AddScoped<ServicioAdministracionCatalogo>();
builder.Services.AddScoped<ServicioCatalogos>();
builder.Services.AddScoped<ServicioInventario>();
builder.Services.AddScoped<ServicioMetricas>();
builder.Services.AddScoped<ServicioPendientesAdministrativos>();
builder.Services.AddScoped<ServicioUsuarios>();
builder.Services.AddScoped<ServicioParametros>();
builder.Services.AddScoped<ServicioCarrito>();
builder.Services.AddScoped<ServicioDescuentos>();
builder.Services.AddScoped<ServicioPedidos>();
builder.Services.AddScoped<ServicioIntentosPago>();
builder.Services.AddScoped<IServicioNotificaciones, ServicioNotificaciones>();
builder.Services.AddScoped<ProcesadorCorreos>();
builder.Services.AddHttpClient<ResendClient>(cliente => cliente.Timeout = TimeSpan.FromSeconds(60));
builder.Services.Configure<ResendClientOptions>(opciones => opciones.ApiToken = ConfiguracionCorreo.ObtenerClave(builder.Configuration) ?? "");
builder.Services.AddTransient<IResend, ResendClient>();
builder.Services.AddScoped<ProveedorCorreoResend>();
builder.Services.AddScoped<ProveedorCorreoSimulado>();
builder.Services.AddScoped<ProveedorCorreoCitein>();
builder.Services.AddScoped<IProveedorCorreo>(servicios =>
    builder.Configuration.GetValue<bool>("CiteinDemo:Enabled") && ConfiguracionCorreo.UsarResend(builder.Configuration)
        ? servicios.GetRequiredService<ProveedorCorreoCitein>()
        : ConfiguracionCorreo.UsarResend(builder.Configuration) ? servicios.GetRequiredService<ProveedorCorreoResend>() : servicios.GetRequiredService<ProveedorCorreoSimulado>());
builder.Services.AddHostedService<ServicioFondoCorreos>();
builder.Services.AddScoped<INotificacionesPedido, NotificacionesPedido>();
builder.Services.AddScoped<ServicioAvisosAdministracion>();
builder.Services.AddScoped<ServicioInstituciones>();
builder.Services.AddScoped<ServicioAsignacionAsesores>();
builder.Services.AddScoped<ServicioSolicitudesCotizacion>();
builder.Services.AddScoped<ServicioCotizaciones>();
builder.Services.AddScoped<ServicioPdfCotizaciones>();
builder.Services.AddScoped<ServicioComprobantePedido>();
builder.Services.AddDbContext<ContextoAplicacion>(opciones =>
    opciones.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddIdentity<UsuarioAplicacion, IdentityRole>()
    .AddEntityFrameworkStores<ContextoAplicacion>()
    .AddDefaultTokenProviders();
builder.Services.Configure<IdentityOptions>(opciones => opciones.User.RequireUniqueEmail = true);
builder.Services.ConfigureApplicationCookie(opciones =>
{
    opciones.LoginPath = "/Cuenta/Ingresar";
    opciones.AccessDeniedPath = "/Cuenta/AccesoDenegado";
});

var app = builder.Build();
var citeinHabilitado = builder.Configuration.GetValue<bool>("CiteinDemo:Enabled");
if (citeinHabilitado)
{
    var cadena = new SqlConnectionStringBuilder(builder.Configuration.GetConnectionString("DefaultConnection"));
    var servidor = cadena.DataSource.ToLowerInvariant();
    if (!app.Environment.IsDevelopment() || cadena.InitialCatalog != "CloudBooks_CITEIN_2026" ||
        !(servidor.StartsWith(@".\") || servidor.StartsWith(@"localhost\") || servidor == "." || servidor == "localhost"))
        throw new InvalidOperationException("CITEIN requiere Development, SQL Server local y base CloudBooks_CITEIN_2026.");
    ConfiguracionCorreo.ValidarCitein(builder.Configuration);
}

using (var alcance = app.Services.CreateScope())
    await DatosIniciales.InicializarAsync(alcance.ServiceProvider, app.Environment.IsDevelopment());

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment() || citeinHabilitado)
{
    app.UseExceptionHandler("/Inicio/Error");
}
app.UseStatusCodePagesWithReExecute("/Inicio/Estado", "?codigo={0}");
app.Use(async (contexto, siguiente) =>
{
    if (!citeinHabilitado && contexto.Request.Path.StartsWithSegments("/citein")) { contexto.Response.StatusCode = 404; return; }
    await siguiente();
});
app.UseRouting();
app.UseRateLimiter();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Inicio}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

public partial class Program { }
