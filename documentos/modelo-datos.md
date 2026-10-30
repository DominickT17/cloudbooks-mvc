# Modelo de datos

`UsuarioAplicacion` se relaciona con solicitudes institucionales y, tras la aprobación, con un `PerfilInstitucional`. Un índice filtrado impide más de una solicitud pendiente por usuario; el perfil es único por usuario.

`SolicitudCotizacion` vincula cliente, perfil y asesor opcional. Una `Cotizacion` pertenece a una única solicitud y contiene uno o más `DetalleCotizacion`; la interfaz actual emite un solo concepto. Los importes se guardan como `decimal` y el identificador numérico produce códigos visibles como `SOL-000001` y `COT-000001`.

Los estados son Solicitada, EnRevision, Cotizada, Aceptada, Rechazada y Vencida. La emisión y la respuesta actualizan ambos registros de forma transaccional. Las fechas de emisión y vencimiento se evalúan en UTC.
