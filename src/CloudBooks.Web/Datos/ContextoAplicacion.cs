using CloudBooks.Web.Modelos;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CloudBooks.Web.Datos;

public class ContextoAplicacion(DbContextOptions<ContextoAplicacion> opciones) : IdentityDbContext<UsuarioAplicacion>(opciones)
{
    public DbSet<Libro> Libros => Set<Libro>();
    public DbSet<Autor> Autores => Set<Autor>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Editorial> Editoriales => Set<Editorial>();
    public DbSet<Carrito> Carritos => Set<Carrito>();
    public DbSet<DetalleCarrito> DetallesCarrito => Set<DetalleCarrito>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<DetallePedido> DetallesPedido => Set<DetallePedido>();
    public DbSet<SolicitudInstitucional> SolicitudesInstitucionales => Set<SolicitudInstitucional>();
    public DbSet<PerfilInstitucional> PerfilesInstitucionales => Set<PerfilInstitucional>();
    public DbSet<SolicitudCotizacion> SolicitudesCotizacion => Set<SolicitudCotizacion>();
    public DbSet<Cotizacion> Cotizaciones => Set<Cotizacion>();
    public DbSet<DetalleCotizacion> DetallesCotizacion => Set<DetalleCotizacion>();
    public DbSet<ParametrosSistema> ConfiguracionSistema => Set<ParametrosSistema>();
    public DbSet<NotificacionPedido> NotificacionesPedido => Set<NotificacionPedido>();

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        base.OnModelCreating(modelo);
        modelo.Entity<UsuarioAplicacion>().Property(u => u.Activo).HasDefaultValue(true);
        modelo.Entity<ParametrosSistema>().Property(p => p.TasaImpuesto).HasPrecision(5, 4);
        modelo.Entity<ParametrosSistema>().Property(p => p.MontoRevision).HasPrecision(18, 2);
        modelo.Entity<ParametrosSistema>().HasData(new ParametrosSistema { Id = 1, TasaImpuesto = ParametrosSistema.ImpuestoInicial, UmbralStockBajo = ParametrosSistema.UmbralInicial, DiasVigencia = ParametrosSistema.VigenciaInicial });
        modelo.Entity<Libro>().Property(libro => libro.PrecioBase).HasPrecision(18, 2);
        modelo.Entity<Libro>().Property(libro => libro.Stock).IsConcurrencyToken();
        modelo.Entity<Libro>().HasIndex(libro => libro.ISBN).IsUnique();
        modelo.Entity<Libro>().HasOne(libro => libro.Categoria).WithMany().OnDelete(DeleteBehavior.Restrict);
        modelo.Entity<Libro>().HasOne(libro => libro.Editorial).WithMany().OnDelete(DeleteBehavior.Restrict);
        modelo.Entity<Carrito>().HasIndex(carrito => carrito.UsuarioId).IsUnique();
        modelo.Entity<DetalleCarrito>().HasIndex(detalle => new { detalle.CarritoId, detalle.LibroId }).IsUnique();
        modelo.Entity<DetalleCarrito>().HasOne(detalle => detalle.Libro).WithMany().OnDelete(DeleteBehavior.Restrict);
        modelo.Entity<DetalleCarrito>().ToTable(t => t.HasCheckConstraint("CK_DetalleCarrito_Cantidad", "[Cantidad] >= 1"));
        modelo.Entity<Pedido>().HasOne(p => p.Usuario).WithMany().OnDelete(DeleteBehavior.Restrict);
        modelo.Entity<DetallePedido>().HasOne(d => d.Libro).WithMany().OnDelete(DeleteBehavior.Restrict);
        modelo.Entity<DetallePedido>().ToTable(t => t.HasCheckConstraint("CK_DetallePedido_Cantidad", "[Cantidad] >= 1"));
        foreach (var propiedad in new[] { nameof(Pedido.Subtotal), nameof(Pedido.DescuentoVolumen), nameof(Pedido.DescuentoInstitucional), nameof(Pedido.Impuesto), nameof(Pedido.Total) })
            modelo.Entity<Pedido>().Property<decimal>(propiedad).HasPrecision(18, 2);
        modelo.Entity<DetallePedido>().Property(d => d.PrecioUnitario).HasPrecision(18, 2);
        modelo.Entity<DetallePedido>().Property(d => d.Subtotal).HasPrecision(18, 2);
        modelo.Entity<Pedido>().Property(p => p.TasaImpuesto).HasPrecision(5, 4);
        modelo.Entity<SolicitudInstitucional>().HasIndex(s => s.UsuarioId).IsUnique().HasFilter("[Estado] = 0");
        modelo.Entity<SolicitudInstitucional>().HasOne(s => s.Usuario).WithMany().OnDelete(DeleteBehavior.Restrict);
        modelo.Entity<PerfilInstitucional>().HasIndex(p => p.UsuarioId).IsUnique();
        modelo.Entity<PerfilInstitucional>().HasOne(p => p.Usuario).WithMany().OnDelete(DeleteBehavior.Restrict);
        modelo.Entity<SolicitudCotizacion>().HasOne(s => s.Usuario).WithMany().OnDelete(DeleteBehavior.Restrict);
        modelo.Entity<SolicitudCotizacion>().HasOne(s => s.Asesor).WithMany().OnDelete(DeleteBehavior.Restrict);
        modelo.Entity<SolicitudCotizacion>().HasOne(s => s.PerfilInstitucional).WithMany().OnDelete(DeleteBehavior.Restrict);
        modelo.Entity<SolicitudCotizacion>().ToTable(t => t.HasCheckConstraint("CK_SolicitudCotizacion_Cantidad", "[Cantidad] > 0"));
        modelo.Entity<Cotizacion>().HasIndex(c => c.SolicitudCotizacionId).IsUnique();
        modelo.Entity<Cotizacion>().HasOne(c => c.Solicitud).WithMany().HasForeignKey(c => c.SolicitudCotizacionId).OnDelete(DeleteBehavior.Restrict);
        foreach (var propiedad in new[] { nameof(Cotizacion.Subtotal), nameof(Cotizacion.DescuentoVolumen), nameof(Cotizacion.DescuentoInstitucional), nameof(Cotizacion.Impuesto), nameof(Cotizacion.Total) })
            modelo.Entity<Cotizacion>().Property<decimal>(propiedad).HasPrecision(18, 2);
        modelo.Entity<DetalleCotizacion>().Property(d => d.PrecioUnitario).HasPrecision(18, 2);
        modelo.Entity<DetalleCotizacion>().Property(d => d.Subtotal).HasPrecision(18, 2);
        modelo.Entity<Cotizacion>().Property(c => c.TasaImpuesto).HasPrecision(5, 4);
        modelo.Entity<DetalleCotizacion>().ToTable(t => t.HasCheckConstraint("CK_DetalleCotizacion_Cantidad", "[Cantidad] > 0"));
    }
}
