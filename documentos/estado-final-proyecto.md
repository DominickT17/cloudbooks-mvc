# Estado del proyecto — diagnóstico inicial, 4 de noviembre de 2026

> **Actualización de cierre:** la matriz inicial y las cifras históricas de abajo describen un momento anterior. El estado vigente de esta continuación aparece al final y en [evidencias de QA](evidencias-qa-integral.md).

Este diagnóstico es una evaluación parcial de preparación, no una certificación de entrega. Se revisaron los servicios críticos, controladores y documentos indicados abajo; no se han completado la auditoría exhaustiva, el recorrido en navegador ni las pruebas de todos los roles. No se localizó una guía o rúbrica oficial del docente en los archivos del repositorio. La rúbrica y los requisitos específicos evaluados aquí proceden de la solicitud del proyecto y no se atribuyen al docente.

## Matriz inicial

| Módulo | Estado | Problemas | Prioridad |
|---|---|---|---|
| Inicio y catálogo | Implementado, pendiente de pruebas. | Faltan recorridos de navegador y responsive. | Media |
| Autenticación | Parcial. | Roles y protección de administradores implementados; falta probar rutas por rol. Se corrigió el modelo nulo de Mi cuenta. | Crítica |
| Carrito | Implementado, pendiente de pruebas. | Faltan recorridos completos de invitado y fusión en navegador. | Alta |
| Pagos | Parcial. | Simulador y estados implementados; no se registran intentos rechazados. Falta QA completo. | Crítica |
| Pedidos | Parcial. | Aprobación automática y revisión implementadas; se corrigieron cancelación y aviso de aprobación. Falta QA HTTP y concurrencia SQL Server. | Crítica |
| Inventario | Implementado, pendiente de pruebas. | Actualización condicional y transacción presentes; falta probar concurrencia real en SQL Server. | Crítica |
| Instituciones | Implementado, pendiente de pruebas. | Flujo y pruebas de servicio existentes; falta recorrido completo con roles en navegador. | Alta |
| Asesores | Implementado, pendiente de pruebas. | Asignación y flujo existentes; falta recorrido completo. | Alta |
| Cotizaciones | Implementado, pendiente de pruebas. | Servicio y PDF existentes; falta verificar recorrido y archivo generado en esta sesión. | Alta |
| Notificaciones | Parcial. | Registro local de pedidos; sin proveedor de correo, envío real, estado de lectura ni historial general de eventos. | Alta |
| Administración | Parcial. | Módulos y panel implementados; faltan auditoría visual y responsive, filtros y QA por pantalla. | Alta |
| Documentación | Parcial. | README y arquitectura existentes; guía anterior desactualizada. Falta contrastar con guía oficial y completar manual de pruebas. | Alta |

## Rúbrica estimada provisional

Las cifras son conservadoras y no equivalen a una calificación oficial. La ausencia de QA integral limita su fiabilidad.

| Criterio | Máximo | Estimación | Evidencia | Pendiente |
|---|---:|---:|---|---|
| Arquitectura MVC | 25 | 20 | Controllers, Modelos, Views, Servicios y ContextoAplicacion separados. | Auditoría completa de dependencias y validaciones. |
| Lógica comercial y descuentos | 20 | 16 | ServicioDescuentos y pruebas de límites; ServicioPedidos aplica reglas configurables. | Concurrencia real y flujos completos pendientes. |
| Roles y seguridad | 20 | 13 | Identity, atributos de autorización y protección de administradores en ServicioUsuarios. | Pruebas HTTP de todos los roles, cargas, secretos y revisión integral. |
| Funcionalidad | 15 | 10 | Servicios y 64 pruebas aprobadas. | Falta QA integral de pagos, instituciones, PDF y notificaciones. |
| UX y responsive | 10 | 5 | Vistas y CSS administrativo presentes. | No se verificaron las resoluciones solicitadas ni todos los módulos. |
| Git, documentación y demostración | 10 | 6 | Rama y documentación existentes; SQL restaurado en base temporal. | Guía oficial no localizada; demo y sincronización final pendientes. |
| **Total** | **100** | **70** | | Estimación provisional, no representa porcentaje exacto de finalización. |

## Evidencia y límites

- Inicio: rama `feature/base-mvc`, commit `f0d87e5`, árbol limpio y alineado con la referencia local `origin/feature/base-mvc`; no se verificó el remoto mediante fetch en ese momento.
- `dotnet build`: correcto, cero advertencias y errores. `dotnet test`: 64 superadas, ninguna fallida. Las pruebas usan SQLite y no prueban por sí solas concurrencia SQL Server ni navegador.
- `base-datos/CloudBooks.sql` se ejecutó con `sqlcmd -f 65001` en una base temporal nueva de SQL Server Express, luego eliminada. Se comprobaron 13 migraciones, 10 libros, 5 usuarios, 5 asignaciones de rol, 24 claves foráneas, el nombre María López y parámetros iniciales 12 %, 5, 30, Q10,000 y 70 %. SQL Server advirtió sobre el tamaño máximo teórico de tres índices de Identity. No se verificaron todos los datos ni las credenciales con inicio de sesión.
- La autorización de tarjeta se realiza en `PedidosController`; `ServicioPedidos` usa transacción serializable, actualización de stock condicionada y conserva importes históricos. Los e-books se excluyen del decremento. No hay reserva de stock para pedidos en revisión.
- Las cancelaciones permitidas son únicamente de pedidos pendientes o en revisión; no se restituye stock porque todavía no se descontó. Desde esta revisión la autorización simulada pasa a anulada y se registra aviso local. Los pedidos aprobados no se cancelan automáticamente.
- Los avisos locales no son correos enviados. No existe integración de correo ni se ha verificado el envío externo; no se deben presentar como funcionalidad terminada.

