param(
    [switch]$SelfContained,
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'artifacts\local')
)
$ErrorActionPreference = 'Stop'
foreach ($port in @(5080, 5173)) {
    if (Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue) {
        throw "Detén la aplicación local antes de publicar. El puerto $port está ocupado y Windows podría bloquear archivos de compilación."
    }
}
$web = Join-Path $PSScriptRoot 'src\evidence-generator-web'
$api = Join-Path $PSScriptRoot 'src\EvidenceGenerator.Api\EvidenceGenerator.Api.csproj'
$output = [IO.Path]::GetFullPath($OutputDirectory)
if ($output -eq [IO.Path]::GetFullPath($PSScriptRoot) -or $output.StartsWith((Join-Path $PSScriptRoot 'src'), [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Usa una carpeta de publicación independiente del código fuente.'
}
Push-Location $web
try {
    npm ci
    if ($LASTEXITCODE -ne 0) { throw 'Falló la instalación del frontend.' }
    npm run build
    if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación del frontend.' }
} finally { Pop-Location }

$standalone = $SelfContained.IsPresent.ToString().ToLowerInvariant()
dotnet publish $api -c Release -r win-x64 --self-contained $standalone --output $output
if ($LASTEXITCODE -ne 0) { throw 'Falló la publicación de la API.' }
$wwwroot = Join-Path $output 'wwwroot'
New-Item -ItemType Directory -Force $wwwroot | Out-Null
Get-ChildItem -LiteralPath (Join-Path $web 'dist') | Copy-Item -Destination $wwwroot -Recurse -Force

$launcher = @'
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    Write-Host 'Evidence Generator: http://127.0.0.1:5080'
    Write-Host 'Ctrl+C para detener. Los datos se guardan en App_Data junto a este script.'
    & (Join-Path $PSScriptRoot 'EvidenceGenerator.Api.exe') --urls http://127.0.0.1:5080 --contentRoot $PSScriptRoot
} finally { Pop-Location }
'@
Set-Content -LiteralPath (Join-Path $output 'Start-Local.ps1') -Value $launcher -Encoding utf8
Write-Host "Publicación creada en: $output"
Write-Host 'Ejecuta Start-Local.ps1 y abre http://127.0.0.1:5080'
