param(
    [string]$Version = "2.5.11",
    [string]$OutputPath = ".perf/UniTask"
)

$ErrorActionPreference = "Stop"

$root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path
$perf = Join-Path $root ".perf"
$download = Join-Path $perf "download"
$archive = Join-Path $download "unitask-$Version.tar.gz"
$extract = Join-Path $download "UniTask-$Version"
$output = Join-Path $root $OutputPath

New-Item -ItemType Directory -Force -Path $download | Out-Null

if (Test-Path -LiteralPath $output) {
    $resolvedOutput = (Resolve-Path -LiteralPath $output).Path
    $resolvedPerf = (Resolve-Path -LiteralPath $perf).Path
    if (-not $resolvedOutput.StartsWith($resolvedPerf, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove output outside .perf: $resolvedOutput"
    }

    Remove-Item -LiteralPath $resolvedOutput -Recurse -Force
}

$url = "https://codeload.github.com/Cysharp/UniTask/tar.gz/refs/tags/$Version"
Invoke-WebRequest -Uri $url -OutFile $archive

if (Test-Path -LiteralPath $extract) {
    Remove-Item -LiteralPath $extract -Recurse -Force
}

tar -xzf $archive -C $download

$source = Join-Path $extract "src/UniTask/Assets/Plugins/UniTask"
if (-not (Test-Path -LiteralPath $source)) {
    throw "UniTask package source not found: $source"
}

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $output) | Out-Null
Copy-Item -LiteralPath $source -Destination $output -Recurse
Write-Host "Prepared UniTask $Version at $output"