## Siguientes pasos

1. QA HTTP y de navegador en base aislada con los cinco roles, especialmente pago, revisión, cancelación, instituciones y cotizaciones.
2. Pruebas de concurrencia reales en SQL Server, incluidas insuficiencia y reintento; revisar manejo de conflictos en aprobación administrativa.
3. Completar proveedor de correo configurable, registro de intentos rechazados y avisos institucionales/comerciales sin duplicar sistemas.
4. Revisar seguridad, diseño administrativo y responsive en las cuatro resoluciones solicitadas.
5. Contrastar guía oficial cuando esté disponible y actualizar diagnóstico, manual y guion con evidencia verificable.

## Avance posterior del 4 de noviembre

Se agregó el registro de intentos de pago no autorizados sin número ni CVV, la edición opcional del teléfono en Mi cuenta y la identificación explícita de simulación académica en el comprobante. El script SQL actualizado se restauró en una base temporal con 14 migraciones, 10 libros y 5 usuarios; se verificaron la tabla de intentos y una muestra con tildes y se eliminó la base. La prueba de servicio comprueba que un intento sin fondos no altera carrito, inventario ni pedidos. Sigue pendiente verificar estos cambios mediante HTTP y navegador.

Se añadió una cola persistente con proveedor simulado y proveedor Resend configurable, avisos administrativos y pruebas de servicio. Una prueba real anterior fue aceptada por Resend; la aceptación no demuestra entrega al buzón. La clave de idempotencia del proveedor dura 24 horas; la cola detiene los reintentos inciertos antes de superar esa ventana. Véase [correo transaccional](correo-transaccional.md).

## QA integral posterior

En SQL Server temporal se comprobaron dos compras simultáneas sobre 12 unidades, aprobación frente a cancelación y dos procesadores de correo. En TestServer se verificaron rutas seleccionadas de los cinco roles, antiforgery, compra desde visitante con fusión, rechazo por fondos, aprobación, cola y acceso al PDF propio y ajeno. Los resultados y límites reproducibles se detallan en [evidencias de QA](evidencias-qa-integral.md). La revisión de navegador, responsive y el resto de casos siguen pendientes. La rúbrica inicial sigue siendo provisional y no debe interpretarse como cierre del proyecto.

## Estado de la continuación de cierre

Esta clasificación expresa la evidencia disponible, no una certificación de entrega. «Parcial» incluye funcionalidad implementada cuyo QA o diseño todavía no cubre el alcance solicitado.

| Área | Estado | Evidencia y trabajo pendiente |
|---|---|---|
| Catálogo, carrito y compra | Parcial | Flujo HTTP de visitante, fondos insuficientes y compra aprobada verificado; faltan escenarios y revisión visual completa. |
| Pagos y pedidos | Parcial | Hay pruebas de simulador, pedidos y revisión; faltan recorridos HTTP de todas las tarjetas y transiciones. |
| Inventario y concurrencia | Parcial | Cinco pruebas SQL aisladas, incluidas doble aprobación y rollback del segundo producto; faltan e-books concurrentes y más entrelazados. |
| Instituciones y cotizaciones | Parcial | Recorrido HTTP de aprobación, asignación, emisión, aceptación, PDF privado y vencimiento; rechazo institucional probado aparte. Falta QA visual integral y rechazo de cotización en el recorrido HTTP. |
| Roles y seguridad | Parcial | Rutas seleccionadas de cinco roles, antiforgery y documentos propios/ajenos probados; no existe una matriz exhaustiva de endpoints. |
| Correo y Resend | Parcial | Cola y proveedor simulado probados; 13 vistas abiertas parcialmente y pedido mejorado. Faltan todos los anchos y clientes de correo reales. Sin nuevos envíos reales. |
| Administración y tienda | Parcial | Dashboard y navegación mejorados; solo se inspeccionaron algunas vistas y tamaños. Faltan rediseño y QA por módulo y responsive integral. |
| PDF | Parcial | Dos ejemplos de una página renderizados y legibles; falta probar documentos largos y paginación. |
| Base de datos | Completo y verificado para la restauración observada | SQL de entrega restaurado con 17 migraciones en base aislada; no se modificó el esquema en esta continuación. Persisten advertencias de longitud teórica de índices Identity. |
| Documentación y demo | Parcial | Guion y evidencias actualizados; falta ensayar la demo completa en navegador y revisar el resto de documentos. |

**Evaluación actual: NO LISTO PARA ENTREGA.** Las tareas visuales, responsive y de QA pendientes son parte de los criterios solicitados; no deben presentarse como completadas por las pruebas existentes.
