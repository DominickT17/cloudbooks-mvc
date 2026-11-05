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
