# Estado del proyecto — diagnóstico inicial, 4 de noviembre de 2026

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

El correo real y la cola persistente siguen pendientes; consultar la documentación oficial de Resend antes de implementarlos. La clave de idempotencia del proveedor se conserva durante 24 horas y no garantiza por sí sola deduplicación indefinida. No hay envío real configurado o probado.
