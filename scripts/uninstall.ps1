param(
    [switch] $KeepUserData,
    [switch] $KeepLogs
)

$ErrorActionPreference = 'Stop'

Get-Process -Name 'FirefoxDiscordPresence.Tray' -ErrorAction SilentlyContinue |
    Stop-Process -Force -ErrorAction SilentlyContinue

& (Join-Path $PSScriptRoot 'uninstall-native-host.ps1')

$startupPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
if (Get-ItemProperty -Path $startupPath -Name 'FirefoxDiscordPresence' -ErrorAction SilentlyContinue) {
    Remove-ItemProperty -Path $startupPath -Name 'FirefoxDiscordPresence'
}

function Remove-FoxPresenceDirectory([string] $Path, [string] $ExpectedParent) {
    if (-not (Test-Path -LiteralPath $Path)) { return }
    $resolvedPath = [IO.Path]::GetFullPath($Path).TrimEnd('\')
    $resolvedParent = [IO.Path]::GetFullPath($ExpectedParent).TrimEnd('\')
    if ((Split-Path $resolvedPath -Parent) -ne $resolvedParent -or (Split-Path $resolvedPath -Leaf) -ne 'FirefoxDiscordPresence') {
        throw "Refusing to remove unexpected path: $resolvedPath"
    }
    Remove-Item -LiteralPath $resolvedPath -Recurse -Force
}

if (-not $KeepUserData) {
    Remove-FoxPresenceDirectory (Join-Path $env:APPDATA 'FirefoxDiscordPresence') $env:APPDATA
}
if (-not $KeepLogs) {
    Remove-FoxPresenceDirectory (Join-Path $env:LOCALAPPDATA 'FirefoxDiscordPresence') $env:LOCALAPPDATA
}

Write-Host ''
Write-Host 'FoxPresence was uninstalled.' -ForegroundColor Green
Write-Host 'Remove the temporary Firefox add-on (or restart Firefox), then delete the extracted FoxPresence folder.'
