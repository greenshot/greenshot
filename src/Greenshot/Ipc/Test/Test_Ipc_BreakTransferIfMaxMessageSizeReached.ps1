# This script tests the named pipes created by Greenshot processes.
# It checks if the maximum message size limit (1MB) is enforced.
# It attempts to send 2MB of data through each pipe, expecting the transfer to break.

# The test needs a running Greenshot instance

$PipeNamePrefix = "Greenshot_Pipe_ID"
$GreenshotProcessName = "Greenshot"

# Get all running Greenshot processes
$greenshotProcesses = Get-Process -Name $GreenshotProcessName -ErrorAction SilentlyContinue

if (-not $greenshotProcesses) {
    Write-Host "[FAIL] No Greenshot processes found." -ForegroundColor Red
    return
}

# Create 2MB dummy data
$dummyData = New-Object byte[] (2MB)

foreach ($proc in $greenshotProcesses) {
    $pipeName = "$PipeNamePrefix$($proc.Id)"
    $serverName = "."

    Write-Host "== Testing pipe '$pipeName' for process PID=$($proc.Id) ==" -ForegroundColor Yellow

    $pipe = New-Object System.IO.Pipes.NamedPipeClientStream(
        $serverName,
        $pipeName,
        [System.IO.Pipes.PipeDirection]::Out
    )

    try {
        # Try to connect (timeout 5s)
        $pipe.Connect(5000)
        Write-Host "[INFO] Connected to $pipeName." -ForegroundColor Yellow

        try {
            # Send 2MB -> expected to break due to 1MB limit
            $pipe.Write($dummyData, 0, $dummyData.Length)
            $pipe.Flush()
            Write-Host "[FAIL] Transfer completed without interruption (limit not enforced)." -ForegroundColor Red
        }
        catch {
            Write-Host "[PASS] Pipe broke during transfer as expected: $($_.Exception.Message)" -ForegroundColor Green
        }
    }
    catch {
        Write-Host "[FAIL] Could not connect to $pipeName : $($_.Exception.Message)" -ForegroundColor Red
    }
    finally {
        $pipe.Dispose()
    }

    Write-Host ""
}