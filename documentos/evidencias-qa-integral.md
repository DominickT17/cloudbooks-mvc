## Recorrido técnico continuo — versión candidata

En `CloudBooks_Demo_Entrega_20261105` se prepararon cinco cuentas académicas, stock 98 del libro de demostración, un pedido aprobado, uno en revisión, una cotización vigente, dos avisos y ocho correos Simulados. Se creó un respaldo `COPY_ONLY` con `CHECKSUM`, se verificó y se restauró en `CloudBooks_QA_Ensayo_20261105`. No se modificó la base principal.

En la copia, un único recorrido automatizado Chromium duró **13,7 s**: visitante busca y agrega seis libros, inicia sesión y conserva el carrito, verifica Q702.24 con descuento e IVA, compra y descarga su comprobante; prueba fondos insuficientes y revisión por titular distinto; consulta pedido, dashboard, inventario, avisos, historial y 13 plantillas; como institucional consulta solicitud, cotización y PDF, el asesor ve la asignación y el cliente acepta. Se verificó acceso del segundo administrador y los enlaces WhatsApp sin enviar. El stock pasó de 98 a 92. La cola registró la respuesta de cotización, aprobación, revisión y rechazo en estado Simulado. La copia se restableció una vez desde el respaldo para comprobar el procedimiento; volvió a tener dos pedidos, una cotización vigente y stock 98.

El primer intento se detuvo por el propio guion: el cliente HTTP siguió una redirección y el resultado se interpretó incorrectamente como acceso al PDF ajeno. Se desactivó el seguimiento en esa comprobación, se restauró la copia y se repitió el recorrido completo con éxito. No se modificó la aplicación por ese error del guion. El recorrido no demuestra diez minutos de exposición oral.

Se comprobaron acciones en la última columna, tras desplazar horizontalmente, en pedidos, catálogo administrativo, inventario, usuarios, instituciones, solicitudes comerciales y cotizaciones a 1440, 1024, 768 y 375 px. En móvil se inspeccionaron además capturas de las últimas columnas. No se observó un defecto que impidiera esas acciones. La [captura de pedidos a 375](evidencias/diseno-final/pedidos-acciones-375.png) se guardó como evidencia. Las verificaciones de seguridad y plantillas anteriores permanecen vigentes; no se afirma cobertura exhaustiva.

La base preparada y su respaldo permanecen en este equipo; la base de ensayo es temporal. Véanse [instrucciones de presentación](instrucciones-presentacion.md) y [checklist](checklist-entrega-final.md). El acceso visual a Gmail/Outlook y la URL pública siguen siendo externos y no son bloqueantes por sí mismos.

---

## Cierre funcional: verificaciones adicionales del 5 de noviembre

