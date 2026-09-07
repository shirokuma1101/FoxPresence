$ErrorActionPreference = 'Stop'
$key = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey('Software\Microsoft\Windows\CurrentVersion\Run')
try { $key.DeleteValue('FoxPresenceAutoUpdate', $false) } finally { $key.Dispose() }
Write-Host 'Automatic update check is disabled.' -ForegroundColor Green
