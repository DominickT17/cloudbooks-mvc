# Evidencias de diseño final

Capturas reales de Chromium/Playwright sobre CloudBooks en Development, una base aislada de QA y correo simulado. Las imágenes son de página completa en el viewport indicado; las tablas móviles muestran su posición horizontal inicial. Se enmascaró en gris el enlace de correo personal del pie y se sustituyeron los nombres de las cuentas administradoras en la base de prueba. Los demás nombres y contactos mostrados son datos académicos de demostración.

- Dashboard: [1440](dashboard-1440.png), [375](dashboard-375.png).
- Pedidos: [1440](pedidos-1440.png), [375](pedidos-375.png), [detalle 1440](pedido-detalle-1440.png).
- Inventario: [1440](inventario-1440.png), [375](inventario-375.png).
- Usuarios: [1440](usuarios-1440.png), [375](usuarios-375.png).
- [Instituciones 1440](instituciones-1440.png) y [cotizaciones 1440](cotizaciones-1440.png).
- Configuración: [1440](configuracion-1440.png), [375](configuracion-375.png).
- Checkout: [1440](checkout-1440.png), [375](checkout-375.png).
- Plantilla de pedido aprobado: [marco 600](correo-pedido-aprobado-600.png), [marco 375](correo-pedido-aprobado-375.png). Para capturar el correo completo se amplió la altura del iframe sin cambiar su ancho ni su contenido. Sin URL pública los botones a la aplicación no aparecen.

Se produjeron capturas temporales adicionales a 1024 y 768 px; no se guardaron en el repositorio. La [matriz de revisión](../../revision-diseno-final.md) distingue esas capturas y mediciones de la inspección visual y de las acciones aún pendientes. Una captura HTML no demuestra compatibilidad con Gmail u Outlook.

Para reproducir la revisión manual, restaura `base-datos/CloudBooks.sql` en una base dedicada, usa Development con `Correo:Proveedor=Simulado` y las [cuentas ficticias](../../credenciales-prueba.md). Recorre cada pantalla hasta el pie a los cuatro anchos; en tablas mueve el contenido horizontalmente hasta la última columna. No uses la base principal ni actives Resend para estas pruebas.
