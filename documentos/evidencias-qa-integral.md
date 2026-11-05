# Evidencias de QA integral — 4 de noviembre de 2026

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

## Resultados comprobados

- HTTP en proceso: visitante accede al catálogo, Administración redirige al ingreso y POST de carrito sin antiforgery responde 400. Se inició sesión con minorista, institucional, asesor y las dos cuentas administradoras demo. Se comprobó una ruta propia y una ajena por rol; la segunda administradora accedió a once rutas de administración. No es una matriz exhaustiva de todos los endpoints.
- HTTP en proceso: visitante abre Inicio, busca y consulta un libro, agrega seis unidades al carrito, inicia sesión y conserva el carrito fusionado. Una tarjeta sin fondos deja el stock en 100 y registra el intento; una tarjeta aprobada produce pedido aprobado, pago confirmado, Q702.24 y stock 94. Se verificaron la cola, PDF propio con cabecera PDF y respuesta 404 al descargarlo desde otra cuenta. No se inspeccionó visualmente el PDF.
- SQL Server, dos contextos y arranque coordinado: stock 12; se aprobaron 3 unidades, stock final 9, y la compra de 10 recibió error de disponibilidad conservando su carrito. Es un resultado observado, no prueba matemática de todos los entrelazados.
- SQL Server, aprobación y cancelación concurrentes del mismo pedido revisado: solo la cancelación se completó, pago anulado y stock 12. Se rechazó una aprobación posterior.
- SQL Server, dos procesadores independientes y proveedor controlado bloqueado: una llamada al proveedor, un intento y estado Simulado. No se envió correo externo.
- Las pruebas unitarias existentes cubren reglas de descuentos, nombres de titular, tarjetas, casos de stock y cotizaciones. La prueba SQL de la sesión anterior cubrió solicitud institucional, asignación y emisión; esta sesión no repitió el flujo completo hasta PDF y respuesta.

## Límites y pendientes

No se ejecutó navegador visual ni se probaron los tamaños 1440, 1024, 768 y 375 px; no hay capturas. No se verificaron por HTTP todos los permisos, compras ahora, todas las tarjetas, revisión administrativa, flujo institucional completo, contenido visual de PDF ni enlaces WhatsApp. Falta probar más entrelazados: doble aprobación simultánea, fallo en segundo producto en SQL Server, e-book bajo concurrencia y reintentos. La prueba de dos trabajadores no cubre caída de proceso real ni expiración de lease bajo carga. Los filtros HTTP y SQL especiales pueden requerir bases recién restauradas para evitar interacción entre escenarios. No se localizó guía oficial docente.
