param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$outputRoot = Join-Path $repositoryRoot "artifacts\windows-x64"
$archivePath = Join-Path $repositoryRoot "artifacts\DDLCard-Windows-x64.zip"
$systemDotnet = Get-Command dotnet -ErrorAction SilentlyContinue
$localDotnet = Join-Path $env:LOCALAPPDATA "DDLCardTools\dotnet\dotnet.exe"
$dotnetPath = $null

if ($systemDotnet) {
    $installedSdks = & $systemDotnet.Source --list-sdks
    if ($installedSdks) {
        $dotnetPath = $systemDotnet.Source
    }
}
if (-not $dotnetPath -and (Test-Path -LiteralPath $localDotnet)) {
    $dotnetPath = $localDotnet
}
if (-not $dotnetPath) {
    throw ".NET 10 SDK was not found. Install it from https://dotnet.microsoft.com/download/dotnet/10.0"
}

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $outputRoot) | Out-Null
if (Test-Path -LiteralPath $outputRoot) {
    Remove-Item -LiteralPath $outputRoot -Recurse -Force
}

& $dotnetPath publish (Join-Path $repositoryRoot "src\DDLCard.Windows\DDLCard.Windows.csproj") `
    --configuration $Configuration `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    --output $outputRoot
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

if (Test-Path -LiteralPath $archivePath) {
    Remove-Item -LiteralPath $archivePath -Force
}
Compress-Archive -Path (Join-Path $outputRoot "*") -DestinationPath $archivePath -CompressionLevel Optimal
Write-Output $archivePath
