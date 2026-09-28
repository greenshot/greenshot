<#
.SYNOPSIS
    Local build/test runner for Greenshot, driven by request files.

.DESCRIPTION
    Watches <repo>\.build-runner\requests for *.json request files and executes a FIXED set of actions:
      build   - MSBuild (from Visual Studio / Build Tools) of src\Greenshot.sln, including the C++ proxy projects
      test    - dotnet test of src\Greenshot.Tests (no build), optional test filter
      verify  - build, then test
    Nothing else can be executed: the request only selects an action, a configuration and an optional,
    validated test filter. Output goes to <repo>\.build-runner\results\<id>.log and a summary to <id>.json.

    A running request is cancelled (process tree killed) when the file <repo>\.build-runner\cancel exists or after
    -TimeoutMinutes. Tests that hang longer than -TestHangTimeout are aborted by dotnet test (--blame-hang), the log then
    names the hanging test. When this script file changes, the runner restarts itself with the new version.

    This lets a coding assistant (or any tool that can write files into the repository) trigger builds and tests
    without having a shell on this machine. Start it once per session; stop it with Ctrl+C.

    Request file example (.build-runner\requests\<id>.json):
      { "id": "20260928-1500-verify", "action": "verify", "configuration": "Release", "filter": "FullyQualifiedName~Ipc" }

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\build-runner\Start-BuildRunner.ps1
#>
[CmdletBinding()]
param(
    [int]$PollSeconds = 2,
    [int]$TimeoutMinutes = 30,
    [string]$TestHangTimeout = '3m'
)

$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$RunnerDir = Join-Path $RepoRoot '.build-runner'
$RequestDir = Join-Path $RunnerDir 'requests'
$ResultDir = Join-Path $RunnerDir 'results'
$CancelFile = Join-Path $RunnerDir 'cancel'
$Solution = Join-Path $RepoRoot 'src\Greenshot.sln'
$TestProject = Join-Path $RepoRoot 'src\Greenshot.Tests\Greenshot.Tests.csproj'

New-Item -ItemType Directory -Force -Path $RequestDir, $ResultDir | Out-Null

function Find-MSBuild {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path $vswhere) {
        $path = & $vswhere -latest -prerelease -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
        if ($path -and (Test-Path $path)) { return $path }
    }
    $cmd = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    return $null
}

$MSBuild = Find-MSBuild
$Dotnet = (Get-Command dotnet.exe -ErrorAction SilentlyContinue).Source

