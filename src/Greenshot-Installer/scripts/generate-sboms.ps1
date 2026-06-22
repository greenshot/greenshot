param(
    [string]$SolutionDir,
    [string]$Configuration,
    [string]$TargetFramework,
    [string]$SbomVersion
)

$sln = Join-Path $SolutionDir "Greenshot.sln"
$csproj = Join-Path $SolutionDir "Greenshot\Greenshot.csproj"
$slnBak = "$sln.bak"
$csprojBak = "$csproj.bak"

function Run-Command {
    param(
        [scriptblock]$Command
    )
    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "External command failed with exit code $($LASTEXITCODE): $Command"
    }
}

Write-Host "Backing up solution and project files..."
if (Test-Path $slnBak) {
    Write-Host "Restoring solution from existing backup to ensure clean state..."
    Copy-Item $slnBak $sln -Force -ErrorAction Stop
} else {
    Copy-Item $sln $slnBak -Force -ErrorAction Stop
}
if (Test-Path $csprojBak) {
    Write-Host "Restoring project from existing backup to ensure clean state..."
    Copy-Item $csprojBak $csproj -Force -ErrorAction Stop
} else {
    Copy-Item $csproj $csprojBak -Force -ErrorAction Stop
}

try {
    Write-Host "Temporarily removing Greenshot.BuildTasks from solution..."
    Run-Command { dotnet sln $sln remove (Join-Path $SolutionDir "Greenshot.BuildTasks\Greenshot.BuildTasks.csproj") }
    
    Write-Host "Temporarily removing ProjectReference to Greenshot.BuildTasks..."
    [xml]$xml = Get-Content $csproj -Raw -ErrorAction Stop
    $nodes = $xml.SelectNodes("//*[local-name()='ProjectReference'][contains(@Include, 'Greenshot.BuildTasks.csproj')]")
    foreach ($node in $nodes) {
        [void]$node.ParentNode.RemoveChild($node)
    }
    $xml.Save($csproj)
    
    $outputPath = Join-Path $SolutionDir "Greenshot\bin\$Configuration\$TargetFramework"
    
    Write-Host "Restoring dotnet tools..."
    Run-Command { dotnet tool restore }
    
    Write-Host "Generating CycloneDX JSON SBOM..."
    Run-Command { dotnet dotnet-CycloneDX $sln --output-format Json --output $outputPath --set-name Greenshot --exclude-test-projects --exclude-dev }
    
    Write-Host "Generating CycloneDX XML SBOM..."
    Run-Command { dotnet dotnet-CycloneDX $sln --output-format Xml --output $outputPath --set-name Greenshot --exclude-test-projects --exclude-dev }
    
    Write-Host "Generating SPDX SBOM via sbom-tool..."
    Run-Command { dotnet sbom-tool generate -b $outputPath -bc $SolutionDir -pn "Greenshot" -pv $SbomVersion -ps "Greenshot" -nsb "https://github.com/greenshot/greenshot" -pm true -li true -cd "--DirectoryExclusionList **/Greenshot.BuildTasks/**" }
}
finally {
    Write-Host "Restoring backups..."
    if (Test-Path $slnBak) {
        Move-Item $slnBak $sln -Force
    }
    if (Test-Path $csprojBak) {
        Move-Item $csprojBak $csproj -Force
    }
}
