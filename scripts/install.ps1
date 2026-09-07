param(
    [Parameter(Mandatory = $false)]
    [string] $DiscordApplicationId
)

$ErrorActionPreference = 'Stop'
$packageRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$bridgePath = Join-Path $packageRoot 'bridge\FirefoxDiscordPresence.Bridge.exe'
$trayPath = Join-Path $packageRoot 'tray\FirefoxDiscordPresence.Tray.exe'

if (-not (Test-Path -LiteralPath $bridgePath) -or -not (Test-Path -LiteralPath $trayPath)) {
    throw 'Release package is incomplete. Extract the entire ZIP before running this script.'
}

if (-not $DiscordApplicationId) {
    $DiscordApplicationId = Read-Host 'Enter the Discord Application ID'
}
if ($DiscordApplicationId -notmatch '^\d{17,20}$') {
    throw 'Discord Application ID must contain 17 to 20 digits.'
}

$settingsDirectory = Join-Path $env:APPDATA 'FirefoxDiscordPresence'
[IO.Directory]::CreateDirectory($settingsDirectory) | Out-Null
$settingsPath = Join-Path $settingsDirectory 'settings.json'
$settings = [ordered]@{
    presenceEnabled = $true
    startWithWindows = $false
    discordApplicationId = $DiscordApplicationId
    staleTimeoutSeconds = 45
}
$settings | ConvertTo-Json | Set-Content -LiteralPath $settingsPath -Encoding utf8NoBOM

& (Join-Path $PSScriptRoot 'install-native-host.ps1') -BridgePath $bridgePath
$extensionArchive = Join-Path $packageRoot 'firefox-extension.zip'
$extensionDirectory = Join-Path $packageRoot 'firefox-extension'
Expand-Archive -LiteralPath $extensionArchive -DestinationPath $extensionDirectory -Force
Start-Process -FilePath $trayPath

Write-Host ''
Write-Host 'FoxPresence setup completed.' -ForegroundColor Green
Write-Host "Next: load $extensionDirectory\manifest.json temporarily from about:debugging in Firefox."
