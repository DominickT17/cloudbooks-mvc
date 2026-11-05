# Checklist de entrega CloudBooks — 5 de noviembre de 2026

Estado: **NO LISTO PARA ENTREGA** frente a los criterios explícitos de esta solicitud. Los resultados parciales y fuentes están en [QA](evidencias-qa-integral.md), [diseño](revision-diseno-final.md) y [guion](guion-demostracion-final.md). No se localizó una guía docente con los nombres buscados; esta lista no le atribuye requisitos.

| Elemento | Resultado verificable | Pendiente |
|---|---|---|
| 14 rutas y cuatro anchos | Imágenes completas inspeccionadas en el estado capturado; 19 PNG conservados en [evidencias](evidencias/diseno-final/README.md). | Recorrer columnas ocultas de tablas y completar estados y acciones en los cuatro anchos. |
| Catálogo e inventario | Alta, edición, desactivación/reactivación, búsqueda vacía, filtros y rechazo de negativo en QA. | Completar combinaciones de formularios y validaciones. |
| Checkout | HTTP y navegador: dos carritos con stock cambiante; carrito conservado y cantidad ajustable, sin segundo pedido ni aviso. | Revisar el estado pendiente y las combinaciones restantes. |
| Roles y privados | Lecturas de 12 rutas, trece POST con antiforgery y roles, protección de administrador; cotización propia/ajena y PDF comprobados. | Ampliar la matriz a los demás POST, recursos y mutaciones autorizadas. |
| Instituciones y cotizaciones | Solicitud, asesor, emisión, aceptación, rechazo, vencimiento y PDF comprobados en pruebas aisladas. | Revisar visualmente todos los estados y acciones. |
| WhatsApp | Se inspeccionaron los seis tipos de enlace y el caso sin teléfono; no se envió nada. | Ningún envío automático es parte de la prueba. |
| Correos | 13 variantes vistas en HTML; un mensaje anterior llegó a Gmail. | Inspección visual en Gmail/Outlook y botones con URL pública válida si se dispone de acceso; son dependencias externas. |
| PDF | Comprobante y cotización multipágina inspeccionados previamente; descargas propias y ajenas verificadas por HTTP. | Mantener los ejemplos académicos y no presentar el comprobante como factura fiscal. |
| Demo | Recorridos rápidos en dos partes con datos temporales y correo simulado. | Ensayar el guion completo en una sesión de aproximadamente diez minutos y registrar tiempo e incidencias. |
| Pruebas | Build 0 errores/advertencias; `dotnet test --no-build`: 92 aprobadas, 16 omitidas, 0 fallidas de 108. Dos pruebas SQL nuevas y la institucional ampliada pasaron por separado. | Las pruebas omitidas requieren bases SQL dedicadas o autorización para correo real. |
| Git | Verificar rama limpia y sincronizada tras publicar esta continuación. | No hacer merge sin autorización. |
