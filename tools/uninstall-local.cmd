@echo off
rem Fjerner RedScreen fra Oppstart og stopper den. Innstillingene beholdes.
powershell -NoProfile -Command "Get-CimInstance Win32_Process -Filter \"Name='dotnet.exe'\" | Where-Object { $_.CommandLine -like '*RedScreen.dll*' -and $_.CommandLine -notlike '*--settings*' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }"
del "%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\RedScreen.lnk" 2>nul
echo RedScreen er fjernet fra Oppstart. Mappen %LOCALAPPDATA%\RedScreen\app kan slettes for hand.
