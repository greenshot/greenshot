param(
    [string]$SolutionDir,
    [string]$Configuration,
    [string]$TargetFramework,
    [string]$SbomVersion
)

$csproj = Join-Path $SolutionDir "Greenshot\Greenshot.csproj"
$csprojSbom = Join-Path $SolutionDir "Greenshot\Greenshot-sbom.csproj"

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
    Write-Host "Creating temporary copy of the project file for SBOM generation..."
    Copy-Item $csproj $csprojSbom -Force -ErrorAction Stop
    
    Write-Host "Removing ProjectReference to Greenshot.BuildTasks from temporary project..."
    [xml]$xml = Get-Content $csprojSbom -Raw -ErrorAction Stop
    $nodes = $xml.SelectNodes("//*[local-name()='ProjectReference'][contains(@Include, 'Greenshot.BuildTasks.csproj')]")
    foreach ($node in $nodes) {
        [void]$node.ParentNode.RemoveChild($node)
    }
    $xml.Save($csprojSbom)
    
    $outputPath = Join-Path $SolutionDir "Greenshot\bin\$Configuration\$TargetFramework"
    
    Write-Host "Restoring dotnet tools..."
    Run-Command { dotnet tool restore }
    
    Write-Host "Generating CycloneDX JSON SBOM..."
    Run-Command { dotnet dotnet-CycloneDX $csprojSbom --output-format Json --output $outputPath --set-name Greenshot --exclude-test-projects --exclude-dev }
    
    Write-Host "Generating CycloneDX XML SBOM..."
    Run-Command { dotnet dotnet-CycloneDX $csprojSbom --output-format Xml --output $outputPath --set-name Greenshot --exclude-test-projects --exclude-dev }
    
    Write-Host "Generating SPDX SBOM via sbom-tool..."
    Run-Command { dotnet sbom-tool generate -b $outputPath -bc $SolutionDir -pn "Greenshot" -pv $SbomVersion -ps "Greenshot" -nsb "https://github.com/greenshot/greenshot" -pm true -li true -cd "--DirectoryExclusionList **/Greenshot.BuildTasks/**" }
}
finally {
    if (Test-Path $csprojSbom) {
        Write-Host "Cleaning up temporary project file..."
        Remove-Item $csprojSbom -Force
    }
}
