# Guion de demostración final — 10 minutos

Preparar una base de demostración dedicada, aplicar migraciones, arrancar en Development con `Correo:Proveedor=Simulado` y verificar las [cuentas y tarjetas ficticias](credenciales-prueba.md). Comprobar existencias y dejar disponibles un pedido en revisión y una cotización para evitar esperas. El pago es simulado y el comprobante no es factura fiscal. Un pedido en revisión no reserva existencias.

| Tiempo | Recorrido |
|---|---|
| 0:00–1:00 | Presentar CloudBooks y el diagrama de [arquitectura MVC](arquitectura.md): controlador, servicio, modelo, vista y SQL Server. |
| 1:00–2:00 | Explorar y filtrar el catálogo; abrir un libro y agregarlo al carrito como visitante. |
| 2:00–4:00 | Iniciar sesión como cliente, comprobar el carrito y agregar seis ejemplares de una obra con stock holgado. Mostrar descuento e IVA, usar tarjeta ficticia aprobada y confirmar. Enseñar aprobación automática, historial y comprobante PDF. |
| 4:00–5:00 | Mostrar un pedido preparado con titular distinto y explicar la revisión académica sin afirmar fraude. La autorización queda pendiente y el stock no está reservado. |
| 5:00–6:30 | Entrar como administrador: revisar motivo y estado del pago, validar el pedido y aprobarlo si hay stock; consultar inventario y dashboard. |
| 6:30–7:30 | Abrir Avisos, historial y plantillas de demostración. Explicar que las intenciones se guardan en una cola persistente; en modo simulado no se envía correo. Resend es configurable y una aceptación del proveedor no confirma entrega al buzón. |
| 7:30–9:00 | Mostrar una cuenta institucional aprobada, solicitud especial, asesor asignado y cotización preparada; descargar el PDF como cliente. |
| 9:00–10:00 | Mostrar roles, parámetros comerciales y protección de stock. Abrir el enlace de WhatsApp sin enviar el mensaje: una persona decide si lo envía. |

Para mostrar la aprobación institucional, volver a iniciar sesión tras la decisión administrativa. Si no hay asesor activo, administración puede reintentar la asignación. Un recorrido HTTP aislado comprobó la secuencia de aprobación, asignación, cotización y aceptación; este guion todavía debe ensayarse en el navegador y con los datos preparados para la presentación.
## Preparación y estado del ensayo — continuación del 5 de noviembre

**Pendiente de ensayo completo cronometrado.** Los recorridos parciales de compra, revisión, administración e institución se comprobaron por separado, pero no constituyen una demostración completa de diez minutos. No usar como datos finales el pedido manipulado para el PDF largo.

Para el ensayo, restaurar el SQL en una base exclusiva, usar Development y `Correo:Proveedor=Simulado`; confirmar las cuentas de demostración, stock del libro elegido y las tarjetas ficticias antes de empezar. Preparar una compra aprobada y otra con titular distinto para revisión; comprobar dashboard e inventario. Crear una solicitud institucional, aprobarla, volver a iniciar sesión, crear solicitud comercial, ingresar como asesor y emitir una cotización. Comprobar acceso al PDF como dueño, historial y plantillas simuladas. Para fondos insuficientes, verificar el mensaje y la conservación del carrito antes de la compra aprobada. En WhatsApp mostrar el enlace sin enviar. El guion de tiempos anterior debe recorrerse una vez con cronómetro y registrar el resultado antes de declarar el ensayo exitoso.

## Recorrido ejecutado en esta continuación

Se recorrieron en Chromium dos partes automáticas: (1) inicio, catálogo, carrito de visitante, ingreso, compra aprobada y comprobante, pedido en revisión, dashboard, inventario, avisos y 13 plantillas; (2) solicitud institucional, revisión por asesor, emisión y PDF de cotización, y enlaces WhatsApp. Las partes tardaron aproximadamente 5 y 4 segundos de automatización, sin exposición oral; la primera tuvo que detenerse antes de la solicitud por un selector ambiguo, resuelto en la segunda. **No se considera un ensayo de diez minutos ni un recorrido continuo completo.**

Para el ensayo pendiente, restaurar el SQL en una base exclusiva, preparar stock suficiente y las cuentas de prueba, y dejar lista una cotización o solicitud. Recorrer la tabla de tiempos con una persona y un cronómetro; registrar duración, incidencias y resultado. No activar Resend ni enviar WhatsApp. Si el tiempo se excede, precargar el pedido en revisión y la cotización; no afirmar que la prueba automática anterior validó el tiempo de exposición.
