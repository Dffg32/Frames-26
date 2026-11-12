@echo off
rem Starts the Juegardo API at http://localhost:5080
rem Uses the installed .NET SDK if there is one; otherwise the one bundled with Unity.
cd /d "%~dp0"

set "DOTNET=dotnet"
dotnet --list-sdks 2>nul | findstr /r "^8\. ^9\. ^10\." >nul
if errorlevel 1 (
  for /d %%U in ("C:\Program Files\Unity\Hub\Editor\*") do (
    if exist "%%U\Editor\Data\DotNetSdk\dotnet.exe" set "DOTNET=%%U\Editor\Data\DotNetSdk\dotnet.exe"
  )
)

echo Usando: %DOTNET%
"%DOTNET%" run --launch-profile http
pause
