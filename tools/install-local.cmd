@echo off
rem ============================================================
rem  RedScreen: installer testversjon for denne PC-en
rem  - Bygger Release og kopierer til %LOCALAPPDATA%\RedScreen\app
rem  - Legger snarvei i Oppstart (starter ved innlogging)
rem  - Starter appen med en gang
rem  Kjør på nytt for å oppdatere til siste versjon av koden.
rem ============================================================
setlocal
set "APPDIR=%LOCALAPPDATA%\RedScreen\app"
set "ROOT=%~dp0.."

echo Stopper RedScreen hvis den kjorer ...
powershell -NoProfile -Command "Get-CimInstance Win32_Process -Filter \"Name='dotnet.exe'\" | Where-Object { $_.CommandLine -like '*RedScreen.dll*' -and $_.CommandLine -notlike '*--settings*' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }"

echo Bygger ...
dotnet publish "%ROOT%\src\RedScreen\RedScreen.csproj" -c Release -o "%APPDIR%" -p:UseAppHost=false --nologo -v q
if errorlevel 1 (
  echo Bygging feilet.
  exit /b 1
)

echo Lager snarvei i Oppstart ...
powershell -NoProfile -Command ^
  "$s = (New-Object -ComObject WScript.Shell).CreateShortcut([Environment]::GetFolderPath('Startup') + '\RedScreen.lnk');" ^
  "$s.TargetPath = \"$env:WINDIR\System32\conhost.exe\";" ^
  "$s.Arguments = '--headless dotnet \"' + $env:LOCALAPPDATA + '\RedScreen\app\RedScreen.dll\"';" ^
  "$s.WorkingDirectory = $env:LOCALAPPDATA + '\RedScreen\app';" ^
  "$s.IconLocation = $env:LOCALAPPDATA + '\RedScreen\app\app.ico';" ^
  "$s.Description = 'RedScreen';" ^
  "$s.WindowStyle = 7;" ^
  "$s.Save()"

copy /y "%ROOT%\src\RedScreen\app.ico" "%APPDIR%\app.ico" >nul

echo Starter RedScreen ...
start "" "%WINDIR%\System32\conhost.exe" --headless dotnet "%APPDIR%\RedScreen.dll"

echo Ferdig. RedScreen starter nå automatisk når du logger inn.
endlocal
