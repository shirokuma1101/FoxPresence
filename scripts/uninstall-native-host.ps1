$ErrorActionPreference = 'Stop'
$registryPath = 'HKCU:\Software\Mozilla\NativeMessagingHosts\com.shiro1103.firefox_discord_presence'
if (Test-Path -LiteralPath $registryPath) { Remove-Item -LiteralPath $registryPath -Recurse -Force }
$manifestPath = Join-Path $env:APPDATA 'FirefoxDiscordPresence\native-host\com.shiro1103.firefox_discord_presence.json'
if (Test-Path -LiteralPath $manifestPath) { Remove-Item -LiteralPath $manifestPath -Force }
Write-Host 'Firefox Discord Presence native host was unregistered.'
