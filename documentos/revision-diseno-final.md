# Revisión de diseño final — 5 de noviembre de 2026

## Alcance y leyenda

Se utilizó Development con `CloudBooks_QA_Diseno_01a1aa1f`, restaurada desde el SQL de entrega y poblada con operaciones académicas mediante pruebas HTTP. El proveedor se forzó a `Simulado`. Se inspeccionaron vistas en el navegador integrado y se midió `document.documentElement.scrollWidth` frente al viewport para las 14 rutas de la matriz. No se observaron desbordamientos **globales** en esas mediciones; las tablas conservan desplazamiento horizontal interno.

La verificación es parcial: una captura de la parte visible y una medición no acreditan todos los estados, toda la página, sus acciones, contraste o todos los recorridos. La herramienta mostró imágenes durante la sesión pero no proporcionó una operación documentada de guardado directo en el repositorio. Por ello la columna **Captura** queda pendiente y el diseño no se declara terminado.

- **CV\***: corregido y visualizado en la parte inicial de la página en ese ancho; revisión integral pendiente.
- **V\***: visualizado en la parte inicial sin cambios específicos en esa vista; revisión integral pendiente.
- **P¹**: pendiente de inspección visual integral; se comprobó únicamente el ancho global de esa ruta.
- **Bloqueado para evidencia guardada**: no se conservaron capturas como archivos. Procedimiento y nombres en [evidencias/diseno-final](evidencias/diseno-final/README.md).

## Matriz de pantallas

| Pantalla | 1440 | 1024 | 768 | 375 | Problemas y correcciones | Captura |
|---|---|---|---|---|---|---|
| Dashboard | CV* | CV* | CV* | CV* | Identidad, resumen, prioridades y actividad real reorganizados; comprobar estados con mayor volumen. | Pendiente |
| Pedidos | CV* | CV* | CV* | CV* | Filtros, prioridad y hora de Guatemala; se corrigió desbordamiento global en móvil. Comprobar todas las acciones y alertas. | Pendiente |
| Detalle de pedido | CV* | CV* | CV* | CV* | Se agruparon cliente, pago, evaluación, importes y registro; no hay historial de fechas de cambios de estado. | Pendiente |
| Catálogo administrativo | CV* | CV* | CV* | CV* | Resumen, filtros, portadas, acciones y formulario por secciones; corregido precio mostrado como texto Razor. | Pendiente |
| Inventario | CV* | CV* | CV* | CV* | Resumen y filtros por disponibilidad; cada actualización identifica el libro y conserva control de concurrencia. | Pendiente |
| Usuarios | CV* | CV* | CV* | CV* | Directorio, iniciales, estado, contacto y cambio de rol plegado; conservadas protecciones del servidor. | Pendiente |
| Instituciones | CV* | CV* | CV* | CV* | Resumen, búsqueda, estados y detalle de solicitud organizados. | Pendiente |
| Solicitudes comerciales | CV* | CV* | CV* | CV* | Seguimiento, filtros, fecha local y detalle organizados; revisar más estados con datos. | Pendiente |
| Cotizaciones | CV* | CV* | CV* | CV* | Vigencia, vencimiento cercano, estados y accesos a detalle, PDF y solicitud; revisar más estados. | Pendiente |
| Configuración | CV* | CV* | CV* | CV* | Cuatro secciones y proveedor visible sin secretos ni botón de activación. | Pendiente |
| Inicio público | V* | V* | V* | V* | Se conservó la composición existente; falta revisar el resto de la página y sus enlaces en cada ancho. | Pendiente |
| Carrito | V* | V* | V* | V* | Vista con un libro de QA; falta revisar estados vacíos y errores en cada ancho. | Pendiente |
| Checkout | CV* | CV* | CV* | CV* | Productos, métodos y resumen reorganizados; pago simulado identificado. Falta inspección integral de errores y controles en cada ancho. | Pendiente |
| Mi cuenta | CV* | CV* | CV* | CV* | Identidad, contacto y accesos organizados; se midió de nuevo tras el cambio. | Pendiente |

## Comprobaciones adicionales

- El menú administrativo a 375 px abrió y se cerró con `Escape`, devolviendo el foco al control. Las tablas marcadas permiten foco y desplazamiento horizontal por teclado; no se ejecutó una auditoría completa de teclado o contraste.
- El filtro de e-books devolvió dos títulos en el conjunto de prueba. Tras detectar un precio que se imprimía literalmente como código Razor, se corrigió y se añadió una comprobación HTTP. El filtro de fecha de pedidos encontró el pedido en el día de Guatemala y no en el día UTC siguiente.
- Con un teléfono ficticio en la base temporal se verificaron los `href` de WhatsApp en Inicio, pedido, usuarios, institución, solicitud y cotización. El número se normalizó a `50255551234`; no se abrió WhatsApp ni se envió ningún mensaje. Se corrigió el texto de un pedido ya aprobado para que no solicite validación previa al procesamiento.
- Las trece plantillas de demostración se abrieron con marcos de 600, 430 y 375 px. Se visualizó un título largo a 375 px y la cotización con el resumen tabular. Algunos mensajes largos requerían desplazamiento del iframe y las imágenes observadas no abarcaron siempre el encabezado y el pie completos; la revisión completa de todos los pies y botones sigue pendiente. Sin URL pública configurada no se mostraron botones a la aplicación. Gmail y Outlook no se probaron.
- `dotnet test --no-restore`: 92 aprobadas, 0 fallidas, 11 omitidas de 103. En una base separada pasaron dos pruebas HTTP de rutas/compra y otra del flujo institucional, con correo simulado. La ejecución ordinaria omite las pruebas especiales y el envío real.

## Trabajo pendiente

1. Guardar las capturas reales indicadas en la carpeta de evidencias y revisar cada página completa, incluidas sus tablas y formularios, en los cuatro anchos.
2. Comprobar botones de correo con una URL HTTPS pública válida y autorizada, pies y contenido largo en las 13 variantes, y realizar pruebas separadas en Gmail y Outlook.
3. Completar la revisión de accesibilidad, estados vacíos, errores del checkout y permisos/acciones para cada rol; comprobar las rutas y formatos con más datos.
4. No hacer merge ni declarar la fase terminada hasta cerrar esas evidencias.
