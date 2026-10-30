# CloudBooks

Plataforma web editorial multicanal para lectores e instituciones. El proyecto está en desarrollo; actualmente contiene la base MVC y una página de presentación. El catálogo y las operaciones comerciales todavía no están implementados.

## Tecnologías

ASP.NET Core MVC, .NET 10, Entity Framework Core, SQL Server y ASP.NET Core Identity. El contexto y las tablas de Identity están configurados; el acceso y los roles aún están pendientes.

## Requisitos

SDK de .NET 10 y SQL Server. La configuración de desarrollo usa `.\SQLEXPRESS` con autenticación integrada de Windows y la base `CloudBooks`; ajusta `ConnectionStrings:DefaultConnection` para otra instancia. No incluyas contraseñas reales en archivos versionados.

## Migraciones

```powershell
dotnet tool restore
dotnet ef database update --project src/CloudBooks.Web
```

Ejecuta el comando con `ASPNETCORE_ENVIRONMENT=Development` si no se aplica el perfil de desarrollo. La migración inicial crea las tablas de Identity y catálogo, todavía sin datos semilla.

## Ejecución

```powershell
dotnet restore
dotnet run --project src/CloudBooks.Web
```

Abre la dirección que indique la consola.

## Estructura y arquitectura MVC

La solución se encuentra en `CloudBooks.sln` y la aplicación en `src/CloudBooks.Web`. Se conservan `Controllers`, `Models` y `Views` por las convenciones de ASP.NET Core; el código propio utiliza nombres en español. El controlador recibe la petición y selecciona la vista; la lógica comercial se ubicará en servicios del modelo.

El servicio de descuentos calcula volumen, descuento institucional e impuesto en ese orden. La integración con carrito y pedidos está pendiente. Sus pruebas se ejecutan con `dotnet test`.

## Integrantes

- Dóminick Ricardo Cifuentes Tomás
- Yeisson Alexander Poroj Toc
