# Demostración interactiva CloudBooks · CITEIN 2026

## Montaje y requisitos

Un único equipo servidor ejecuta ASP.NET Core MVC (.NET 10) y SQL Server Express local. La tablet, el equipo administrativo y la segunda computadora se conectan a esa misma instancia mediante Wi-Fi **privado**; no hace falta internet ni acceso remoto a SQL Server. Todos los pedidos, usuarios e inventario de la feria viven en `CloudBooks_CITEIN_2026`. El servidor necesita `dotnet`, `sqlcmd`, el servicio `MSSQL$SQLEXPRESS` y el repositorio con sus dependencias restauradas. Las portadas están incluidas localmente.

La experiencia de la tablet usa un layout propio. Al comenzar se crea una identidad Identity sin contraseña con un alias y una cookie independientes de la cookie administrativa. La identidad tiene exclusivamente el rol `ClienteMinorista`, y cada nueva demostración crea otra identidad. Las sesiones se limitan a 12 inicios por IP por minuto y 5.000 identidades académicas por base; las demás rutas tienen un límite de 120 solicitudes por IP por minuto. La compra utiliza el carrito, cálculo comercial, simulador de tarjeta y transacción de pedidos existentes. Si un pedido requiere revisión, no se descuenta el inventario hasta su aprobación administrativa. El monitor consulta el total, el pedido más reciente y los últimos cinco pedidos académicos cada cuatro segundos mientras está visible, sin recargar la página; solo un administrador puede acceder. El panel normal sigue disponible. Una identidad académica registra como máximo un pedido: las solicitudes de compra y de comprobación se serializan en la única instancia del servidor. Ante un resultado incierto, la tablet consulta el pedido antes de permitir otro intento; una recarga conserva la comprobación pendiente si el navegador permite almacenamiento de sesión.

La activación exige simultáneamente `CiteinDemo:Enabled=true`, entorno `Development`, servidor SQL local, base con nombre exacto `CloudBooks_CITEIN_2026` y una modalidad de correo válida; el valor predeterminado es `Simulado` y Resend exige una activación y un destinatario explícitos por ejecución. Si la combinación no se cumple, el arranque falla. Si la función está desactivada, `/citein` y sus subrutas responden 404. Este es un entorno académico: `Development` crea cuentas de prueba con credenciales conocidas documentadas en [credenciales-prueba.md](credenciales-prueba.md). **No exponer la aplicación a internet ni usar credenciales de producción.** Los visitantes no necesitan conocer esas cuentas; el expositor debe proteger la red y cerrar sesión administrativa al terminar.

## Preparación de red

1. Conecta los tres dispositivos a la misma red Wi-Fi privada, sin aislamiento entre clientes. En Windows, ejecuta `ipconfig` e identifica la IPv4 del adaptador Wi-Fi; puede cambiar entre eventos. El script también muestra direcciones detectadas con puerta de enlace.
2. Si el firewall bloquea la conexión, un administrador de Windows puede crear una regla de entrada TCP para el puerto 5199, **solo para el perfil Privado y el rango de direcciones de la red de la feria**. En Seguridad de Windows → Firewall → Configuración avanzada → Reglas de entrada → Nueva regla: Puerto → TCP 5199 → Permitir → Privado; en Propiedades → Ámbito limita las direcciones remotas al rango real. No habilites el perfil Público ni abras SQL Server. Retira o deshabilita la regla al finalizar. Si no controlas la red, solicita ayuda al responsable del equipo.
3. La dirección HTTP sin cifrado es solo para esta red aislada. No introduzcas información personal ni credenciales de producción. Comprueba que el perfil de la red es Privado y que el router no tiene redirección de puertos hacia el servidor.

## Inicio y detención

En PowerShell, desde la raíz del repositorio:

```powershell
.\scripts\iniciar-demo-citein.ps1
# Puerto alternativo únicamente si es necesario:
.\scripts\iniciar-demo-citein.ps1 -Puerto 5200
```

