$ErrorActionPreference = 'Stop'
$updateScript = (Resolve-Path (Join-Path $PSScriptRoot 'update.ps1')).Path
$command = "powershell.exe -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$updateScript`" -Silent"
$key = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey('Software\Microsoft\Windows\CurrentVersion\Run')
try { $key.SetValue('FoxPresenceAutoUpdate', $command) } finally { $key.Dispose() }
Write-Host 'Automatic update check is enabled for Windows sign-in.' -ForegroundColor Green
