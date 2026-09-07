Get-Content .\checksum.SHA256 | ForEach-Object {
  $parts = $_ -split '  '; $expected = $parts[0]; $file = $parts[1]
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