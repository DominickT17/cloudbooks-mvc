# Flujo institucional

1. El cliente minorista solicita la cuenta; un administrador revisa la solicitud y la aprueba o rechaza.
2. Tras la aprobación, el cliente vuelve a iniciar sesión y envía una solicitud especial desde Instituciones.
3. Se asigna un asesor activo por menor número de solicitudes Solicitadas, EnRevision o Cotizadas. Los bloqueos de Identity y la propiedad `Activo` se consideran al asignar. En empate se elige el menor identificador ordinal.
4. Sin asesores disponibles, la solicitud se guarda sin asignación. Un administrador puede usar **Asignar asesor disponible** en su detalle para volver a intentar cuando haya uno activo.
5. El asesor asignado inicia la revisión y emite la propuesta. El cliente puede consultar y descargar su PDF y aceptar o rechazar antes del vencimiento. Una cotización Cotizada se marca Vencida cuando se consulta o se intenta responder después de su fecha límite.

La propuesta utiliza el motor comercial compartido y tiene una vigencia de 15 días, con fecha UTC. El PDF se genera al solicitarlo; no se guardan archivos públicos.
