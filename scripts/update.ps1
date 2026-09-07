param([switch] $Silent)

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$packageRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$versionPath = Join-Path $packageRoot 'VERSION'
if (-not (Test-Path -LiteralPath $versionPath)) { throw 'VERSION file is missing.' }
$currentVersion = [version](Get-Content -Raw -LiteralPath $versionPath).Trim()
$headers = @{ 'User-Agent' = 'FoxPresence-Updater'; 'Accept' = 'application/vnd.github+json' }
$release = Invoke-RestMethod -Uri 'https://api.github.com/repos/shirokuma1101/FoxPresence/releases/latest' -Headers $headers
$latestVersion = [version]($release.tag_name -replace '^v', '')
if ($latestVersion -le $currentVersion) {
    if (-not $Silent) { Write-Host "FoxPresence $currentVersion is already up to date." }
    return
}
$asset = $release.assets | Where-Object { $_.name -eq 'FoxPresence-win-x64.zip' } | Select-Object -First 1
if (-not $asset -or $asset.digest -notmatch '^sha256:([a-fA-F0-9]{64})$') { throw 'Release asset or SHA-256 digest is missing.' }
$expectedHash = $Matches[1]
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ("FoxPresenceUpdate-" + [guid]::NewGuid().ToString('N'))
$archivePath = Join-Path $tempRoot 'update.zip'
$extractPath = Join-Path $tempRoot 'package'
try {
    [IO.Directory]::CreateDirectory($extractPath) | Out-Null
    Invoke-WebRequest -Uri $asset.browser_download_url -Headers $headers -OutFile $archivePath -UseBasicParsing
    $actualHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $archivePath).Hash
    if ($actualHash -ne $expectedHash) { throw 'Downloaded update failed SHA-256 verification.' }
    Expand-Archive -LiteralPath $archivePath -DestinationPath $extractPath
    if (-not (Test-Path (Join-Path $extractPath 'tray\FirefoxDiscordPresence.Tray.exe')) -or
        -not (Test-Path (Join-Path $extractPath 'bridge\FirefoxDiscordPresence.Bridge.exe'))) { throw 'Downloaded package is invalid.' }
    Get-Process -Name 'FirefoxDiscordPresence.Tray' -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 750
    Copy-Item -Path (Join-Path $extractPath '*') -Destination $packageRoot -Recurse -Force
    Expand-Archive -LiteralPath (Join-Path $packageRoot 'firefox-extension.zip') -DestinationPath (Join-Path $packageRoot 'firefox-extension') -Force
    & (Join-Path $packageRoot 'scripts\install-native-host.ps1') -BridgePath (Join-Path $packageRoot 'bridge\FirefoxDiscordPresence.Bridge.exe')
    Start-Process -FilePath (Join-Path $packageRoot 'tray\FirefoxDiscordPresence.Tray.exe')
    if (-not $Silent) { Write-Host "FoxPresence was updated to $latestVersion. Reload the temporary Firefox add-on." -ForegroundColor Green }
}
finally { if (Test-Path -LiteralPath $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force } }
