# Capturas pendientes — diseño final

La revisión del 4 y 5 de noviembre de 2026 se realizó en el navegador integrado con datos de una base SQL temporal y correo simulado. La herramienta permitió observar capturas en la sesión, pero en esta ejecución no ofreció una operación documentada para guardarlas directamente en esta carpeta. Por ello **no se adjuntan imágenes** ni se consideran completadas las evidencias visuales de entrega. Los resultados y su alcance están en [revisión de diseño](../../revision-diseno-final.md).

## Capturas manuales por guardar

Usa una instancia Development con una base exclusiva de demostración y proveedor `Simulado`. Inicia sesión con las cuentas descritas en [credenciales de prueba](../../credenciales-prueba.md). No actives Resend ni uses la base principal. Ajusta el viewport del navegador y guarda capturas reales, sin recortar información necesaria para entender la pantalla:

| Archivo sugerido | Vista | Ancho |
|---|---|---:|
| `dashboard-1440.png`, `dashboard-375.png` | Administración → Dashboard | 1440 y 375 px |
| `pedidos-1440.png`, `pedidos-375.png` | Administración → Pedidos | 1440 y 375 px |
| `pedido-detalle-1440.png` | Detalle de un pedido de demostración | 1440 px |
| `inventario-1440.png`, `inventario-375.png` | Administración → Inventario | 1440 y 375 px |
| `usuarios-1440.png`, `usuarios-375.png` | Administración → Usuarios | 1440 y 375 px |
| `instituciones-1440.png`, `cotizaciones-1440.png` | Listados correspondientes | 1440 px |
| `configuracion-1440.png`, `configuracion-375.png` | Administración → Configuración | 1440 y 375 px |
| `checkout-1440.png`, `checkout-375.png` | Carrito de demostración → Finalizar compra | 1440 y 375 px |
| `correos-600.png`, `correos-375.png` | Avisos → Historial de correos → Plantillas de demostración | Marco de 600 y 375 px |

En 1024 y 768 px revisa también las vistas listadas en la matriz. En tablas, utiliza el desplazamiento horizontal interno para comprobar las acciones de la última columna. Para las plantillas, inspecciona además los pies, texto largo y botones con una URL pública real configurada y controlada; la vista sin URL omite esos botones. Una captura de navegador no demuestra compatibilidad con Gmail u Outlook.