- Una prueba HTTP nueva, ejecutada en SQL Server restaurado para QA, preparó dos carritos de 6 y 8 unidades sobre stock 12. La primera compra se aprobó; la segunda redirigió al carrito con mensaje de existencias, conservó 8 unidades, no produjo pedido nuevo ni aviso adicional y dejó stock 6. En navegador se reprodujo el rechazo y se ajustó el carrito a 6. [Captura](evidencias/diseno-final/stock-insuficiente-1024.png). No se probó que todas las ejecuciones simultáneas tengan el mismo orden; la prueba concurrente previa cubre otra intercalación.
- Una prueba HTTP nueva ejerció trece POST administrativos con token de visitante y de minorista, institucional y asesor: se denegaron según rol. Para administrador sin token recibieron 400; un intento con token de cambiar el rol de otro administrador no alteró sus roles. Se comprobó que no cambió el stock observado. No es una matriz exhaustiva de todos los endpoints ni demuestra todos los cambios autorizados.
- Se amplió el recorrido institucional en una base restaurada distinta: otro cliente institucional recibió 404 al ver, descargar o responder una cotización ajena y el asesor fue denegado al responder; la cotización seguía vigente hasta la respuesta del dueño. La aceptación, vencimiento y rechazo previos siguen cubiertos por el mismo recorrido.
- En navegador se verificaron alta/edición/desactivación/reactivación de libro QA y validación del formulario; filtros y rechazo de stock negativo; cambio de rol permitido y restauración; error de configuración fuera de rango; y estados vacíos de dashboard, carrito y búsqueda del catálogo. Son escenarios puntuales.
- Los enlaces `wa.me` de inicio, pedido, usuarios, institución, solicitud y cotización se revisaron mediante sus URL, con el número comercial o teléfono ficticio normalizado, texto contextual y caso de usuario sin teléfono. No se abrió ni envió WhatsApp.
- Recorrido de navegador en dos sesiones rápidas: catálogo, carrito de visitante, ingreso, compra aprobada, comprobante propio, pedido por titular distinto, dashboard, inventario, avisos y 13 plantillas; luego solicitud institucional, asesor, emisión y PDF de cotización, y enlaces WhatsApp. **No fue una presentación única de diez minutos ni se completó el guion oral.**
- Se encontró en Gmail el mensaje recibido anteriormente, pero la herramienta de interfaz no inició por el error `failed to write kernel assets`; por tanto no se observó su apariencia real. No se envió correo nuevo. No se identificó una guía docente en los archivos con los nombres buscados.


## Continuación del 5 de noviembre: pruebas adicionales

- Compilación posterior a las correcciones: cero errores y advertencias. La ejecución ordinaria más reciente se indica en la sección de estado final; las pruebas SQL especiales requieren base dedicada y no se consideran aprobadas al aparecer omitidas.
- En base nueva pasó el recorrido HTTP institucional ampliado: solicitud aprobada, asignación, cotización aceptada, otra vencida y otra rechazada; se comprobaron los estados de solicitud y cotización y la intención de correo de rechazo.
- En otra base nueva pasó una prueba SQL de dos compras concurrentes del mismo e-book: se comprobó que las compras aprobadas no consumen existencias digitales y que un conflicto controlado conserva el carrito y admite reintento. Es una ejecución observada, no prueba de todos los entrelazados.
- En base separada se comprobó que un bloqueo de correo vigente no se reclama y uno vencido se recupera, terminando en Simulado y liberando el bloqueo. No se simuló la caída real de un proceso.
- Matriz HTTP de lectura: 12 rutas administrativas, visitante, cliente minorista, institucional, asesor y dos administradores. El visitante fue redirigido al ingreso, los tres roles no administrativos a acceso denegado y ambos administradores recibieron 200. No cubre todos los POST ni todos los recursos privados.
- Se generaron comprobante y cotización de tres páginas con 40 conceptos académicos largos añadidos exclusivamente en QA. Se detectó una descripción dividida entre páginas y se cambió la composición para intentar mantener la fila unida; en los PDF regenerados se inspeccionaron las tres páginas, encabezados de tabla, pie y totales. [Comprobante multipágina](evidencias/qa-cierre/comprobante-multipagina-qa.pdf). La cotización se revisó temporalmente, pero no se adjunta porque incluye un correo personal en el encabezado. Los importes del ejemplo se recalcularon para los datos artificiales y no representan una venta real.
- Playwright comprobó en la base visual búsqueda de pedidos y apertura de detalle, búsqueda y actualización de inventario, filtro de usuarios y protección visual, guardado de configuración, menús con Escape y foco, tarjeta vencida y sin fondos con carrito conservado, y revisión por titular distinto. El flujo previo de compra aprobada también pasó por HTTP en esa base. La matriz visual y sus límites están en [revisión de diseño](revision-diseno-final.md).

# Evidencias de QA integral — 4 de noviembre de 2026

> Para la continuación de diseño del 5 de noviembre, incluidas las mediciones por resolución y las capturas pendientes, consulta [revisión de diseño final](revision-diseno-final.md). Las secciones históricas siguientes conservan el alcance y los límites de sus respectivas ejecuciones.

## Entorno y reproducción

