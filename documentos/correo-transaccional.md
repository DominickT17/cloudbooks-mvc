# Correo transaccional

CloudBooks guarda las intenciones de correo en SQL dentro de la operación comercial y las procesa después mediante un servicio en segundo plano. Se registran pedidos aprobados, en revisión o cancelados, intentos de pago rechazados, solicitudes institucionales y comerciales, asignación de asesor, emisión y respuesta de cotizaciones. Los avisos administrativos aparecen en Administración > Avisos; el historial de correos se abre desde esa pantalla. La vista previa está aislada en un iframe y el acceso está restringido al rol Administrador.

Por defecto se usa el proveedor **simulado**. El HTML y el destinatario quedan registrados para inspección, pero no se envía ningún mensaje. La activación de Resend es explícita. Los correos ya marcados como simulados no se envían al activar Resend; realiza nuevas operaciones de prueba con destinatarios controlados.

## Configurar Resend

1. Crear una clave de API y verificar un dominio de envío en Resend para enviar a clientes. `onboarding@resend.dev` es un remitente de prueba con restricciones; para uso normal configura un remitente de tu dominio verificado. Revisa la [guía oficial para .NET](https://resend.com/docs/send-with-dotnet).
2. Guardar la clave como `Resend:ApiKey` en user-secrets del proyecto en Development. Si ya existe, no hace falta volver a introducirla. La clave anterior `Correo:ApiKey` sigue funcionando como alternativa; si ambas están configuradas, prevalece `Resend:ApiKey`.
3. Cuando se desee habilitar el envío real, configurar el remitente, proveedor y destinatarios mediante user-secrets (sustituye los marcadores por valores autorizados):

```powershell
dotnet user-secrets set "Correo:Remitente" "CloudBooks <notificaciones@dominio-verificado.example>" --project src/CloudBooks.Web
dotnet user-secrets set "Correo:Proveedor" "Resend" --project src/CloudBooks.Web
dotnet user-secrets set "Correo:UrlBase" "https://tu-dominio.example" --project src/CloudBooks.Web
dotnet user-secrets set "Correo:Administradores" "admin-controlado@example.test" --project src/CloudBooks.Web
```

También pueden utilizarse variables de entorno `Resend__ApiKey`, `Correo__Remitente`, `Correo__Proveedor`, `Correo__UrlBase` y `Correo__Administradores`; los destinatarios se separan con punto y coma. Si no se configura la lista, se utiliza `dominickt1703@gmail.com`. Activar Resend permite que el servicio procese todos los mensajes pendientes, por lo que primero deben verificarse sus destinatarios y utilizarse un remitente apropiado. Para volver al modo simulado, cambia `Correo:Proveedor` a `Simulado`. Si faltan la clave o un remitente con sintaxis válida, se selecciona el proveedor simulado. La aplicación no comprueba anticipadamente que el dominio esté verificado: el proveedor puede rechazarlo.

`Correo:UrlBase` debe ser una URL HTTPS pública controlada por el equipo para que los enlaces funcionen fuera del equipo local. Si falta o apunta a localhost o una IP, no se insertan botones a la aplicación. Para utilizar el logo oficial se puede configurar `Correo:LogoUrl` con una URL HTTPS pública y accesible de la imagen PNG o JPEG; comprueba que sea realmente accesible sin autenticación antes de usarla. Sin ella se muestra la marca tipográfica CloudBooks. Las rutas de pedidos y cotizaciones mantienen sus comprobaciones de autorización; es necesario iniciar sesión para acceder a documentos privados.

## Plantillas y vista previa

Los correos nuevos usan una base de tablas y estilos inline, con preencabezado, marca, estado, contenido contextual y ayuda por WhatsApp. Incluyen una alternativa de texto plano; los mensajes anteriores en cola conservan su HTML y, si no tenían texto, se genera una versión básica al enviarlos. Los correos de compra muestran los importes guardados en el pedido, y las cotizaciones emitidas muestran institución, importe y fecha de vigencia. No se incrustan datos completos de tarjetas o CVV.

En Development, inicia sesión como administrador y abre **Administración → Avisos → Historial de correos → Ver plantillas de demostración**. Los trece ejemplos contienen datos ficticios y no crean ni envían correos. Si no existe `Correo:UrlBase`, se omiten los botones que apuntan a la aplicación. El iframe puede estrecharse para inspeccionar una aproximación de vista móvil. En otros entornos esta ruta devuelve 404. El resumen del pedido aprobado separa productos y totales en tablas HTML y conserva una alternativa de texto plano.

La vista permite seleccionar marcos de 600, 430 y 375 px; si la ventana es más estrecha, el marco se ajusta al espacio disponible. Los enlaces de ejemplos con URL configurada son ilustrativos y pueden apuntar a identificadores ficticios. En la revisión del 5 de noviembre se abrieron las trece variantes en esos anchos y se comprobó el ajuste de un título largo a 375 px. Parte del contenido largo requiere desplazar el iframe; la revisión de todos los pies y botones no quedó completa. La vista de navegador no sustituye una prueba en Gmail u Outlook. No se hicieron envíos reales.

El 4 de noviembre de 2026 se envió una única prueba controlada de la nueva plantilla de pedido aprobado a `dominickt1703@gmail.com`, con datos ficticios y sin crear un pedido comercial ni modificar inventario. Resend aceptó la solicitud con identificador `01a1a9a0-6a41-754b-8610-25f469193fbf`; esto no confirma entrega ni apariencia en el buzón. El ejemplo se envió sin URL de aplicación configurada, por lo que no incluyó botones de pedido o comprobante.

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

El 4 de noviembre de 2026 se realizó una única prueba real con `CloudBooks <onboarding@resend.dev>`, destinatario autorizado `dominickt1703@gmail.com` y asunto `CloudBooks - Prueba de correo`. La cola de una base temporal registró un intento aceptado por Resend con identificador `01a1a969-4d81-75c8-b910-420902d20440`. Se eliminó la base temporal después de comprobar el registro. La aceptación no confirma la entrega al buzón. El envío real de prueba automatizado queda desactivado salvo que se configure explícitamente su bandera y una base temporal dedicada.
## Recepción observada y comprobación visual pendiente — 5 de noviembre

La búsqueda del buzón Gmail mediante el conector encontró en la bandeja de entrada el ejemplo de pedido aprobado enviado el 4 de noviembre. Esto confirma recepción en esa cuenta; no se observó su presentación en la interfaz de Gmail ni en Outlook. Las 13 vistas de demostración se capturaron completas en marcos de 600, 430 y 375 px y se revisaron visualmente en hojas de contacto; faltan botones con URL pública válida y pruebas de clientes reales. Véase la [matriz de diseño](revision-diseno-final.md). No se enviaron mensajes nuevos.

## Continuación funcional del 5 de noviembre

La recepción anterior se comprobó mediante el conector de Gmail. Se intentó acceder a la interfaz para revisar su presentación, pero el entorno de interfaz falló al iniciar (`failed to write kernel assets`); la apariencia de Gmail escritorio/móvil y Outlook continúa sin comprobarse. No se realizaron envíos nuevos. Los botones a la aplicación permanecen condicionados a una URL HTTPS pública válida y controlada; la falta de esa URL o de Outlook no se presenta como un defecto de CloudBooks ni como requisito docente sin guía.

## Verificación del recorrido candidato

En la base de presentación se observaron ocho correos en estado Simulado. En la copia de ensayo, la respuesta de cotización y los eventos de compra/revisión quedaron también en Simulado. El recorrido abrió las 13 plantillas sin efectuar envíos. La apariencia en las interfaces de Gmail/Outlook y los enlaces hacia una URL pública controlada siguen siendo verificaciones externas; la recepción anterior en Gmail sí está documentada.
