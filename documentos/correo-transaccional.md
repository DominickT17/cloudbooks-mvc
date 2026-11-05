# Correo transaccional

CloudBooks guarda las intenciones de correo en SQL dentro de la operación comercial y las procesa después mediante un servicio en segundo plano. Se registran pedidos aprobados, en revisión o cancelados, intentos de pago rechazados, solicitudes institucionales y comerciales, asignación de asesor, emisión y respuesta de cotizaciones. Los avisos administrativos aparecen en Administración > Avisos; el historial de correos se abre desde esa pantalla. La vista previa está aislada en un iframe y el acceso está restringido al rol Administrador.

Por defecto se usa el proveedor **simulado**. El HTML y el destinatario quedan registrados para inspección, pero no se envía ningún mensaje. La activación de Resend es explícita. Los correos ya marcados como simulados no se envían al activar Resend; realiza nuevas operaciones de prueba con destinatarios controlados.

## Configurar Resend

1. Crear una clave de API y verificar un dominio de envío en Resend. La dirección remitente debe pertenecer al dominio autorizado; no se puede asumir que una cuenta Gmail personal sirve como remitente de otro dominio. Revisa la [guía oficial para .NET](https://resend.com/docs/send-with-dotnet).
2. Configurar la clave fuera de Git con user-secrets en Development (sustituye los marcadores localmente, sin enviarlos en el chat):

```powershell
dotnet user-secrets set "Correo:ApiKey" "<CLAVE_LOCAL>" --project src/CloudBooks.Web
dotnet user-secrets set "Correo:Remitente" "CloudBooks <notificaciones@dominio-verificado.example>" --project src/CloudBooks.Web
dotnet user-secrets set "Correo:Proveedor" "Resend" --project src/CloudBooks.Web
dotnet user-secrets set "Correo:UrlBase" "https://tu-dominio.example" --project src/CloudBooks.Web
dotnet user-secrets set "Correo:Administradores" "admin-controlado@example.test" --project src/CloudBooks.Web
```

También pueden utilizarse variables de entorno `Correo__ApiKey`, `Correo__Remitente`, `Correo__Proveedor`, `Correo__UrlBase` y `Correo__Administradores`; los destinatarios se separan con punto y coma. Si no se configura la lista, se utiliza `dominickt1703@gmail.com`. Utiliza únicamente destinatarios autorizados al activar el envío real. Para volver al modo simulado, cambia `Correo:Proveedor` a `Simulado` o quita la clave. Si faltan la clave o un remitente con sintaxis válida, se selecciona el proveedor simulado. La aplicación no comprueba anticipadamente que el dominio esté verificado: el proveedor puede rechazarlo.

`Correo:UrlBase` debe ser una URL pública controlada por el equipo para que las imágenes y enlaces funcionen fuera del equipo local. Sin ella se conserva la vista previa, pero los enlaces a la aplicación no aparecen. Las rutas de pedidos y cotizaciones mantienen sus comprobaciones de autorización; es necesario iniciar sesión para acceder a documentos privados.

## Entrega y reintentos

La cola distingue Pendiente, EnProceso, AceptadoProveedor, Simulado, ReintentoPendiente y Fallido. Un bloqueo temporal y una actualización condicional impiden que dos trabajadores reclamen simultáneamente el mismo mensaje; tras un reinicio se recuperan los bloqueos vencidos. Los fallos programan hasta cinco intentos con espera progresiva. El historial permite reintentar manualmente un fallo solo dentro de la ventana segura.

Cada correo tiene una clave de envío persistente y una clave única de evento. Resend considera su clave de idempotencia durante [24 horas](https://resend.com/docs/dashboard/emails/idempotency-keys); CloudBooks detiene los reintentos cuando el primer intento tiene más de 23 horas. En ese caso hay que investigar en el proveedor si lo aceptó; no se reenvía automáticamente porque el resultado podría ser incierto. «AceptadoProveedor» no significa entregado al buzón. El sistema no implementa confirmación por webhook.

Un fallo de Resend no revierte un pedido ya confirmado. Guardar la intención en la base es parte de la transacción comercial para evitar perderla; una falla de base de datos antes de confirmar sí impide completar esa transacción. La cola solo trabaja mientras la aplicación se ejecuta; no es un servicio 24/7 independiente.

## Verificación

Aplicar migraciones antes de iniciar la versión nueva:

```powershell
dotnet ef database update --project src/CloudBooks.Web
dotnet build
dotnet test
```

Las pruebas habituales usan SQLite; la prueba marcada para SQL Server requiere `CLOUDBOOKS_QA_SQL` apuntando **únicamente a una base temporal restaurada para QA**, ya que modifica datos. Sin esa variable se omite. No habilites Resend para pruebas con direcciones ficticias. WhatsApp abre un mensaje que una persona debe enviar manualmente; no usa una API de envío.
