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
& sqlcmd -S '.\SQLEXPRESS' -E -C -b -h -1 -Q "SET NOCOUNT ON; IF DB_ID(N'CloudBooks_CITEIN_2026') IS NULL THROW 50000, 'Falta la base CITEIN; consulta la documentación.', 1; IF OBJECT_ID(N'CloudBooks_CITEIN_2026.dbo.__EFMigrationsHistory') IS NULL THROW 50000, 'Faltan migraciones CITEIN.', 1;" | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'No se pudo verificar la base CITEIN local y sus migraciones.' }
$env:ConnectionStrings__DefaultConnection = 'Server=.\SQLEXPRESS;Database=CloudBooks_CITEIN_2026;Trusted_Connection=True;TrustServerCertificate=True'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Correo__Proveedor = 'Simulado'
$env:CiteinDemo__Enabled = 'true'
$env:Logging__LogLevel__Default = 'Warning'
$direcciones = @(Get-NetIPConfiguration | Where-Object { $_.NetAdapter.Status -eq 'Up' -and $_.IPv4DefaultGateway } | ForEach-Object { $_.IPv4Address.IPAddress } | Where-Object { $_ -and $_ -notlike '169.254.*' })
Write-Host 'Base: CloudBooks_CITEIN_2026 | Correo: Simulado | Red: solo privada'
if ($direcciones.Count -eq 0) { Write-Warning 'No se detectó una IP con puerta de enlace. Consulta ipconfig y usa la IPv4 de la red privada.' }
foreach ($ip in $direcciones) {
    Write-Host "Tablet / Jurado: http://${ip}:${Puerto}/citein"
    Write-Host "Administración:   http://${ip}:${Puerto}/"
    Write-Host "Monitor CITEIN:   http://${ip}:${Puerto}/citein/monitor"
}
Write-Host 'Detén la aplicación con Ctrl+C en esta ventana.'
& dotnet run --project $proyecto --no-launch-profile --urls "http://0.0.0.0:$Puerto"
if ($LASTEXITCODE -ne 0) { throw 'La aplicación terminó con error.' }
