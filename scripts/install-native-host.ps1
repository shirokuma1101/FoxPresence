param(
    [Parameter(Mandatory = $false)]
    [string] $BridgePath = (Join-Path $PSScriptRoot '..\artifacts\bridge\FirefoxDiscordPresence.Bridge.exe')
)
$ErrorActionPreference = 'Stop'
$resolvedBridge = (Resolve-Path -LiteralPath $BridgePath).Path
if ([IO.Path]::GetExtension($resolvedBridge) -ne '.exe') { throw 'BridgePath must point to FirefoxDiscordPresence.Bridge.exe.' }
$installDirectory = Join-Path $env:APPDATA 'FirefoxDiscordPresence\native-host'
[IO.Directory]::CreateDirectory($installDirectory) | Out-Null
$manifestPath = Join-Path $installDirectory 'com.shiro1103.firefox_discord_presence.json'
$manifest = [ordered]@{
    name = 'com.shiro1103.firefox_discord_presence'
    description = 'Firefox Discord Presence native messaging bridge'
    path = $resolvedBridge
    type = 'stdio'
    allowed_extensions = @('firefox-discord-presence@shiro1103.local')
}
$manifest | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath $manifestPath -Encoding utf8NoBOM
$registryPath = 'HKCU:\Software\Mozilla\NativeMessagingHosts\com.shiro1103.firefox_discord_presence'
New-Item -Path $registryPath -Force | Out-Null
Set-Item -Path $registryPath -Value $manifestPath
Write-Host "Registered native host: $manifestPath"
