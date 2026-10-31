# Guía de demostración — aproximadamente 10 minutos

Antes de comenzar, aplica las migraciones, inicia la aplicación en Development y comprueba las [cuentas académicas](credenciales-prueba.md). Los libros son ficticios. El dashboard puede iniciar sin ventas; no se insertan pedidos artificiales en la base principal.

1. **Presentación y MVC (1 min):** explica CloudBooks y el diagrama de [arquitectura](arquitectura.md).
2. **Catálogo (1 min):** busca por título o autor, combina un filtro y abre una ficha.
3. **Compra (2 min):** inicia sesión como cliente, agrega seis ejemplares de un libro con stock suficiente, muestra el descuento, el impuesto y confirma el pago simulado. Abre el pedido pendiente en Mis pedidos.
4. **Administración (2 min):** inicia sesión como administrador, muestra el pedido y su stock, apruébalo y comprueba el inventario. Abre el dashboard: la venta se cuenta al quedar aprobada. Muestra catálogo y configuración.
5. **Institucional y asesor (3 min):** inicia como cliente institucional, envía una solicitud especial con fecha futura; entra como asesor, abre la solicitud asignada, inicia revisión y emite una cotización. Vuelve al cliente institucional, muestra la propuesta y descarga el PDF.
6. **Cierre (1 min):** señala la separación MVC, roles, validación en servidor, control de stock y carácter simulado del pago.

Si se demuestra la aprobación de cuenta institucional, parte del cliente minorista y vuelve a iniciar sesión después de que un administrador apruebe. Si no hay asesor activo, un administrador puede reintentar la asignación desde Solicitudes comerciales. El stock usado en la demostración cambia realmente; usa una base de demostración dedicada.
