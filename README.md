# CloudBooks

Plataforma web editorial multicanal para lectores e instituciones. El proyecto está en desarrollo; actualmente contiene la base MVC y una página de presentación. El catálogo y las operaciones comerciales todavía no están implementados.

## Tecnologías

ASP.NET Core MVC y .NET 10. Se prevé utilizar Entity Framework Core, SQL Server e Identity en los siguientes bloques.

## Requisitos

SDK de .NET 10. Para las siguientes fases se utilizará SQL Server.

## Ejecución

```powershell
dotnet restore
dotnet run --project src/CloudBooks.Web
```

Abre la dirección que indique la consola.

## Estructura y arquitectura MVC

La solución se encuentra en `CloudBooks.sln` y la aplicación en `src/CloudBooks.Web`. Se conservan `Controllers`, `Models` y `Views` por las convenciones de ASP.NET Core; el código propio utiliza nombres en español. El controlador recibe la petición y selecciona la vista; la lógica comercial se ubicará en servicios del modelo.

## Integrantes

- Dóminick Ricardo Cifuentes Tomás
- Yeisson Alexander Poroj Toc
