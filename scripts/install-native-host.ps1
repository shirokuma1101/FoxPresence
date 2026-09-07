param(
    [Parameter(Mandatory = $false)]
    [string] $BridgePath
)
$ErrorActionPreference = 'Stop'
if (-not $BridgePath) {
    $releaseBridge = Join-Path $PSScriptRoot '..\bridge\FirefoxDiscordPresence.Bridge.exe'
    $developmentBridge = Join-Path $PSScriptRoot '..\artifacts\bridge\FirefoxDiscordPresence.Bridge.exe'
    $BridgePath = if (Test-Path -LiteralPath $releaseBridge) { $releaseBridge } else { $developmentBridge }
}
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
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[IO.File]::WriteAllText($manifestPath, ($manifest | ConvertTo-Json -Depth 3), $utf8NoBom)
$registryPath = 'HKCU:\Software\Mozilla\NativeMessagingHosts\com.shiro1103.firefox_discord_presence'
New-Item -Path $registryPath -Force | Out-Null
Set-Item -Path $registryPath -Value $manifestPath
Write-Host "Registered native host: $manifestPath"
