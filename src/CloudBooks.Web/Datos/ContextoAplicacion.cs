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

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        base.OnModelCreating(modelo);
        modelo.Entity<Libro>().Property(libro => libro.PrecioBase).HasPrecision(18, 2);
        modelo.Entity<Libro>().HasIndex(libro => libro.ISBN).IsUnique();
        modelo.Entity<Libro>().HasOne(libro => libro.Categoria).WithMany().OnDelete(DeleteBehavior.Restrict);
        modelo.Entity<Libro>().HasOne(libro => libro.Editorial).WithMany().OnDelete(DeleteBehavior.Restrict);
    }
}
