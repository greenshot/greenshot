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

Write-Host "Backing up solution and project files..."
Copy-Item $sln $slnBak -Force
Copy-Item $csproj $csprojBak -Force

try {
    Write-Host "Temporarily removing Greenshot.BuildTasks from solution..."
    dotnet sln $sln remove (Join-Path $SolutionDir "Greenshot.BuildTasks\Greenshot.BuildTasks.csproj")
    
    Write-Host "Temporarily removing ProjectReference to Greenshot.BuildTasks..."
    (Get-Content $csproj) -replace '<ProjectReference Include="[^"]*Greenshot\.BuildTasks\.csproj"[^>]*/>', '' | Set-Content $csproj
    
    $outputPath = Join-Path $SolutionDir "Greenshot\bin\$Configuration\$TargetFramework"
    
    Write-Host "Restoring dotnet tools..."
    dotnet tool restore
    
    Write-Host "Generating CycloneDX JSON SBOM..."
    dotnet dotnet-cyclonedx $sln --output-format Json --output $outputPath --set-name Greenshot --exclude-test-projects --exclude-dev
    
    Write-Host "Generating CycloneDX XML SBOM..."
    dotnet dotnet-cyclonedx $sln --output-format Xml --output $outputPath --set-name Greenshot --exclude-test-projects --exclude-dev
    
    Write-Host "Generating SPDX SBOM via sbom-tool..."
    dotnet sbom-tool generate -b $outputPath -bc $SolutionDir -pn "Greenshot" -pv $SbomVersion -ps "Greenshot" -nsb "https://github.com/greenshot/greenshot" -pm true -li true -cd "--DirectoryExclusionList **/Greenshot.BuildTasks/**"
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
