@echo off
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Start-Dev.ps1" %*
if errorlevel 1 (
  echo.
  echo No se pudo iniciar Evidence Generator. Revisa el mensaje anterior.
  pause
  exit /b 1
)
