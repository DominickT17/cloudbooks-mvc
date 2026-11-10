## Actualización del 10 de noviembre de 2026

**Estado: LISTO PARA REVISIÓN MANUAL FINAL.** La nueva selección de pago y las pruebas descritas en [QA integral](evidencias-qa-integral.md) pasaron en entornos aislados. Sigue pendiente el ensayo oral humano y la revisión visual en Gmail/Outlook; verificar la vigencia de la cotización el día de la presentación. Las secciones históricas siguientes documentan el estado de la versión anterior.

# Checklist de versión candidata — CloudBooks

**Estado del código: CANDIDATO A ENTREGA.** El recorrido técnico continuo y la restauración aislada pasaron; no se detectaron bloqueantes en los escenarios comprobados. Antes de presentar, el equipo debe efectuar el ensayo oral con cronómetro y comprobar la vigencia de la cotización. Esto no equivale a una aprobación docente ni autoriza el merge.

| Área | Resultado | Acción o límite |
|---|---|---|
| Base y arranque | SQL de entrega restaurado con 17 migraciones; base `CloudBooks_Demo_Entrega_20261105` preparada y respaldo local verificado y restaurado en una copia. | El respaldo es local a este equipo; seguir [instrucciones](instrucciones-presentacion.md). |
| Compra y stock | En una sesión continua: carrito fusionado, descuentos e IVA, compra aprobada, PDF y stock 98→92; fondos insuficientes conservan carrito y titular distinto produce revisión. | Usar tarjetas académicas; se trata de pago simulado. |
| Institución | Solicitud y asesor preparados; cotización propia, PDF y aceptación en navegador. Respuesta registrada en correo simulado. | La cotización preparada vence el 20/11/2026; crear otra si ha vencido. |
| Roles y recursos | Pruebas SQL/HTTP anteriores cubren lecturas administrativas, 13 POST con antiforgery y roles, protección de administradores y acceso a PDF/cotización propios y ajenos. | No se afirma cobertura de todas las combinaciones posibles. |
| Diseño | Las 14 rutas se inspeccionaron en cuatro anchos; se comprobaron acciones de las últimas columnas de siete tablas administrativas en 1440, 1024, 768 y 375. [Captura móvil](evidencias/diseno-final/pedidos-acciones-375.png). | Tablas estrechas requieren desplazamiento horizontal interno. |
| Correo y WhatsApp | 13 plantillas HTML, historial simulado y enlaces contextuales comprobados; no se envió nada en este cierre. | Gmail/Outlook y botones con URL HTTPS pública controlada son verificaciones externas opcionales salvo exigencia docente. |
| PDF | Comprobante y cotización descargados en el recorrido; la paginación larga fue inspeccionada previamente. | El comprobante es académico, no factura fiscal. |
| Demo | Recorrido automatizado continuo: 13,7 s, con datos preparados y ambos administradores; ver [guion](guion-demostracion-final.md). | **Falta ensayo oral humano de aproximadamente diez minutos**, con cronómetro y registro de incidencias. |
| Compilación y pruebas | Build: 0 errores y 0 advertencias. Pruebas ordinarias: 92 aprobadas, 16 omitidas, 0 fallidas de 108; especiales SQL se documentaron por separado. | La prueba de envío real permanece omitida. |
| Git | Publicar únicamente en `feature/base-mvc`. | No hacer merge sin autorización. |

No se localizó una guía docente con los nombres buscados, por lo que esta evaluación no le atribuye requisitos. Ver [evidencias de QA](evidencias-qa-integral.md) y [matriz visual](revision-diseno-final.md).
