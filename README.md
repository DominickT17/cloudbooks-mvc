# CloudBooks

Plataforma web editorial multicanal para lectores e instituciones. Incluye catálogo, cuentas, carrito y pedidos con pago simulado. Las solicitudes y cotizaciones institucionales se implementarán más adelante.

## Tecnologías

ASP.NET Core MVC, .NET 10, Entity Framework Core, SQL Server y ASP.NET Core Identity.

## Requisitos

SDK de .NET 10 y SQL Server. La configuración de desarrollo usa `.\SQLEXPRESS` con autenticación integrada de Windows y la base `CloudBooks`; ajusta `ConnectionStrings:DefaultConnection` para otra instancia. No incluyas contraseñas reales en archivos versionados.

## Migraciones

```powershell
dotnet tool restore
dotnet ef database update --project src/CloudBooks.Web
```

Ejecuta el comando con `ASPNETCORE_ENVIRONMENT=Development` si no se aplica el perfil de desarrollo. La migración inicial crea las tablas de Identity y catálogo. No se aplican migraciones automáticamente al iniciar.

## Ejecución

```powershell
dotnet restore
dotnet run --project src/CloudBooks.Web
```

Abre la dirección que indique la consola.

## Datos y cuentas de prueba

En Development, al arrancar por primera vez se agregan diez libros ficticios con portadas locales y cinco cuentas de demostración. El inicializador evita duplicar libros por ISBN y cuentas por correo; no modifica los roles de cuentas existentes. Los cuatro roles se preparan al arrancar. Consulta los correos y la contraseña exclusivamente académica en [credenciales de prueba](documentos/credenciales-prueba.md). No uses esas cuentas ni contraseñas en producción.

Desde **Iniciar sesión** se accede con una de esas cuentas; también puedes registrarte como cliente minorista. El enlace **Administración** y su página están protegidos para administradores.

## Catálogo

El catálogo busca en SQL Server por título, autor, ISBN o editorial y permite combinar categoría, formato y rango de precio. Cada ficha muestra sus datos y disponibilidad; los libros digitales no muestran existencias físicas. Las portadas y obras son de demostración.

## Estructura y arquitectura MVC

La solución se encuentra en `CloudBooks.sln` y la aplicación en `src/CloudBooks.Web`. Se conservan `Controllers`, `Models` y `Views` por las convenciones de ASP.NET Core; el código propio utiliza nombres en español. Los controladores reciben peticiones, los servicios consultan datos o aplican reglas y las vistas presentan el resultado.

## Carrito y pedidos

El carrito requiere una cuenta, se conserva por usuario y valida las existencias físicas. El resumen aplica el descuento por volumen a la cantidad total, después el 8 % institucional cuando corresponde y finalmente el IVA académico del 12 %. Los importes se calculan en el servidor.

La confirmación ofrece tarjeta o transferencia **simuladas** y no solicita datos financieros. El pedido conserva el título, formato, precio e importes del momento de la compra. Se crea en estado `Pendiente` sin descontar stock. Un administrador puede aprobarlo, enviarlo después de aprobar o cancelar únicamente mientras esté pendiente. El historial solo muestra pedidos propios.

La aprobación usa una transacción: cambia el estado solamente si sigue pendiente y descuenta cada libro físico con una actualización condicionada a que haya existencias suficientes. Si falta stock se revierte toda la operación; los e-books no descuentan inventario. Esto evita un segundo descuento y evita stock negativo ante aprobaciones concurrentes. Las pruebas se ejecutan con `dotnet test`.

## Integrantes

- Dóminick Ricardo Cifuentes Tomás
- Yeisson Alexander Poroj Toc