Se restauró `base-datos/CloudBooks.sql` con `sqlcmd -f 65001` en `CloudBooks_QA_Integral_01a1a8e9`, una base temporal de SQL Server Express que se eliminó después de las pruebas. Los recorridos HTTP utilizaron `WebApplicationFactory`/TestServer en proceso, con conexión forzada a esa base y `IProveedorCorreo` sustituido por el proveedor simulado. No se habilitó Resend ni se mostraron o modificaron secretos. Las pruebas de SQL solo se habilitan mediante `CLOUDBOOKS_QA_SQL`; las nuevas verifican además que el nombre de base empiece por `CloudBooks_QA_`.

Los filtros de pruebas especiales deben ejecutarse sobre bases temporales nuevas e independientes, restauradas desde el SQL de entrega; modifican sus datos. Ejemplo, tras crear y restaurar una base dedicada:

```powershell
$env:CLOUDBOOKS_QA_SQL = 'Server=.\SQLEXPRESS;Database=CloudBooks_QA_NOMBRE_DE_PRUEBA;Trusted_Connection=True;TrustServerCertificate=True'
dotnet test --filter FullyQualifiedName~HttpSqlPruebas
dotnet test --filter FullyQualifiedName~ConcurrenciaSqlPruebas
Remove-Item Env:CLOUDBOOKS_QA_SQL
```

Utiliza una base nueva por filtro cuando se requiera reproducibilidad completa. No combines las clases de correo y concurrencia en una misma ejecución: comparten la cola y pueden interferir. Para restaurar con `sqlcmd`, habilita `QUOTED_IDENTIFIER` con `-I` además de `-f 65001`; sin `-I` puede fallar la creación de índices filtrados. Eliminar únicamente la base creada para QA al terminar. Las pruebas reales de Resend permanecen desactivadas; no se realizó un nuevo envío.

## Continuación de cierre — 4 de noviembre de 2026

- Restauración aislada con `sqlcmd -b -I -f 65001`: 17 migraciones, 10 libros, 5 usuarios, 25 claves foráneas y muestra «María López». SQL Server advirtió que tres índices de Identity pueden exceder el límite teórico de longitud; la restauración terminó correctamente.
- Pruebas HTTP con SQL temporal nuevo: 2 aprobadas, sin activar Resend. Cubren las rutas y compra descritas abajo, no toda la matriz de permisos ni todo el recorrido institucional.
- En una primera ejecución concurrente SQL Server produjo un interbloqueo. EF Core envolvió el error 1205 en `InvalidOperationException`, por lo que no lo capturaba el código existente. Se añadió manejo específico: la transacción se revierte y se solicita revisar el carrito y reintentar. No se capturan otros errores como si fueran conflictos de inventario.
- Tras el cambio, en una base restaurada nueva pasaron 5 pruebas SQL: compras concurrentes, doble aprobación y reintento, aprobación frente a cancelación, rollback al faltar el segundo producto con reintento posterior y dos procesadores de correo. Esto no garantiza todos los entrelazados ni cubre aún e-books concurrentes o caída real de un trabajador.
- Un recorrido HTTP adicional, con base SQL nueva y proveedor simulado, pasó desde la solicitud institucional minorista y aprobación administrativa hasta el nuevo inicio de sesión, solicitud comercial, asignación del asesor, emisión y aceptación de la cotización. Verificó ambos estados finales, intenciones de correo, institución y vigencia en el mensaje, PDF propio válido y PDF ajeno 404. Una segunda cotización se venció ajustando su fecha en la base temporal: la consulta la marcó vencida y la aceptación posterior no cambió ese estado. Otra prueba en una base independiente comprobó el rechazo institucional, la notificación y la conservación del rol minorista. No se inspeccionó visualmente el PDF.
- La ejecución combinada de las clases de concurrencia y correo quedó esperando por interferencia de la cola compartida y se interrumpió; no se cuenta como aprobada. Los filtros aislados evitan esa interferencia.
- En una base independiente pasó la prueba SQL que verifica que un fallo del proveedor no revierte el pedido ni la intención persistida.
- Se descargaron y renderizaron un [comprobante](evidencias/qa-cierre/comprobante-qa.pdf) y una [cotización](evidencias/qa-cierre/cotizacion-qa.pdf) con datos académicos. Ambos tienen una página y muestran logo, tabla, descuentos, IVA y total legibles; el comprobante indica que no es factura fiscal. La cotización se corrigió para indicar «Simulación académica» y se volvió a renderizar. No se probaron documentos largos ni saltos de página.
- Inspección visual parcial en navegador local: dashboard a 1440 y 375 px y las 13 variantes de la vista Development en un viewport de 768 px, con un marco de correo cercano a 600 px. Se observaron los encabezados y partes visibles del contenido; se revisaron además el resumen del pedido, la cotización emitida y el aviso administrativo. El correo aprobado se abrió en viewports de 430 y 375 px (el marco interior es más estrecho). En el dashboard a 375 px se midió un ancho de documento de 360 px. No se revisaron todos los pies y botones, contenido largo ni las 13 variantes a cada ancho; no se guardaron capturas del navegador. Gmail y Outlook requieren pruebas reales separadas.

