using CloudBooks.Web.Modelos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CloudBooks.Web.Datos;

public static class DatosIniciales
{
    public static async Task InicializarAsync(IServiceProvider servicios, bool desarrollo)
    {
        var roles = servicios.GetRequiredService<RoleManager<IdentityRole>>();
        var usuarios = servicios.GetRequiredService<UserManager<UsuarioAplicacion>>();
        var contexto = servicios.GetRequiredService<ContextoAplicacion>();
        foreach (var nombre in new[] { "ClienteMinorista", "ClienteInstitucional", "AsesorVentas", "Administrador" })
            if (!await roles.RoleExistsAsync(nombre)) Comprobar(await roles.CreateAsync(new IdentityRole(nombre)));

        if (!desarrollo) return;

        foreach (var (correo, nombre, rol) in new[] {
            ("admin.dominick@cloudbooks.local", "Dóminick Ricardo Cifuentes Tomás", "Administrador"),
            ("admin.yeisson@cloudbooks.local", "Yeisson Alexander Poroj Toc", "Administrador"),
            ("asesor@cloudbooks.local", "Andrea Morales", "AsesorVentas"),
            ("cliente@cloudbooks.local", "María López", "ClienteMinorista"),
            ("institucion@cloudbooks.local", "Carlos Méndez", "ClienteInstitucional") })
        {
            var usuario = await usuarios.FindByEmailAsync(correo);
            if (usuario == null)
            {
                usuario = new UsuarioAplicacion { UserName = correo, Email = correo, NombreCompleto = nombre, EmailConfirmed = true };
                Comprobar(await usuarios.CreateAsync(usuario, "CloudBooksDemo2026!"));
                Comprobar(await usuarios.AddToRoleAsync(usuario, rol));
            }
        }

        var datos = new[] {
            ("9789929000001", "Arquitectura para sistemas humanos", "Laura Méndez", "Tecnología", "Nube Editorial", 2024, 185m, 12, FormatoLibro.TapaBlanda, "Una mirada práctica al diseño de sistemas que responden a las necesidades de las personas."),
            ("9789929000002", "Los caminos del altiplano", "Ana Lucía Pérez", "Narrativa", "Casa del Quetzal", 2023, 140m, 3, FormatoLibro.TapaDura, "Una familia descubre sus raíces a través de los pueblos y paisajes del altiplano."),
            ("9789929000003", "Ideas que construyen futuro", "Mateo Rivera", "Educación", "Nube Editorial", 2025, 110m, 20, FormatoLibro.TapaBlanda, "Herramientas para aprender, compartir conocimiento y emprender proyectos en comunidad."),
            ("9789929000004", "El jardín de las palabras", "Elena Solís", "Narrativa", "Luna de Papel", 2022, 95m, 0, FormatoLibro.Ebook, "Relatos breves sobre la memoria, las amistades y las pequeñas cosas cotidianas."),
            ("9789929000005", "Diseño digital con propósito", "Laura Méndez", "Tecnología", "Nube Editorial", 2025, 225m, 7, FormatoLibro.TapaDura, "Principios de diseño para crear productos digitales claros, útiles y accesibles."),
            ("9789929000006", "La ciudad entre montañas", "Ana Lucía Pérez", "Historia", "Casa del Quetzal", 2021, 160m, 0, FormatoLibro.TapaBlanda, "Una exploración de la historia y la vida cotidiana de una ciudad rodeada de montañas."),
            ("9789929000007", "Aprender en comunidad", "Mateo Rivera", "Educación", "Luna de Papel", 2024, 89m, 0, FormatoLibro.Ebook, "Estrategias sencillas para construir espacios de aprendizaje participativos."),
            ("9789929000008", "Mapas de la memoria", "Elena Solís", "Historia", "Casa del Quetzal", 2023, 175m, 5, FormatoLibro.TapaDura, "Historias y fotografías imaginadas que recorren la memoria de varias generaciones."),
            ("9789929000009", "La biblioteca del viento", "Sofía Barrios", "Narrativa", "Luna de Papel", 2025, 125m, 18, FormatoLibro.TapaBlanda, "Una joven bibliotecaria encuentra cartas que transforman su comunidad."),
            ("9789929000010", "Código y pensamiento", "Diego Castillo", "Tecnología", "Nube Editorial", 2024, 199m, 9, FormatoLibro.TapaBlanda, "Una introducción a la programación y la resolución de problemas paso a paso.") };

        foreach (var (isbn, titulo, autorNombre, categoriaNombre, editorialNombre, anio, precio, stock, formato, sinopsis) in datos)
        {
            if (await contexto.Libros.AnyAsync(x => x.ISBN == isbn)) continue;
            var autor = await contexto.Autores.FirstOrDefaultAsync(x => x.Nombre == autorNombre) ?? new Autor { Nombre = autorNombre };
            var categoria = await contexto.Categorias.FirstOrDefaultAsync(x => x.Nombre == categoriaNombre) ?? new Categoria { Nombre = categoriaNombre };
            var editorial = await contexto.Editoriales.FirstOrDefaultAsync(x => x.Nombre == editorialNombre) ?? new Editorial { Nombre = editorialNombre };
            contexto.Libros.Add(new Libro { ISBN = isbn, Titulo = titulo, Sinopsis = sinopsis, AnioEdicion = anio, PrecioBase = precio, Stock = stock, Formato = formato, Activo = true, ImagenPortada = "/imagenes/libros/" + isbn + ".svg", Autores = [autor], Categoria = categoria, Editorial = editorial });
            await contexto.SaveChangesAsync();
        }
    }

    private static void Comprobar(IdentityResult resultado)
    {
        if (!resultado.Succeeded) throw new InvalidOperationException(string.Join("; ", resultado.Errors.Select(e => e.Description)));
    }
}
