# Guion final de demostración — aproximadamente 10 minutos

Usa la base y los accesos de [instrucciones de presentación](instrucciones-presentacion.md). Los tiempos son una propuesta para una persona; el recorrido automatizado continuo se ejecutó en 13,7 segundos y **no valida la duración ni la exposición oral**. Falta un ensayo presencial con cronómetro.

| Tiempo | Acción | Texto breve de exposición |
|---|---|---|
| 0:00–1:00 | Presentar inicio y [arquitectura](arquitectura.md). | «CloudBooks es una librería académica MVC: las vistas presentan, los controladores autorizan y los servicios aplican reglas sobre SQL Server». |
| 1:00–3:00 | Buscar un libro, agregar seis como visitante e iniciar sesión como minorista. | «El carrito se conserva al iniciar sesión. Aquí se ven el descuento por volumen, el IVA y el total». |
| 3:00–4:00 | Pagar con la tarjeta ficticia aprobada, abrir el pedido y descargar el comprobante. | «El pago es simulado y el comprobante no es una factura fiscal. Al aprobarse se actualiza la existencia». |
| 4:00–5:30 | Entrar como administrador y abrir el pedido preparado en revisión; mostrar dashboard e inventario. | «Un titular diferente requiere revisión académica; no implica afirmar fraude ni reserva existencias. El panel reúne alertas y stock». |
| 5:30–6:30 | Mostrar avisos, historial y plantillas de demostración. | «Los avisos de correo se guardan en una cola. En modo simulado no sale ningún mensaje». |
| 6:30–8:30 | Mostrar solicitud comercial y cotización como institucional; descargar PDF y mostrar la asignación al asesor. | «La institución solicita una propuesta, el asesor la prepara y el cliente puede consultar y responder su cotización». |
| 8:30–10:00 | Mostrar usuarios, configuración y enlace WhatsApp sin enviarlo. | «Los roles limitan las acciones; los parámetros afectan nuevas operaciones. WhatsApp abre un mensaje que una persona decide enviar». |

## Resultado técnico observado

En una copia restaurada del respaldo de presentación se ejecutaron los pasos de navegador de forma continua en 13,7 segundos: búsqueda de visitante, carrito fusionado, importe con descuento e IVA, compra aprobada, comprobante propio, fondos insuficientes con carrito conservado, nueva revisión, panel, stock 98→92, avisos e historial simulado, 13 plantillas, solicitud comercial preparada, asesor, cotización, PDF y aceptación, segundo administrador y enlaces WhatsApp. También se comprobó que un cliente sin el rol adecuado no accedió al PDF de cotización. Tras la aceptación, la cola registró el correo en estado Simulado. La prueba usó datos aislados y no acredita una exposición oral de diez minutos.

El primer intento se detuvo porque el guion de prueba siguió una redirección HTTP automáticamente; tras corregir el guion y restaurar la copia, la ejecución continua pasó. No se envió correo ni WhatsApp. Ensayar en persona con cronómetro, anotar tiempo e incidencias y restablecer la base preparada si es necesario.