function Invoke-Step {
    param([string]$Name, [string]$Exe, [string[]]$Arguments, [string]$Log)

    Add-Content -Path $Log -Encoding UTF8 -Value ("==== {0}: {1} {2}" -f $Name, $Exe, ($Arguments -join ' '))
    $out = "$Log.$Name.out"
    $err = "$Log.$Name.err"
    $proc = Start-Process -FilePath $Exe -ArgumentList $Arguments -WorkingDirectory $RepoRoot -NoNewWindow -PassThru `
        -RedirectStandardOutput $out -RedirectStandardError $err
    # Touch the handle right away, otherwise ExitCode stays empty for processes started with redirection
    $null = $proc.Handle
    $deadline = (Get-Date).AddMinutes($TimeoutMinutes)
    $stopReason = $null
    while (-not $proc.WaitForExit(1000)) {
        if (Test-Path $CancelFile) { $stopReason = 'CANCELLED' }
        elseif ((Get-Date) -gt $deadline) { $stopReason = "TIMED OUT after $TimeoutMinutes minutes" }
        if ($stopReason) {
            # Kill the whole tree (e.g. testhost.exe started by dotnet test)
            & taskkill.exe /T /F /PID $proc.Id 2>&1 | Out-Null
            $proc.WaitForExit(10000) | Out-Null
            break
        }
    }
    if ($stopReason) {
        Remove-Item -Path $CancelFile -Force -ErrorAction SilentlyContinue
        Add-Content -Path $Log -Encoding UTF8 -Value "==== $Name $stopReason"
        $code = -1
    } else {
        $code = $proc.ExitCode
    }
    foreach ($f in @($out, $err)) {
        if (Test-Path $f) {
            Get-Content -Path $f -Encoding UTF8 | Add-Content -Path $Log -Encoding UTF8
            Remove-Item $f -Force
        }
    }
    Add-Content -Path $Log -Encoding UTF8 -Value "==== $Name exit code: $code"
    return $code
}

function Invoke-Request {
    param([string]$File)

    $request = Get-Content -Path $File -Raw -Encoding UTF8 | ConvertFrom-Json
    $id = [string]$request.id
    if ($id -notmatch '^[A-Za-z0-9_.-]{1,80}$') { throw "Invalid request id '$id'." }

    $action = ([string]$request.action).ToLowerInvariant()
    if ($action -notin @('build', 'test', 'verify')) { throw "Invalid action '$action' (allowed: build, test, verify)." }

    $configuration = if ($request.configuration) { [string]$request.configuration } else { 'Debug' }
    if ($configuration -notin @('Debug', 'Release')) { throw "Invalid configuration '$configuration'." }

    $filter = [string]$request.filter
    if ($filter -and $filter -notmatch '^[A-Za-z0-9_.=~!&|(),* -]{1,300}$') { throw "Invalid test filter." }

    $log = Join-Path $ResultDir "$id.log"
    Set-Content -Path $log -Encoding UTF8 -Value ("Request {0}: action={1} configuration={2} filter={3} started={4:o}" -f $id, $action, $configuration, $filter, (Get-Date))

    $summary = [ordered]@{ id = $id; action = $action; configuration = $configuration; filter = $filter; started = (Get-Date).ToString('o') }

    if ($action -in @('build', 'verify')) {
        if (-not $MSBuild) { throw 'MSBuild (Visual Studio or Build Tools with C++ workload) not found; needed for the C++ proxy projects.' }
        $summary.build_exit_code = Invoke-Step -Name 'build' -Exe $MSBuild -Log $log -Arguments @(
            "`"$Solution`"", '/restore', "/p:Configuration=$configuration", '/m', '/nologo', '/v:minimal', '/clp:Summary;ErrorsOnly;WarningsOnly')
    }

    if ($action -eq 'test' -or ($action -eq 'verify' -and $summary.build_exit_code -eq 0)) {
        if (-not $Dotnet) { throw 'dotnet not found.' }
        $trx = "$id.trx"
        $testArgs = @('test', "`"$TestProject`"", '-c', $configuration, '--no-build', '--nologo',
            '--logger', "`"trx;LogFileName=$trx`"", '--logger', '"console;verbosity=normal"', '--results-directory', "`"$ResultDir`"",
            '--blame-hang', '--blame-hang-timeout', $TestHangTimeout, '--blame-hang-dump-type', 'none')
        if ($filter) { $testArgs += @('--filter', "`"$filter`"") }
        $summary.test_exit_code = Invoke-Step -Name 'test' -Exe $Dotnet -Log $log -Arguments $testArgs
        $summary.trx = $trx
    }

    $summary.finished = (Get-Date).ToString('o')
    $summary.success = (($summary.build_exit_code -eq $null) -or ($summary.build_exit_code -eq 0)) -and
                       (($summary.test_exit_code -eq $null) -or ($summary.test_exit_code -eq 0)) -and
                       -not ($action -eq 'verify' -and $summary.test_exit_code -eq $null)
    return $summary
}

Write-Host "Greenshot build runner watching $RequestDir (MSBuild: $MSBuild, dotnet: $Dotnet). Ctrl+C to stop."
if (Get-Process -Name Greenshot -ErrorAction SilentlyContinue) {
    Write-Warning 'Greenshot is running: builds may fail on locked files and pipe tests may clash with it.'
}

$ScriptPath = $PSCommandPath
$ScriptVersion = (Get-Item $ScriptPath).LastWriteTimeUtc
Remove-Item -Path $CancelFile -Force -ErrorAction SilentlyContinue

while ($true) {
    # Restart with the new version when this script was updated
    if ((Get-Item $ScriptPath).LastWriteTimeUtc -ne $ScriptVersion) {
        Write-Host 'Runner script changed, restarting...'
        & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $ScriptPath -PollSeconds $PollSeconds -TimeoutMinutes $TimeoutMinutes -TestHangTimeout $TestHangTimeout
        exit $LASTEXITCODE
    }

    foreach ($file in Get-ChildItem -Path $RequestDir -Filter '*.json' -File | Sort-Object LastWriteTime) {
        $processing = "$($file.FullName).processing"
        try { Move-Item -Path $file.FullName -Destination $processing -Force } catch { continue }
        $resultName = [IO.Path]::GetFileNameWithoutExtension($file.Name)
        try {
            Write-Host ("[{0:HH:mm:ss}] Running {1}" -f (Get-Date), $file.Name)
            $summary = Invoke-Request -File $processing
            $resultName = $summary.id
        } catch {
            $summary = [ordered]@{ id = $resultName; success = $false; error = $_.Exception.Message; finished = (Get-Date).ToString('o') }
        }
        $json = $summary | ConvertTo-Json -Depth 4
        $tmp = Join-Path $ResultDir "$resultName.json.tmp"
        Set-Content -Path $tmp -Encoding UTF8 -Value $json
        Move-Item -Path $tmp -Destination (Join-Path $ResultDir "$resultName.json") -Force
        Remove-Item -Path $processing -Force -ErrorAction SilentlyContinue
        Write-Host ("[{0:HH:mm:ss}] Finished {1}: success={2}" -f (Get-Date), $resultName, $summary.success)
    }
    Start-Sleep -Seconds $PollSeconds
}
