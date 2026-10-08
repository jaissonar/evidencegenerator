param([switch]$NoBrowser)
$ErrorActionPreference = 'Stop'
$api = Join-Path $PSScriptRoot 'src\EvidenceGenerator.Api'
$web = Join-Path $PSScriptRoot 'src\evidence-generator-web'
$logs = Join-Path $PSScriptRoot 'work'
New-Item -ItemType Directory -Force $logs | Out-Null
$occupiedPorts = @(5080, 5173 | Where-Object { Get-NetTCPConnection -LocalPort $_ -State Listen -ErrorAction SilentlyContinue })
if ($occupiedPorts.Count -gt 0) {
    $alreadyRunning = $false
    try {
        $health = Invoke-RestMethod 'http://127.0.0.1:5080/api/health' -TimeoutSec 3
        $page = Invoke-WebRequest 'http://127.0.0.1:5173/' -UseBasicParsing -TimeoutSec 3
        $alreadyRunning = $health.status -eq 'ok' -and $health.template -eq 'PruebasUnitarias.v1' -and $page.Content -match 'Evidence Generator'
    } catch { $alreadyRunning = $false }
    if ($alreadyRunning) {
        Write-Host 'Evidence Generator ya esta iniciado: http://127.0.0.1:5173'
        if (-not $NoBrowser) { Start-Process 'http://127.0.0.1:5173/' }
        return
    }
    throw "Puertos ocupados: $($occupiedPorts -join ', '). Cierra la instancia anterior antes de iniciar; no se detendran otros procesos automaticamente."
}
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
