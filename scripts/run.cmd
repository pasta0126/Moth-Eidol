@echo off
rem Build and launch Moth-Eidol straight from the project sources (Windows).
rem Usage: scripts\run.cmd [-c Release]
setlocal
where dotnet >nul 2>nul
if errorlevel 1 (
  echo dotnet SDK not found. Install it from https://dotnet.microsoft.com/download
  exit /b 1
)
pushd "%~dp0.."
dotnet run --project src\MothEidol.App %*
set EXITCODE=%ERRORLEVEL%
popd
exit /b %EXITCODE%
