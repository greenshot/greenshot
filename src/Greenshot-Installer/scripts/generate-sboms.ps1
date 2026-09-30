param(
    [string]$SolutionDir,
    [string]$Configuration,
    [string]$TargetFramework,
    [string]$SbomVersion
)

$csproj = Join-Path $SolutionDir "Greenshot\Greenshot.csproj"
$csprojBackup = Join-Path $SolutionDir "Greenshot\Greenshot.csproj.sbom-backup"
$greenshotProjectDir = Join-Path $SolutionDir "Greenshot"

function Run-Command {
    param(
        [scriptblock]$Command
    )
    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "External command failed with exit code $($LASTEXITCODE): $Command"
    }
}

try {
    Write-Host "Backing up Greenshot project file for SBOM generation..."
    Copy-Item $csproj $csprojBackup -Force -ErrorAction Stop
    
    Write-Host "Temporarily removing ProjectReference to Greenshot.BuildTasks..."
    $xml = New-Object System.Xml.XmlDocument
    $xml.Load($csproj)
    $nodes = $xml.SelectNodes("//*[local-name()='ProjectReference'][contains(@Include, 'Greenshot.BuildTasks.csproj')]")
    foreach ($node in $nodes) {
        [void]$node.ParentNode.RemoveChild($node)
    }
    $xml.Save($csproj)
    
    $outputPath = Join-Path $SolutionDir "Greenshot\bin\$Configuration\$TargetFramework"
    
    Write-Host "Restoring dotnet tools..."
    Run-Command { dotnet tool restore }
    
    Write-Host "Generating CycloneDX JSON SBOM..."
    Run-Command { dotnet dotnet-CycloneDX $csproj --output-format Json --output $outputPath --set-name Greenshot --exclude-test-projects --exclude-dev }
    
    Write-Host "Generating CycloneDX XML SBOM..."
    Run-Command { dotnet dotnet-CycloneDX $csproj --output-format Xml --output $outputPath --set-name Greenshot --exclude-test-projects --exclude-dev }
    
    Write-Host "Generating SPDX SBOM via sbom-tool..."
    try {
        $tempManifestDir = Join-Path ([System.IO.Path]::GetTempPath()) ([System.Guid]::NewGuid().ToString())
        New-Item -ItemType Directory -Path $tempManifestDir -Force | Out-Null
        Run-Command {
            dotnet sbom-tool generate -b $outputPath -bc $greenshotProjectDir -m $tempManifestDir -pn "Greenshot" -pv $SbomVersion -ps "Greenshot" -nsb "https://github.com/greenshot/greenshot" -pm true -li true
        }
        # Move the generated _manifest folder into the target build output directory
        $generatedManifestSource = Join-Path $tempManifestDir "_manifest"
        $destination = Join-Path $outputPath "_manifest"

        if (Test-Path $destination) {
            Remove-Item -Path $destination -Recurse -Force
        }

        Move-Item -Path $generatedManifestSource -Destination $outputPath -Force
    }
    finally {
        # Clean up temporary scratch space
        if (Test-Path $tempManifestDir) {
            Remove-Item -Path $tempManifestDir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}
finally {
    if (Test-Path $csprojBackup) {
        Write-Host "Restoring original Greenshot project file..."
        Copy-Item $csprojBackup $csproj -Force -ErrorAction Stop
        Remove-Item $csprojBackup -Force
    }
}