## Resultados comprobados

- HTTP en proceso: visitante accede al catálogo, Administración redirige al ingreso y POST de carrito sin antiforgery responde 400. Se inició sesión con minorista, institucional, asesor y las dos cuentas administradoras demo. Se comprobó una ruta propia y una ajena por rol; la segunda administradora accedió a once rutas de administración. No es una matriz exhaustiva de todos los endpoints.
- HTTP en proceso: visitante abre Inicio, busca y consulta un libro, agrega seis unidades al carrito, inicia sesión y conserva el carrito fusionado. Una tarjeta sin fondos deja el stock en 100 y registra el intento; una tarjeta aprobada produce pedido aprobado, pago confirmado, Q702.24 y stock 94. Se verificaron la cola, PDF propio con cabecera PDF y respuesta 404 al descargarlo desde otra cuenta. No se inspeccionó visualmente el PDF.
- SQL Server, dos contextos y arranque coordinado: stock 12; se aprobaron 3 unidades, stock final 9, y la compra de 10 recibió error de disponibilidad conservando su carrito. Es un resultado observado, no prueba matemática de todos los entrelazados.
- SQL Server, aprobación y cancelación concurrentes del mismo pedido revisado: solo la cancelación se completó, pago anulado y stock 12. Se rechazó una aprobación posterior.
- SQL Server, dos procesadores independientes y proveedor controlado bloqueado: una llamada al proveedor, un intento y estado Simulado. No se envió correo externo.
- Las pruebas unitarias existentes cubren reglas de descuentos, nombres de titular, tarjetas, casos de stock y cotizaciones. La prueba SQL de la sesión anterior cubrió solicitud institucional, asignación y emisión; esta sesión no repitió el flujo completo hasta PDF y respuesta.

## Límites y pendientes

No se ejecutó navegador visual ni se probaron los tamaños 1440, 1024, 768 y 375 px; no hay capturas. No se verificaron por HTTP todos los permisos, compras ahora, todas las tarjetas, revisión administrativa, flujo institucional completo, contenido visual de PDF ni enlaces WhatsApp. Falta probar más entrelazados: doble aprobación simultánea, fallo en segundo producto en SQL Server, e-book bajo concurrencia y reintentos. La prueba de dos trabajadores no cubre caída de proceso real ni expiración de lease bajo carga. Los filtros HTTP y SQL especiales pueden requerir bases recién restauradas para evitar interacción entre escenarios. No se localizó guía oficial docente.

Se comprobó adicionalmente en navegador la transición aprobado → enviado, revisión comercial → cancelado y revisión de inventario → revisado → aprobado, con stock preparado exclusivamente en QA. No se observó el estado pendiente en esa ejecución.
