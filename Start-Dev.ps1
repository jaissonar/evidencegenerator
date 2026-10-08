param([switch]$NoBrowser)
$ErrorActionPreference = 'Stop'
$api = Join-Path $PSScriptRoot 'src\EvidenceGenerator.Api'
$web = Join-Path $PSScriptRoot 'src\evidence-generator-web'
$logs = Join-Path $PSScriptRoot 'work'
New-Item -ItemType Directory -Force $logs | Out-Null
# Only reclaim listeners belonging to this checkout; another app may use these ports.
function Test-ProjectProcess($process) {
    if (-not $process) { return $false }
    $rootPath = [IO.Path]::GetFullPath($PSScriptRoot).TrimEnd('\') + '\'
    $executable = $process.ExecutablePath
    if ($executable -and $executable.StartsWith($rootPath, [StringComparison]::OrdinalIgnoreCase) -and
        [IO.Path]::GetFileName($executable) -eq 'EvidenceGenerator.Api.exe') { return $true }
    $command = ([string]$process.CommandLine).Replace('/', '\')
    $webPath = [IO.Path]::GetFullPath($web).TrimEnd('\') + '\'
    if ([IO.Path]::GetFileName($executable) -eq 'node.exe' -and
        $command.IndexOf($webPath, [StringComparison]::OrdinalIgnoreCase) -ge 0 -and
        $command -match 'vite[\\]+bin[\\]+vite\.js') { return $true }
    $apiPath = [IO.Path]::GetFullPath($api).TrimEnd('\') + '\'
    return ([IO.Path]::GetFileName($executable) -eq 'dotnet.exe' -and
        $command.IndexOf($apiPath, [StringComparison]::OrdinalIgnoreCase) -ge 0 -and
        $command -match 'EvidenceGenerator\.Api\.dll(?:"|\s|$)')
}
$listeners = @(Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue | Where-Object { $_.LocalPort -in 5080, 5173 })
$previousProcesses = @()
foreach ($ownerId in @($listeners.OwningProcess | Sort-Object -Unique)) {
    $process = Get-CimInstance Win32_Process -Filter "ProcessId = $ownerId" -ErrorAction SilentlyContinue
    if (-not $process) { continue }
    if (-not (Test-ProjectProcess $process)) {
        $ports = ($listeners | Where-Object { $_.OwningProcess -eq $ownerId }).LocalPort -join ', '
        throw "El puerto $ports pertenece a otro proceso o no se pudo verificar su origen (PID $ownerId). No se cerrara automaticamente."
    }
    $previousProcesses += $process
}
foreach ($process in $previousProcesses) {
    $current = Get-CimInstance Win32_Process -Filter "ProcessId = $($process.ProcessId)" -ErrorAction SilentlyContinue
    if ($current -and $current.CreationDate -eq $process.CreationDate -and (Test-ProjectProcess $current)) {
        Write-Host "Cerrando instancia anterior de Evidence Generator (PID $($current.ProcessId))..."
        Stop-Process -Id $current.ProcessId -Force -ErrorAction Stop
    }
}
for ($attempt = 0; $attempt -lt 20; $attempt++) {
    $remaining = @(Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue | Where-Object { $_.LocalPort -in 5080, 5173 })
    if ($remaining.Count -eq 0) { break }
    Start-Sleep -Milliseconds 500
}
if ($remaining.Count -gt 0) { throw 'No se pudieron liberar los puertos 5080/5173. Revisa los procesos o permisos e intenta nuevamente.' }

foreach ($tool in @('dotnet', 'npm')) { if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) { throw "No se encontro $tool. Consulta docs/Manual-Evidence-Generator.md." } }
Push-Location $web
try {
    if (-not (Test-Path 'node_modules')) { npm ci; if ($LASTEXITCODE -ne 0) { throw 'No se pudieron instalar las dependencias.' } }
    $apiProcess = Start-Process dotnet -ArgumentList @('run', '--no-launch-profile', '--urls', 'http://127.0.0.1:5080') -WorkingDirectory $api -WindowStyle Hidden -RedirectStandardOutput (Join-Path $logs 'api.log') -RedirectStandardError (Join-Path $logs 'api.error.log') -PassThru
    $ready = $false
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        if ($apiProcess.HasExited) { throw 'La API no pudo iniciar. Revisa work/api.error.log y work/api.log.' }
        try { $ready = (Invoke-RestMethod 'http://127.0.0.1:5080/api/health' -TimeoutSec 1).status -eq 'ok' } catch { $ready = $false }
        if ($ready) { break }
        Start-Sleep -Milliseconds 500
    }
    if (-not $ready) { throw 'La API no respondio a tiempo. Revisa work/api.log.' }
    Write-Host 'Evidence Generator: http://127.0.0.1:5173'
    Write-Host 'API: http://127.0.0.1:5080/api/health | Ctrl+C para detener.'
    if ($NoBrowser) { npm run dev -- --host 127.0.0.1 --port 5173 --strictPort }
    else { npm run dev -- --host 127.0.0.1 --port 5173 --strictPort --open }
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo iniciar la interfaz.' }
} finally {
    if ($apiProcess -and -not $apiProcess.HasExited) { taskkill /PID $apiProcess.Id /T /F | Out-Null }
    Pop-Location
}

