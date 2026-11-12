param([ValidateRange(1,65535)][int]$Puerto = 5199)
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent $PSScriptRoot
$proyecto = Join-Path $raiz 'src\CloudBooks.Web\CloudBooks.Web.csproj'
if (-not (Test-Path -LiteralPath $proyecto)) { throw 'No se encontró el proyecto CloudBooks en la carpeta esperada.' }
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue) -or -not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) { throw 'Se requieren dotnet y sqlcmd en PATH.' }
$servicio = Get-Service -Name 'MSSQL$SQLEXPRESS' -ErrorAction SilentlyContinue
if (-not $servicio -or $servicio.Status -ne 'Running') { throw 'SQL Server Express no está en ejecución. Inicia MSSQL$SQLEXPRESS y vuelve a intentar.' }
$sonda = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Any, $Puerto)
try { $sonda.Start() } catch { throw "El puerto $Puerto ya está ocupado. Detén solo el proceso que corresponda o usa -Puerto con otro puerto libre." } finally { $sonda.Stop() }
Write-Host 'CloudBooks CITEIN — Preparación'
Write-Host 'SQL Server Express: disponible.'
& sqlcmd -S '.\SQLEXPRESS' -d CloudBooks_CITEIN_2026 -E -C -b -h -1 -Q "SET NOCOUNT ON; SELECT 1;" | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'No se pudo conectar a CloudBooks_CITEIN_2026 en SQL Express.' }
Write-Host 'Base CITEIN: disponible.'
$migracionesEsperadas = @(Get-ChildItem (Join-Path $raiz 'src\CloudBooks.Web\Datos\Migraciones') -Filter '*.cs' | Where-Object { $_.Name -match '^\d{14}_.+\.cs$' -and $_.Name -notmatch '\.Designer\.cs$' } | ForEach-Object { $_.BaseName })
$migracionesAplicadas = @(& sqlcmd -S '.\SQLEXPRESS' -d CloudBooks_CITEIN_2026 -E -C -b -h -1 -W -Q "SET NOCOUNT ON; SELECT MigrationId FROM dbo.__EFMigrationsHistory;" | ForEach-Object { $_.Trim() } | Where-Object { $_ })
if ($LASTEXITCODE -ne 0 -or $migracionesEsperadas.Count -eq 0) { throw 'No se pudo comprobar el historial de migraciones.' }
$pendientes = @($migracionesEsperadas | Where-Object { $_ -notin $migracionesAplicadas })
if ($pendientes.Count -gt 0) { throw "Faltan migraciones: $($pendientes -join ', '). Revisa la documentación antes de continuar." }
Write-Host "Migraciones del proyecto: $($migracionesEsperadas.Count) aplicadas."
$filas = @(& sqlcmd -S '.\SQLEXPRESS' -d CloudBooks_CITEIN_2026 -E -C -b -h -1 -W -s '|' -Q "SET NOCOUNT ON; SELECT ISBN,Formato,Stock,Activo FROM dbo.Libros WHERE ISBN IN ('9786075502090','9786075502076','9786075502069','9786073221245','9781951693510','9781951693909');" | Where-Object { $_.Trim() })
if ($LASTEXITCODE -ne 0) { throw 'No se pudo consultar el catálogo CITEIN.' }
$esperados = @('9786075502090','9786075502076','9786075502069','9786073221245','9781951693510','9781951693909')
$registros = @{}; foreach ($fila in $filas) { $partes = $fila.Split('|').Trim(); if ($partes.Count -eq 4) { $registros[$partes[0]] = $partes } }
$faltan = @($esperados | Where-Object { -not $registros.ContainsKey($_) -or $registros[$_][3] -ne '1' })
if ($faltan.Count -gt 0) { throw "Faltan libros activos en el catálogo CITEIN: $($faltan -join ', '). Consulta la preparación del catálogo." }
$digitales = @('9781951693510','9781951693909')
$formatosInvalidos = @($esperados | Where-Object { if ($_ -in $digitales) { $registros[$_][1] -ne '2' } else { $registros[$_][1] -notin @('0','1') } })
if ($formatosInvalidos.Count -gt 0) { throw "Formato de libro inesperado: $($formatosInvalidos -join ', ')." }
Write-Host 'Catálogo: seis libros activos configurados.'
$bajo = @($esperados | Where-Object { $registros[$_][1] -ne '2' -and [int]$registros[$_][2] -lt 20 })
if ($bajo.Count -gt 0) { Write-Warning "Existencias físicas menores de 20: $($bajo -join ', '). Revísalas en Administración." } else { Write-Host 'Inventario: al menos 20 ejemplares por libro físico.' }
Write-Host "Puerto ${Puerto}: disponible."
$env:ConnectionStrings__DefaultConnection = 'Server=.\SQLEXPRESS;Database=CloudBooks_CITEIN_2026;Trusted_Connection=True;TrustServerCertificate=True'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Correo__Proveedor = 'Simulado'
$env:CiteinDemo__Enabled = 'true'
$env:Logging__LogLevel__Default = 'Warning'
[Environment]::SetEnvironmentVariable('Logging__LogLevel__Microsoft.Hosting.Lifetime', 'Information', 'Process')
Write-Host 'Correo: proveedor Simulado configurado para esta ejecución.'
$direcciones = @(Get-NetIPConfiguration | Where-Object { $_.NetAdapter.Status -eq 'Up' -and $_.IPv4DefaultGateway } | ForEach-Object { $_.IPv4Address.IPAddress } | Where-Object { $_ -and $_ -notlike '169.254.*' })
Write-Host 'Base: CloudBooks_CITEIN_2026 | Correo: Simulado | Red: solo privada'
if ($direcciones.Count -eq 0) { Write-Warning 'No se detectó una IP con puerta de enlace. Consulta ipconfig y usa la IPv4 de la red privada.' }
foreach ($ip in $direcciones) {
    Write-Host "Tablet / Jurado: http://${ip}:${Puerto}/citein"
    Write-Host "Administración:   http://${ip}:${Puerto}/"
    Write-Host "Monitor CITEIN:   http://${ip}:${Puerto}/citein/monitor"
}
Write-Host 'Detén la aplicación con Ctrl+C en esta ventana.'
$verificado = $false
& dotnet run --project $proyecto --no-launch-profile --urls "http://0.0.0.0:$Puerto" | ForEach-Object {
    Write-Host $_
    if (-not $verificado -and "$_" -match 'Now listening on:') {
        $verificado = $true
        try {
            $respuesta = Invoke-WebRequest -Uri "http://127.0.0.1:$Puerto/citein" -UseBasicParsing -TimeoutSec 8
            if ($respuesta.StatusCode -eq 200 -and $respuesta.Content -match 'Bienvenido a') { Write-Host 'Aplicación local: accesible (HTTP 200).' }
            else { Write-Warning 'La aplicación respondió, pero no se pudo verificar la bienvenida CITEIN.' }
        } catch { Write-Warning 'No se pudo verificar la aplicación local. Comprueba la ventana y vuelve a probar la URL.' }
    }
}
if ($LASTEXITCODE -ne 0) { throw 'La aplicación terminó con error.' }
if (-not $verificado) { Write-Warning 'No se recibió una señal de escucha; no se comprobó la aplicación local.' }
