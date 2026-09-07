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
$registryPath = 'HKCU:\Software\Mozilla\NativeMessagingHosts\com.shiro1103.firefox_discord_presence'
$manifestPath = Join-Path $env:APPDATA 'FirefoxDiscordPresence\native-host\com.shiro1103.firefox_discord_presence.json'
$nativeHostWasRegistered = Test-Path -LiteralPath $registryPath
$stoppedApplications = $false
$updateCompleted = $false

function Stop-FoxPresenceProcesses {
    foreach ($processName in @('FirefoxDiscordPresence.Tray', 'FirefoxDiscordPresence.Bridge')) {
        $processes = @(Get-Process -Name $processName -ErrorAction SilentlyContinue)
        foreach ($process in $processes) {
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
            try { $process.WaitForExit(5000) | Out-Null } catch { }
        }
    }
}

function Restore-NativeHostRegistration {
    if (-not $nativeHostWasRegistered -or -not (Test-Path -LiteralPath $manifestPath)) { return }
    $key = New-Item -Path $registryPath -Force
    Set-Item -LiteralPath $key.PSPath -Value $manifestPath
}

try {
    [IO.Directory]::CreateDirectory($extractPath) | Out-Null
    Invoke-WebRequest -Uri $asset.browser_download_url -Headers $headers -OutFile $archivePath -UseBasicParsing
    $actualHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $archivePath).Hash
    if ($actualHash -ne $expectedHash) { throw 'Downloaded update failed SHA-256 verification.' }
    Expand-Archive -LiteralPath $archivePath -DestinationPath $extractPath
    if (-not (Test-Path (Join-Path $extractPath 'tray\FirefoxDiscordPresence.Tray.exe')) -or
        -not (Test-Path (Join-Path $extractPath 'bridge\FirefoxDiscordPresence.Bridge.exe'))) { throw 'Downloaded package is invalid.' }
    if ($nativeHostWasRegistered) { Remove-Item -LiteralPath $registryPath -Recurse -Force }
    Stop-FoxPresenceProcesses
    $stoppedApplications = $true

    $copyError = $null
    for ($attempt = 1; $attempt -le 5; $attempt++) {
        try {
            Copy-Item -Path (Join-Path $extractPath '*') -Destination $packageRoot -Recurse -Force
            $copyError = $null
            break
        }
        catch {
            $copyError = $_
            Stop-FoxPresenceProcesses
            Start-Sleep -Milliseconds 500
        }
    }
    if ($copyError) { throw $copyError }
    Expand-Archive -LiteralPath (Join-Path $packageRoot 'firefox-extension.zip') -DestinationPath (Join-Path $packageRoot 'firefox-extension') -Force
    & (Join-Path $packageRoot 'scripts\install-native-host.ps1') -BridgePath (Join-Path $packageRoot 'bridge\FirefoxDiscordPresence.Bridge.exe')
    Start-Process -FilePath (Join-Path $packageRoot 'tray\FirefoxDiscordPresence.Tray.exe')
    $updateCompleted = $true
    if (-not $Silent) { Write-Host "FoxPresence was updated to $latestVersion. Reload the temporary Firefox add-on." -ForegroundColor Green }
}
finally {
    if (-not $updateCompleted) {
        Restore-NativeHostRegistration
        if ($stoppedApplications) {
            $existingTray = Join-Path $packageRoot 'tray\FirefoxDiscordPresence.Tray.exe'
            if (Test-Path -LiteralPath $existingTray) { Start-Process -FilePath $existingTray }
        }
    }
    if (Test-Path -LiteralPath $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
}
