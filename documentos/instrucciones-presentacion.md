# Instrucciones para la presentación de CloudBooks

## Antes de empezar

En este equipo se preparó la base SQL Server **`CloudBooks_Demo_Entrega_20261105`**. Contiene las cinco cuentas académicas, un pedido aprobado, uno en revisión por titular distinto, una solicitud comercial con asesor, una cotización y correos simulados. El libro «Ideas que construyen futuro» comenzó con 98 unidades después de la preparación. La cotización preparada vence el **20/11/2026**; si la exposición es posterior, crea una nueva desde la solicitud/asesor y comprueba su vigencia antes de mostrarla. Los datos son de demostración.

1. Inicia SQL Server Express desde Servicios de Windows; el servicio se llama `MSSQL$SQLEXPRESS`. En PowerShell puedes consultar `Get-Service -Name 'MSSQL$SQLEXPRESS'` y, si tienes permisos, iniciarlo con `Start-Service -Name 'MSSQL$SQLEXPRESS'`.
2. Abre PowerShell en el repositorio y ejecuta:

```powershell
cd C:\Users\domin\Documents\Arquitectura_Sistemas\cloudbooks-mvc
dotnet build
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__DefaultConnection = 'Server=.\SQLEXPRESS;Database=CloudBooks_Demo_Entrega_20261105;Trusted_Connection=True;TrustServerCertificate=True'
$env:Correo__Proveedor = 'Simulado'
$env:Correo__Administradores = 'admin@cloudbooks.local'
$env:ASPNETCORE_URLS = 'http://127.0.0.1:5199'
dotnet run --no-build --no-launch-profile --project src/CloudBooks.Web
```

3. Abre [CloudBooks local](http://127.0.0.1:5199/) y deja esa terminal abierta. Para detener la aplicación usa `Ctrl+C`. Confirma en Administración → Configuración que aparezca **Simulado · sin envío externo**. Las variables anteriores solo afectan a esa terminal y evitan utilizar la base principal y el proveedor real.

## Cuentas y tarjetas académicas

Todas las cuentas siguientes usan la contraseña ficticia `CloudBooksDemo2026!`. Son exclusivamente para Development; no las uses en producción.

| Rol | Usuario |
|---|---|
| Cliente minorista | `cliente@cloudbooks.local` |
| Cliente institucional | `institucion@cloudbooks.local` |
| Asesor | `asesor@cloudbooks.local` |
| Administradores | `admin.dominick@cloudbooks.local`, `admin.yeisson@cloudbooks.local` |

Tarjeta **aprobada**: `4000 0000 0000 0002`. Tarjeta **sin fondos**: `4000 0000 0000 0010`. Usa vencimiento futuro `12/35` y CVV ficticio `123`. Usa el nombre de la cuenta para aprobación automática; un titular diferente provoca revisión. No introduzcas datos bancarios reales. La compra y el pago son simulados; el comprobante **no es factura fiscal**.

## Recorrido

1. Como visitante busca «Ideas que construyen futuro», agrega seis unidades e inicia sesión como minorista. Comprueba el carrito, descuento e IVA; realiza una compra con la tarjeta aprobada y abre el pedido y su comprobante desde **Mi cuenta → Mis pedidos**.
2. Muestra el pedido en revisión preparado desde **Administración → Pedidos**. Con la cuenta administradora entra al panel desde **Mi cuenta → Administración**; abre el detalle, explica el motivo, el pago simulado y que la revisión no reserva stock. Consulta dashboard e inventario.
3. En **Administración → Avisos → Historial de correos**, enseña los estados simulados y abre **Ver plantillas de demostración**. No envíes correo real.
4. Accede como institucional a **Mis solicitudes** y **Mis cotizaciones**; abre la cotización preparada y descarga su PDF. Con la cuenta de asesor abre **Panel de asesor y solicitudes** para mostrar la asignación. Si deseas aceptar la cotización, hazlo después de mostrarla y ten en cuenta que el estado queda modificado.
5. Señala el enlace de WhatsApp de ayuda o el del detalle de un pedido con teléfono ficticio. El enlace prepara un mensaje; **no lo envíes**. Termina mostrando usuarios y configuración. El [guion con tiempos y texto](guion-demostracion-final.md) organiza la exposición.

La ejecución crea o modifica registros académicos. No hace falta abrir SQL para seguir la secuencia, pero sí se recomienda restaurar el estado preparado antes del ensayo o la presentación si ya se usó.

## Restablecer la base preparada en este equipo

Existe un respaldo SQL verificado en:

`C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\Backup\CloudBooks_Demo_20261105_7f31b8.bak`

**Este comando reemplaza únicamente `CloudBooks_Demo_Entrega_20261105` y descarta sus cambios posteriores.** Confirma que el nombre es exactamente ese y detén CloudBooks antes de ejecutarlo; no lo cambies por `CloudBooks` ni por otra base.

```powershell
sqlcmd -S '.\SQLEXPRESS' -E -b -Q "USE master; ALTER DATABASE [CloudBooks_Demo_Entrega_20261105] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE [CloudBooks_Demo_Entrega_20261105] FROM DISK=N'C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\Backup\CloudBooks_Demo_20261105_7f31b8.bak' WITH REPLACE, CHECKSUM, RECOVERY, MOVE N'CloudBooks_Demo_Entrega_20261105' TO N'C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\DATA\CloudBooks_Demo_Entrega_20261105.mdf', MOVE N'CloudBooks_Demo_Entrega_20261105_log' TO N'C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\DATA\CloudBooks_Demo_Entrega_20261105_log.ldf'; ALTER DATABASE [CloudBooks_Demo_Entrega_20261105] SET MULTI_USER;"
```

El respaldo pertenece a este equipo y no está en Git. En otro equipo, restaura primero `base-datos/CloudBooks.sql` en una base nueva de demostración con `sqlcmd -b -I -f 65001` y prepara desde la aplicación una compra aprobada, otra con titular distinto y una solicitud/cotización institucional. No restaures el SQL sobre una base existente con datos que deban conservarse.

La revisión de apariencia en Gmail/Outlook y el funcionamiento de botones hacia una URL pública quedan como verificaciones externas si se dispone de acceso; no se necesitan para recorrer la demostración local.
