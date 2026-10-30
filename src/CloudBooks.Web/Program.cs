using CloudBooks.Web.Datos;
using CloudBooks.Web.Modelos;
using CloudBooks.Web.Servicios;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ServicioCatalogo>();
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

using (var alcance = app.Services.CreateScope())
    await DatosIniciales.InicializarAsync(alcance.ServiceProvider, app.Environment.IsDevelopment());

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Inicio/Error");
}
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Inicio}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
