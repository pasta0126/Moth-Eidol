@echo off
rem Build the Windows package (self-contained, single file) on a Windows host.
rem Usage: scripts\publish.cmd      Output: artifacts\MothEidol-<version>-win-x64.zip
rem The macOS and Linux packages are produced with scripts/publish.sh.
setlocal EnableDelayedExpansion

set "ROOT=%~dp0.."
set "OUT=%ROOT%\artifacts"

where dotnet >nul 2>nul || (echo dotnet SDK not found. Install it from https://dotnet.microsoft.com/download & exit /b 1)

for /f "tokens=2 delims=<>" %%v in ('findstr "<Version>" "%ROOT%\Directory.Build.props"') do set "VERSION=%%v"
if not defined VERSION (echo Could not read the version from Directory.Build.props & exit /b 1)

set "NAME=MothEidol-%VERSION%-win-x64"
set "PUB=%OUT%\publish\win-x64"
set "STAGE=%OUT%\stage\%NAME%"
if exist "%PUB%" rmdir /s /q "%PUB%"
if exist "%STAGE%" rmdir /s /q "%STAGE%"
mkdir "%STAGE%"

dotnet publish "%ROOT%\src\MothEidol.App" -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true ^
  -p:PublishTrimmed=false -p:DebugType=none -p:DebugSymbols=false -o "%PUB%" --nologo -v quiet || exit /b 1

move "%PUB%\MothEidol.App.exe" "%STAGE%\MothEidol.exe" >nul
powershell -NoProfile -Command "(Get-Content '%ROOT%\scripts\dist\README.win.txt') -replace '@VERSION@','%VERSION%' -replace '@ARCH@','x64' | Set-Content '%STAGE%\README.txt'"

if exist "%OUT%\%NAME%.zip" del "%OUT%\%NAME%.zip"
powershell -NoProfile -Command "Compress-Archive -Path '%STAGE%' -DestinationPath '%OUT%\%NAME%.zip'" || exit /b 1
rmdir /s /q "%OUT%\stage"
echo %OUT%\%NAME%.zip
