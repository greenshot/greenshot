# Verifies the files of a Greenshot installation against checksum.SHA256 (run it in the installation directory).
# Files of a plugin that is not installed (its directory under Plugins does not exist) are skipped.
Get-Content .\checksum.SHA256 | ForEach-Object {
  $parts = $_ -split '  '; $expected = $parts[0]; $file = $parts[1]
  $segments = $file -split '/'
  if ($segments.Count -ge 3 -and $segments[0] -eq 'Plugins' -and -not (Test-Path (Join-Path 'Plugins' $segments[1]))) {
    return
  }
  if (Test-Path $file) {
    $actual = (Get-FileHash $file -Algorithm SHA256).Hash
    if ($actual -eq $expected) {
        Write-Host "OK: $file" -ForegroundColor Green
    }
    else {
        Write-Warning "FAILED: $file (Hash mismatch!)"
    }
  } else {
    Write-Warning "MISSING: $file"
  }
}
