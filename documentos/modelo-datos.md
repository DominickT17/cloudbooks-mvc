# Modelo de datos

```mermaid
erDiagram
    USUARIO ||--o{ SOLICITUD_INSTITUCIONAL : solicita
    USUARIO ||--o| PERFIL_INSTITUCIONAL : tiene
    USUARIO ||--o| CARRITO : posee
    CARRITO ||--o{ DETALLE_CARRITO : contiene
    LIBRO ||--o{ DETALLE_CARRITO : aparece
    CATEGORIA ||--o{ LIBRO : clasifica
    EDITORIAL ||--o{ LIBRO : publica
    AUTOR }o--o{ LIBRO : escribe
    USUARIO ||--o{ PEDIDO : realiza
    PEDIDO ||--|{ DETALLE_PEDIDO : contiene
    LIBRO ||--o{ DETALLE_PEDIDO : referencia
    USUARIO ||--o{ SOLICITUD_COTIZACION : solicita
    PERFIL_INSTITUCIONAL ||--o{ SOLICITUD_COTIZACION : identifica
    USUARIO o|--o{ SOLICITUD_COTIZACION : asesora
    SOLICITUD_COTIZACION ||--o| COTIZACION : origina
    COTIZACION ||--|{ DETALLE_COTIZACION : contiene
```

Identity relaciona usuarios y roles mediante sus propias tablas. Un índice filtrado impide más de una solicitud institucional pendiente por usuario; el perfil y el carrito son únicos por usuario. Los libros se desactivan lógicamente; los detalles de pedidos conservan título y precio históricos.

La solicitud comercial vincula cliente, perfil y asesor opcional. La interfaz emite una cotización con un concepto. La emisión y respuesta actualizan estados de solicitud y cotización en transacción. Los importes son `decimal`; la tasa aplicada y el vencimiento se conservan en cada operación. La fila `ConfiguracionSistema` contiene los valores vigentes para nuevas operaciones.
