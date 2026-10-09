# Builds the test for .NET Framework 4.8 and .NET 10, 64-bit and 32-bit, and runs each
# without and with the stale type library key. The key is always removed at the end.
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$builds = @()
foreach ($tf in 'net48', 'net10.0-windows') {
    foreach ($platform in 'x64', 'x86') {
        $out = "out\$tf-$platform"
        # The runtime identifier sets the bitness of the exe (and of the .NET 10 apphost)
        dotnet build -c Release -f $tf -r "win-$platform" --no-self-contained -o $out | Out-Null
        if ($LASTEXITCODE -ne 0) { Write-Host "Build failed: $tf $platform"; continue }
        $builds += "$out\TypeLibTest.exe"
    }
}
function Run-All($title) {
    Write-Host "`n===== $title"
    foreach ($exe in $builds) { & $exe; Write-Host "(exit $LASTEXITCODE)" }
}
try {
    Run-All 'Without stale key'
    & .\stale-typelib.ps1
    Run-All 'With stale key'
}
finally {
    & .\stale-typelib.ps1 -Remove
}
Run-All 'Key removed again'
$left = @(Get-Process WINWORD -ErrorAction SilentlyContinue).Count
Write-Host "`nWord processes still running: $left"