El script comprueba el proyecto, SQL Express, cada migración del proyecto, los seis ISBN activos y sus formatos, avisa si algún libro físico tiene menos de 20 ejemplares y revisa el puerto. Tras el inicio comprueba la respuesta HTTP local. Configura la conexión y, por defecto, el correo simulado en el proceso; al terminar restaura las variables anteriores. No cambia configuraciones privadas ni cierra otros procesos. Copia la IP mostrada y usa:

- Tablet: `http://IP-MOSTRADA:5199/citein`.
- Administración y sistema completo: `http://IP-MOSTRADA:5199/`.
- Monitor: `http://IP-MOSTRADA:5199/citein/monitor`.

En Administración inicia sesión con una de las cuentas académicas administradoras de [credenciales-prueba.md](credenciales-prueba.md). En otra computadora abre la aplicación completa. Comprueba que el código y total del resultado de la tablet coinciden con el monitor y con Administración → Pedidos; el libro muestra su existencia actual. Si la actualización automática falla, abre el panel normal de pedidos. Mantén la ventana PowerShell abierta y pulsa **Ctrl+C** para detener el servidor; SQL Server se mantiene activo.

## Correo transaccional y prueba controlada con Resend

La cola y las plantillas existentes registran los eventos de aprobación, revisión y cancelación. Con el comando de inicio sin opciones, **Simulado** sigue siendo el predeterminado y no necesita internet. Los pedidos de la tablet usan identidades `@visitante.citein.invalid`: nunca se envían a Resend ni se pide el correo personal al jurado. Las direcciones `.invalid` y `.local` tampoco salen mediante el proveedor real.

### Preparar configuración privada

