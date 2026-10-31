# CloudBooks

## Descripción

Plataforma editorial multicanal para lectores e instituciones. Permite explorar libros, realizar compras simuladas y gestionar pedidos, inventario y cotizaciones institucionales. Es un proyecto universitario; no procesa dinero real.

## Tecnologías

.NET 10, ASP.NET Core MVC, Razor, Entity Framework Core, SQL Server, Identity, Bootstrap, CSS propio, Chart.js y QuestPDF.

## Requisitos

SDK .NET 10, SQL Server y `sqlcmd` si se utiliza la alternativa SQL. Para desarrollo Windows se probó `.\SQLEXPRESS` con autenticación integrada. La base se llama `CloudBooks`.

## Instalación

```powershell
dotnet restore
dotnet tool restore
```

## Configuración de SQL Server

En `src/CloudBooks.Web/appsettings.Development.json`, ajusta `ConnectionStrings:DefaultConnection` a tu instancia. La configuración incluida usa autenticación integrada. No agregues contraseñas reales a archivos versionados.

## Crear base mediante migraciones

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef database update --project src/CloudBooks.Web
```

Al iniciar en Development, el inicializador agrega datos académicos faltantes sin duplicar libros por ISBN ni usuarios por correo. Los roles se preparan al arrancar. Las migraciones no se aplican automáticamente.

## Alternativa mediante CloudBooks.sql

Usa [CloudBooks.sql](base-datos/CloudBooks.sql) **solo en una base vacía y dedicada**: incluye esquema, historial de migraciones y datos de demostración con hashes de contraseñas académicas. No lo ejecutes sobre una base existente ni lo vuelvas a ejecutar sobre la misma base.

```powershell
sqlcmd -S '.\SQLEXPRESS' -E -b -Q "CREATE DATABASE [CloudBooks];"
sqlcmd -S '.\SQLEXPRESS' -d CloudBooks -E -b -I -f 65001 -i base-datos/CloudBooks.sql
```

El primer comando requiere que `CloudBooks` no exista. Si usas otra base o instancia, ajusta ambos comandos y la cadena de conexión. SQL Server puede advertir sobre el tamaño máximo teórico de índices de Identity.

## Ejecución

```powershell
dotnet run --project src/CloudBooks.Web
```

Abre la dirección indicada en consola. Las portadas y Chart.js se sirven localmente.

## Credenciales de prueba

Consulta [credenciales de prueba](documentos/credenciales-prueba.md). Son **solo para demostración académica**; no se deben usar en producción. La semilla contiene diez obras ficticias con portadas locales.

## Roles

Visitante anónimo, `ClienteMinorista`, `ClienteInstitucional`, `AsesorVentas` y `Administrador`. El registro público asigna el rol minorista; la cuenta institucional requiere aprobación. Las cuentas administradoras no se pueden modificar desde la pantalla de roles.

## Arquitectura MVC

Los controladores autorizan y reciben peticiones, los servicios concentran las reglas, EF Core accede a SQL Server y Razor presenta los resultados. Identity gestiona cuentas, QuestPDF genera cotizaciones y Chart.js muestra los estados de pedidos. Véanse [arquitectura](documentos/arquitectura.md) y [modelo de datos](documentos/modelo-datos.md).

## Estructura de carpetas

- `src/CloudBooks.Web/Controllers`: controladores MVC.
- `Modelos`, `ModelosVista`, `Servicios` y `Datos`: entidades, datos para vistas, reglas y contexto/migraciones.
- `Views` y `wwwroot`: vistas, estilos, imágenes y bibliotecas locales.
- `pruebas`: pruebas automatizadas; `documentos`: guías; `base-datos`: SQL de entrega.

## Funcionalidades

Catálogo con búsqueda y filtros; carrito persistente; descuentos e impuesto; pago simulado; pedidos y aprobación transaccional; inventario; gestión administrativa de libros, autores, categorías, editoriales y usuarios; dashboard; solicitudes institucionales; asignación de asesores; cotizaciones, respuesta y PDF.

El administrador configura la tasa de impuesto, el umbral de stock bajo y la vigencia de nuevas cotizaciones. Los valores iniciales son 12 %, 5 unidades y 15 días. Pedidos y cotizaciones existentes conservan sus importes, tasa y fecha de vencimiento. Las ventas del dashboard incluyen solo pedidos aprobados y enviados. Para el flujo institucional consulta [su guía](documentos/flujo-institucional.md).

## Pruebas

```powershell
dotnet build
dotnet test
```

Las pruebas automatizadas usan SQLite aislado para reglas y servicios; las operaciones de SQL Server y recorridos de navegador se deben verificar también en el entorno de destino. Recorrido sugerido: [guía de demostración](documentos/guia-demostracion.md).

## Integrantes

- Dóminick Ricardo Cifuentes Tomás
- Yeisson Alexander Poroj Toc
