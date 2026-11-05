namespace CloudBooks.Web.Servicios;

public record EjemploCorreo(string Nombre, string Asunto, string Html, string Texto);

public static class EjemplosCorreo
{
    public static IReadOnlyList<EjemploCorreo> Crear(string? urlBase, string? urlLogo)
    {
        var ejemplos = new[]
        {
            ("Pedido aprobado", "pedido/42/aprobado/cliente", "aprobado", "CloudBooks | Tu pedido PED-000042 fue confirmado", "Hola, María López:\n\nGracias por comprar en CloudBooks. Tu pedido PED-000042 fue aprobado y el pago es simulado.\nFecha: 04/11/2026 10:30 UTC\n\nProductos:\n2 × La biblioteca del viento\n1 × Código y pensamiento\n\nSubtotal: Q449.00\nDescuento por volumen: Q0.00\nIVA: Q53.88\nTOTAL: Q502.88"),
            ("Pedido en revisión", "pedido/43/revision/cliente", "revision", "CloudBooks | Estamos revisando tu pedido PED-000043", "Hola, María López:\n\nRecibimos tu pedido PED-000043. Nuestro equipo está revisando la compra; no necesitas realizarla nuevamente. No se han reservado existencias."),
            ("Pago rechazado", "intento/8/rechazado", "pago_rechazado", "CloudBooks | No pudimos procesar tu pago", "Hola, María López:\n\nEl pago simulado no fue procesado: fondos insuficientes. Tu carrito sigue disponible. No hubo ningún cargo bancario real."),
            ("Pedido cancelado", "pedido/44/cancelado/cliente", "cancelado", "CloudBooks | Actualización de tu pedido PED-000044", "Hola, María López:\n\nTu pedido PED-000044 fue cancelado durante la revisión. Si existía una autorización simulada, fue anulada. No se realizó ningún cargo bancario real."),
            ("Institución recibida", "institucion/5/recibida/cliente", "institucion_recibida", "CloudBooks | Solicitud institucional recibida", "Hola, Carlos Méndez:\n\nRecibimos tu solicitud institucional y será revisada."),
            ("Institución aprobada", "institucion/5/resuelta/cliente", "institucion_aprobada", "CloudBooks | Actualización de solicitud institucional", "Hola, Carlos Méndez:\n\nTu solicitud institucional fue aprobada. Inicia sesión nuevamente para actualizar tus permisos."),
            ("Institución rechazada", "institucion/5/resuelta/cliente", "institucion_rechazada", "CloudBooks | Actualización de solicitud institucional", "Hola, Carlos Méndez:\n\nTu solicitud institucional fue rechazada. Puedes contactar a CloudBooks para obtener asistencia."),
            ("Solicitud comercial", "solicitud/6/creada/cliente", "solicitud_comercial", "CloudBooks | Solicitud comercial recibida", "Hola, Carlos Méndez:\n\nRecibimos tu solicitud SOL-000006. Nuestro equipo dará seguimiento a tu gestión."),
            ("Asesor asignado", "solicitud/6/asignada/asesor", "solicitud_asignada", "CloudBooks | Solicitud comercial asignada", "Hola:\n\nSe te asignó la solicitud SOL-000006 para una edición especial."),
            ("Cotización emitida", "cotizacion/7/emitida/cliente", "cotizacion_emitida", "CloudBooks | Cotización COT-000007", "Hola, Carlos Méndez:\n\nTu cotización está disponible en tu cuenta.\nInstitución: Biblioteca Horizonte\nCotización: COT-000007\nFecha: 04/11/2026 UTC\nVigencia hasta: 19/11/2026\nEstado: Emitida\nTotal: Q2,318.40\n\nPuedes consultar el detalle y descargar el PDF iniciando sesión."),
            ("Cotización aceptada", "cotizacion/7/respuesta/admin", "cotizacion_aceptada", "CloudBooks | Respuesta a cotización", "La cotización COT-000007 fue aceptada por el cliente."),
            ("Cotización rechazada", "cotizacion/7/respuesta/admin", "cotizacion_rechazada", "CloudBooks | Respuesta a cotización", "La cotización COT-000007 fue rechazada por el cliente."),
            ("Aviso administrativo", "pedido/43/revision/admin", "revision", "CloudBooks Administración | Pedido PED-000043 requiere revisión", "Pedido: PED-000043\nCliente: María López\nTotal: Q502.88\nRevisión: Inventario\nPrioridad: Normal\nRevisar disponibilidad antes de continuar.")
        };
        return ejemplos.Select(e => { var resultado = PlantillasCorreo.Crear(e.Item2, e.Item3, e.Item4, e.Item5, urlBase, urlLogo); return new EjemploCorreo(e.Item1, e.Item4, resultado.Html, resultado.Texto); }).ToList();
    }
}