1. En [Domains de Resend](https://resend.com/domains), añade un dominio propio controlado por el equipo, publica los registros DNS que indique Resend y espera a que figure como verificado. El remitente debe usar exactamente ese dominio. `onboarding@resend.dev` no sirve para esta modalidad CITEIN. En [API Keys](https://resend.com/api-keys), comprueba que la clave existente tenga permiso de envío para ese dominio. La aplicación verifica que el dominio declarado coincida con el remitente; **Resend verifica la autorización real del dominio y de la clave** al recibir la solicitud.
2. La clave `Resend:ApiKey` se almacena en User Secrets de este proyecto, fuera de Git. No la imprimas ni pegues en el repositorio. Para comprobar únicamente si existen las entradas, desde la raíz ejecuta:

   ```powershell
   $rutaSecretos = Join-Path $env:APPDATA 'Microsoft\UserSecrets\c9ab3ad4-5ea3-40f1-9e53-3d1b4174ea0f\secrets.json'
   if (Test-Path -LiteralPath $rutaSecretos) {
       $datosSecretos = Get-Content -LiteralPath $rutaSecretos -Raw | ConvertFrom-Json
       foreach ($nombre in @('Resend:ApiKey', 'Correo:ApiKey', 'Correo:Remitente', 'Correo:DominioVerificado')) {
           $valor = $datosSecretos.PSObject.Properties[$nombre].Value
           '{0}: {1}' -f $nombre, $(if ([string]::IsNullOrWhiteSpace($valor)) { 'ausente' } else { 'configurado' })
       }
       Remove-Variable datosSecretos, valor -ErrorAction SilentlyContinue
   } else { 'Archivo de User Secrets ausente' }
   ```

   Si ya existe la clave, consérvala. Si falta, el responsable puede agregar `Resend:ApiKey` a los User Secrets del proyecto mediante un editor privado, sin pegarla en la consola ni compartirla. `Correo:ApiKey` es una alternativa anterior; si ambas existen, prevalece `Resend:ApiKey`.
3. Configura solo los valores reales autorizados del remitente y dominio propio; sustituye ambos marcadores antes de ejecutar. No incluyas la clave en estos comandos:

   ```powershell
   dotnet user-secrets set 'Correo:Remitente' 'CloudBooks <notificaciones@TU-DOMINIO-VERIFICADO>' --project src/CloudBooks.Web
   dotnet user-secrets set 'Correo:DominioVerificado' 'TU-DOMINIO-VERIFICADO' --project src/CloudBooks.Web
   ```

   El logo oficial se adjunta de forma interna al correo nuevo; no exige una URL pública. Los botones de la aplicación solo se incluyen si `Correo:UrlBase` es una URL HTTPS pública configurada y controlada por el equipo. Sin ella se omiten; no se usan direcciones de la red privada. No publiques CITEIN para habilitar los botones.
4. La versión añade las migraciones `ComprobanteCorreo` y `LogoCorreo`, con columnas opcionales para conservar los archivos exactos usados por los reintentos. Antes de iniciar, confirma que se aplicaron **exclusivamente en la base CITEIN**. Si el script indica que faltan, haz antes un respaldo verificado de esa base, detén la aplicación y, desde la raíz, ejecuta:

   ```powershell
   dotnet ef database update --project src/CloudBooks.Web --connection 'Server=.\SQLEXPRESS;Database=CloudBooks_CITEIN_2026;Trusted_Connection=True;TrustServerCertificate=True'
   ```

   Nunca cambies el nombre de esa base por `CloudBooks` u otra base principal. Las migraciones agregan columnas y conservan los registros anteriores.

### Autorizar y realizar una compra de prueba

1. Pide autorización del dueño de **un** buzón real y verifica su dirección. Esta versión admite direcciones válidas de otros proveedores, no solo Gmail. Para una prueba real, muestra al operador únicamente el destinatario y el asunto previsto `CloudBooks | Tu pedido PED-NNNNNN fue confirmado` (el código se asigna al crear el pedido); solicita su confirmación antes de comprar. No autorices otras direcciones ni envíes mensajes históricos.
2. Con internet disponible para Resend y la red local privada para CloudBooks, inicia reemplazando el marcador por esa única dirección autorizada:

   ```powershell
   .\scripts\iniciar-demo-citein.ps1 -CorreoReal -DestinatarioAutorizado 'BUZON-AUTORIZADO@gmail.com'
   ```

   Sin `-CorreoReal`, se usa Simulado aunque exista una clave guardada. Cada inicio de Resend genera una nueva autorización de sesión: solo los nuevos eventos de pedido para el destinatario indicado pueden enviarse en esa ejecución. Las demás notificaciones nuevas se simulan, incluidos los avisos administrativos. Los mensajes históricos pendientes y los de sesiones previas no se seleccionan para envío real ni se borran. Los mensajes ya simulados nunca se reenvían automáticamente. La autorización y el proveedor se restauran en el proceso al cerrar el script.
3. En el equipo servidor abre `http://127.0.0.1:5199/`, ingresa con el cliente registrado cuyo correo coincide exactamente con el autorizado o regístralo con su consentimiento. Mantén esta prueba en el equipo y la red privada de confianza: el sitio de la feria usa HTTP y no debe exponerse a internet. Selecciona un libro disponible, agrega una unidad al carrito y confirma con **transferencia simulada**. No realices transferencias bancarias. El pedido modifica la base e inventario CITEIN; conserva el código. Si entra en revisión, el correo indica revisión y no lleva comprobante. Tras la revisión y aprobación administrativa se crea otro evento de aprobación con el PDF; al cancelar uno pendiente o en revisión se crea el evento de cancelación sin PDF.
4. Abre **Administración → Avisos → Historial de correos**, busca el código y abre el detalle. `Pendiente` indica que se guardó el mensaje; `Simulado`, que no se envió; `AceptadoProveedor`, que Resend aceptó la solicitud; `ReintentoPendiente` indica un fallo temporal o resultado incierto y `Fallido`, que no se pudo confirmar después de los intentos. El identificador del proveedor aparece en el detalle. CloudBooks no implementa webhooks y no confirma entrega al buzón: comprueba el estado en Resend y pregunta al titular del Gmail si lo recibió, incluyendo spam. La vista previa del historial no reproduce necesariamente el logo interno CID.
5. Si se generó el comprobante para un pedido aprobado, abre el PDF adjunto en el mensaje recibido y contrasta código, cliente, libros, importes y aviso académico. La generación fallida queda en el registro del servidor; el pedido y el correo siguen su curso sin adjunto. Los archivos del correo se guardan junto a la cola. Una aceptación por Resend no prueba que el adjunto haya llegado al buzón ni su apariencia en Gmail.

La cola solo trabaja mientras la aplicación está abierta. Los fallos se reintentan con la misma clave y contenido durante la sesión, con un máximo de cinco intentos y una ventana de seguridad de 23 horas. Al reiniciar, una sesión nueva **no recupera automáticamente los pendientes reales de la sesión anterior**; investiga su estado antes de decidir cómo proceder, pues podría existir una aceptación cuyo resultado se perdió. No vuelvas a comprar para forzar un correo. Si no hay internet, detén y vuelve a iniciar sin `-CorreoReal`: Simulado conserva el funcionamiento de las compras sin enviar mensajes. Un fallo del proveedor no revierte ni duplica el pedido confirmado.

## Recorrido y comprobaciones previas

1. Tablet: comenzar, elegir uno de los tres destacados o pulsar **Explorar todos los libros**, revisar subtotal, descuento, IVA y total, y pulsar **Confirmar compra simulada**.
2. Comprueba el código y estado reales. Si está en revisión, explícalo: todavía no se han descontado existencias. Si falla, lee el mensaje y vuelve a elegir un libro.
3. En el monitor observa el pedido, abre su detalle original y verifica el inventario. El botón **Preparar siguiente demostración** cierra únicamente la sesión académica de la tablet y vuelve a la bienvenida; el siguiente visitante inicia una identidad nueva y los pedidos anteriores se conservan.
4. Antes del evento, prueba al menos dos compras en una red privada, acceso administrativo, actualización del monitor, existencias y legibilidad táctil en ambas orientaciones. Prueba también en la tablet física; la verificación automatizada no sustituye esta prueba presencial. Las capturas de navegador están en `documentos/evidencias/citein-2026`.

## Base, respaldo y restablecimiento

La base CITEIN se creó por separado mediante las migraciones del repositorio y datos académicos de desarrollo. El catálogo de feria incorpora cuatro libros físicos con 200 unidades iniciales y dos ebooks sin inventario físico; los pedidos de las pruebas permanecen en ella. La base principal `CloudBooks`, `CloudBooks_Demo_Entrega_20261105` y sus respaldos no forman parte del procedimiento. La contraseña académica y los datos de tarjeta ficticia ya presentes en el código no son credenciales bancarias reales; no agregues secretos ni datos reales.

### Respaldar y verificar la base actual

El respaldo conserva todos los pedidos existentes; no es un estado limpio. En SQL Server Management Studio, conéctate a `.\SQLEXPRESS` y confirma que la base seleccionada sea exactamente `CloudBooks_CITEIN_2026`. En **Tareas → Copia de seguridad**, selecciona una copia completa de solo copia y una ruta nueva fuera del repositorio donde la cuenta del servicio SQL Server tenga permiso de escritura. Usa un nombre que incluya fecha y hora y comprueba que no exista antes de iniciar. Activa la suma de comprobación del medio si está disponible; no sobrescribas respaldos existentes.

Comprueba en las propiedades del archivo que la base de origen sea `CloudBooks_CITEIN_2026` y ejecuta la opción **Comprobar copia de seguridad** de SSMS, o `RESTORE VERIFYONLY` sobre ese archivo. Confirma que la verificación termine correctamente y registra la ruta y la fecha. Esta comprobación verifica la legibilidad del respaldo; no sustituye una restauración de ensayo en un entorno aislado.

### Recuperación y estado limpio

Para recuperar un **estado inicial limpio**, acuerda primero con el equipo qué pedidos de CITEIN se perderán. Detén la aplicación y confirma en SQL Server Management Studio que el destino es exactamente `CloudBooks_CITEIN_2026`; verifica además el origen y la fecha de un respaldo limpio creado antes de la feria. Haz un respaldo adicional de la base actual en una ruta aprobada y verifica que se pueda leer. Solo entonces restaura manualmente el respaldo limpio sobre esa base concreta con las opciones y rutas de archivos revisadas en SSMS. Nunca selecciones `CloudBooks`, `CloudBooks_Demo_Entrega_20261105` ni otro respaldo como destino. La restauración elimina los cambios posteriores al respaldo; requiere autorización explícita del responsable. No hay restauración ni eliminación automáticas. Si aún no existe un respaldo limpio, prepara y verifica uno después de que el responsable decida cómo conservar o retirar los pedidos de prueba; el respaldo actual con pedidos no representa un estado limpio.

Para reponer existencias sin borrar pedidos, un administrador puede editar el stock desde Administración → Inventario, tras comprobar que la aplicación apunta a la base CITEIN. Registra cualquier ajuste en la preparación de la feria. No modifiques directamente otras bases.

## Catálogo académico y procedencia

Los importes son **precios simulados en quetzales**, elegidos para el ejercicio académico; no son precios de las editoriales. Los ebooks de OpenStax pueden consultarse gratuitamente en sus sitios oficiales. La compra simulada no proporciona licencias, archivos ni descargas. Las existencias indicadas son las iniciales: pueden disminuir conforme se aprueban pedidos.

| ISBN de la edición | Libro y autores | Editorial, edición y año | Formato | Precio demo | Stock inicial |
|---|---|---|---|---:|---:|
| 9786075502090 | Álgebra — Aurelio Baldor | Grupo Editorial Patria, 4.ª, 2019 | Tapa dura | Q165.00 | 200 |
| 9786075502076 | Aritmética — Aurelio Baldor | Grupo Editorial Patria, 4.ª, 2019 | Tapa dura | Q150.00 | 200 |
| 9786075502069 | Geometría y trigonometría — Aurelio Baldor | Grupo Editorial Patria, 4.ª, 2019 | Tapa dura | Q175.00 | 200 |
| 9786073221245 | Física universitaria, volumen 1 — Hugh D. Young y Roger A. Freedman | Pearson Educación, 13.ª, 2013 | Tapa blanda | Q235.00 | 200 |
| 9781951693510 | Cálculo volumen 1 — Gilbert Strang y Edwin “Jed” Herman | OpenStax, edición digital en español, 2022 | Digital | Q35.00 | No aplica (0) |
| 9781951693909 | Física universitaria volumen 1 — Samuel J. Ling, Jeff Sanny y William Moebs | OpenStax, edición digital en español, 2021 | Digital | Q35.00 | No aplica (0) |

**Fuentes bibliográficas consultadas:**

- [Grupo Editorial Patria, serie Baldor](https://editorialpatria.com.mx/wp-content/uploads/2024/07/Serie-Baldor.pdf): títulos, autor, 4.ª edición e ISBN; se adopta 2019 para la serie según esa ficha. Otra [ficha de recursos académicos](https://recursosacademicos.com/wp-content/uploads/2024/10/Catalogo_General_Bachillerato_compressed.pdf) identifica la encuadernación y consigna 2020 para Geometría; hay discrepancia de año entre fuentes.
- [Catálogo de Pearson, Física universitaria volumen I](https://pearson.es/docs/default-source/espa%C3%B1afiles/cat%C3%A1logo-universidad-2015-low1fb56a8b436366b1aea8ff00004a2a88.pdf): edición 13, ISBN físico y año. La [biblioteca UPSE](https://bibliotecas.upse.edu.ec/cgi-bin/koha/opac-detail.pl?biblionumber=18030) confirma Young y Freedman como autores, y la [ficha comercial](https://www.buscalibre.com/int-es/libro-fisica-universitaria-volumen-01/9786073221245/p/32247506) especifica tapa blanda.
- [OpenStax, Cálculo volumen 1](https://openstax.org/books/c%C3%A1lculo-volumen-1/pages/prefacio) y [ficha legal del PDF](https://assets.openstax.org/oscms-prodcms/media/documents/Calculo_volumen_1_-_WEB_vGHB4xK.pdf): autores, 2022 e ISBN digital. Texto bajo CC BY-NC-SA 4.0 según la ficha vigente.
- [OpenStax, Física universitaria volumen 1](https://openstax.org/books/f%C3%ADsica-universitaria-volumen-1/pages/indice) y [ficha legal del PDF](https://assets.openstax.org/oscms-prodcms/media/documents/FisicaUniversitariaVolumen1-WEB.pdf): autores, 2021 e ISBN digital. El PDF de 2021 indica CC BY 4.0, mientras el pie actual de la web indica CC BY-NC-SA; consulta las condiciones específicas vigentes antes de reutilizar contenido.

**Imágenes y permisos:** se reutiliza el logo oficial de CloudBooks incluido en el repositorio. Las seis cubiertas de `wwwroot/imagenes/citein` son SVG originales, ilustrativos y locales: no reproducen las cubiertas de Patria, Pearson ni OpenStax. No se encontró autorización suficiente para incorporar portadas editoriales; la ficha de OpenStax excluye expresamente sus portadas y logotipos de la licencia del contenido. No se distribuye texto, imágenes, PDF ni marcas gráficas de OpenStax. El enlace de referencia es opcional y requiere internet; la aplicación y sus imágenes no lo requieren.

### Preparar o actualizar la selección

Con SQL Express local en ejecución y después de comprobar el destino, desde la raíz del repositorio:

```powershell
sqlcmd -S '.\SQLEXPRESS' -d CloudBooks_CITEIN_2026 -E -C -b -f 65001 -i scripts\preparar-catalogo-citein.sql
```

El script comprueba el nombre exacto de la base, opera en una transacción e inserta solo los ISBN ausentes y sus autores. Una segunda ejecución no duplica obras ni repone el stock; una coincidencia incompatible detiene la operación. No modifica pedidos ni productos existentes. Los seis ISBN permitidos y su orden están declarados en `CiteinController`; para cambiar la selección, verificar primero datos y derechos, actualizar la lista explícita, preparar los registros sin sobrescribir históricos, y ejecutar las pruebas de selección, compra e inventario. El inventario se ajusta desde Administración en la base CITEIN.

Para comprobar un ebook, elige uno marcado **Libro digital** y revisa que el monitor muestre «No aplica · libro digital» en existencias físicas. El stock SQL permanece en cero. Sus textos se consultan en los enlaces oficiales anteriores cuando haya conexión; la feria no incluye una copia descargable.

## Fallos habituales

- **Puerto ocupado:** el script se detiene. Identifica el proceso con `netstat -ano | findstr :5199`; no termines procesos ajenos. Usa `-Puerto 5200` y cambia las URL y la regla privada si procede.
- **No llega desde la tablet:** confirma IP, Wi-Fi, aislamiento de clientes y regla privada del firewall; prueba la URL desde el propio servidor. La IPv4 puede cambiar.
- **SQL o base no disponible:** inicia `MSSQL$SQLEXPRESS` y verifica que exista `CloudBooks_CITEIN_2026` con sus migraciones. El script no crea ni restaura bases.
- **Faltan libros o stock:** verifica inventario y portadas en Administración. No cambies la base principal.
- **Monitor solicita acceso:** inicia sesión con un administrador en el navegador del monitor. La cookie de la tablet no concede acceso administrativo.
- **Pedido en revisión:** muestra el estado real; utiliza el proceso normal de revisión administrativa. No repitas la compra suponiendo que falló.
- **Límite de sesiones o de solicitudes:** espera un minuto si se limita la IP; si se alcanza el límite de identidades, prepara un respaldo y acuerda un reinicio seguro del entorno.

## Monitor de presentación y recuperación de conexión

Abre `/citein/monitor` con la cuenta de Administrador. La cabecera muestra el total real de pedidos CITEIN, el código más reciente, el estado de conexión y la hora guatemalteca de la última consulta exitosa. El historial muestra los cinco pedidos más recientes de visitantes CITEIN; no incluye clientes normales. El botón **Modo presentación** amplía la vista y solicita pantalla completa si el navegador lo permite; **Salir de presentación** o la tecla Escape permiten regresar. La consulta se pausa en una pestaña oculta.

Cuando falla una consulta, el monitor muestra **Reconectando** y, ante otro fallo, **Sin conexión con el servidor**, conservando los últimos datos y su hora. Al recuperarse la conexión vuelve a consultar. No interpreta un historial vacío como desconexión. Para un libro físico muestra la cantidad comprada y las existencias actuales; no inventa un valor anterior. Un ebook indica que no descuenta inventario físico. Un pedido en revisión se presenta como pendiente y aclara que el inventario físico aún no se ha descontado.

Si la tablet pierde la respuesta de la compra, deja la selección en pantalla y solicita comprobar el estado. No pulses repetidamente ni reinicies la aplicación: recupera la red y usa **Comprobar estado de la compra**. Si ya existe el pedido, se abre su resultado; si el servidor confirma que no existe, se habilita otro intento. Si continúa sin conexión, permanece bloqueado hasta poder comprobarlo. El servidor también impide crear un segundo pedido para la misma identidad académica. Si se recarga la página, se reanuda la comprobación pendiente cuando el almacenamiento de sesión está disponible.

### Pruebas de preparación

`dotnet test CloudBooks.sln --no-restore` ejecuta la suite habitual. Para verificar dos clientes y el doble envío contra la demostración local, con SQL Express, la aplicación y el puerto 5199 activos, usa `py -3 pruebas\probar-citein-http.py --ejecutar`. **La prueba crea dos pedidos reales de demostración y descuenta una unidad física**; no los elimina. No la ejecutes durante la exposición y verifica que el libro tenga más de 20 unidades. Las capturas se guardaron en `documentos/evidencias/citein-2026/pulido-final`.

### Lista del día de CITEIN

- Confirmar energía, SQL Express y la red privada sin aislamiento de clientes; ejecutar el script y leer sus diagnósticos.
- Comprobar en tablet la IP mostrada, los tres destacados y el catálogo completo; abrir Administración y el monitor con la cuenta académica.
- Ejecutar una compra de ensayo y contrastar código, importe, estado y stock en ambas pantallas.
- Ensayar la reconexión y verificar que el monitor vuelva a **Conectado**. Confirmar un respaldo verificado y espacio suficiente de inventario.
- Verificar presencialmente la tablet física y el acceso entre los tres equipos. Falta realizar una prueba con internet externo realmente bloqueado manteniendo la red local; no se cambiaron reglas de firewall para simularla.

## Guion de aproximadamente dos minutos

«CloudBooks es una librería digital construida con ASP.NET Core MVC y SQL Server. Para esta feria preparamos una experiencia simplificada para tablet, pero estamos usando la misma aplicación y las mismas reglas comerciales que ven los administradores.

Aquí el visitante comienza sin registrar datos personales. Selecciona un libro que proviene del catálogo real de esta base académica. El sistema calcula el subtotal, los descuentos aplicables y el IVA desde el servidor. El pago es exclusivamente simulado; no pedimos datos bancarios.

Al confirmar, el servicio valida nuevamente las existencias y registra el pedido dentro de una transacción. Esta pantalla muestra su código, total y estado reales. Si necesita revisión, no lo presentamos como aprobado.

En la computadora administrativa el monitor detecta el nuevo pedido en pocos segundos. Abrimos el detalle original y contrastamos el código, el total y el inventario. Esta conexión entre la tablet y Administración utiliza una sola instancia y una sola base de demostración. En la segunda computadora podemos mostrar el catálogo completo, los módulos institucionales, las cotizaciones y cómo se organiza el proyecto MVC.

Finalmente, podemos iniciar otra demostración; el pedido anterior permanece registrado y cada visitante recibe una sesión académica independiente».

## Límites conocidos

Se realizaron pruebas con clientes independientes en el mismo servidor, no con una tablet física. El montaje y la interacción táctil real, la conectividad entre equipos y la regla de firewall deben verificarse presencialmente. La cuenta académica y los pedidos de prueba persisten hasta un restablecimiento explícito. El límite de IP usa la dirección observada directamente: no se ha diseñado para operar detrás de un proxy público. El monitor presenta hasta cinco pedidos CITEIN y el total de esos pedidos; el historial completo permanece en Administración. La sincronización por identidad se ha diseñado para una sola instancia del servidor, como en el montaje de la feria.
