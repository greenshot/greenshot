# USAGE
# * Enable script execution in Powershell: 'Set-ExecutionPolicy RemoteSigned'
# * Create a GitHub personal access token (PAT) for greenshot repository
#   * user must be owner of the repository
#   * token needs read and write permissions ""for Contents"" and ""Pages""
# * Execute the script and paste your token

# Prompt the user to securely input the Github token
$SecureToken = Read-Host "Please enter your GitHub personal access token" -AsSecureString
$ReleaseToken = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureToken))

# Variables
$RepoPath = "."  # Replace with your local repo path
$BuildArtifactsPath = "$RepoPath\src\Greenshot\bin\Release\net472"
$ArtifactsPath = "$RepoPath\artifacts"
$PortableFilesPath = "$ArtifactsPath\portable-files"
$SolutionFile = "$RepoPath\src\Greenshot.sln"

# Clear Artifacts Directory
Remove-Item -Path "$ArtifactsPath\*" -Recurse -Force

# Update Local Repository
git pull

# Restore NuGet Packages
Write-Host "Restoring NuGet packages..."
msbuild "$SolutionFile" /p:Configuration=Release /restore /t:PrepareForBuild
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to restore NuGet packages."
    exit $LASTEXITCODE
}

# Build and Package
Write-Host "Building and packaging the solution..."
msbuild "$SolutionFile" /p:Configuration=Release /t:Rebuild /v:normal
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed."
    exit $LASTEXITCODE
}


# Extract Version from File Name
Write-Host "Extracting version from installer file name..."
$InstallerFile = Get-ChildItem "$RepoPath\installer" -Filter "Greenshot-INSTALLER-*.exe" | Select-Object -Last 1
if (-not $InstallerFile) {
    Write-Error "No matching installer file found in '$RepoPath\installer'."
    exit 1
}
if ($InstallerFile.Name -match "Greenshot-INSTALLER-([\d\.]+).*\.exe") {
    $Version = $matches[1]
    Write-Host "Extracted version: $Version"
} else {
    Write-Error "Version number could not be extracted from file name: $($InstallerFile.Name)"
    exit 1
}

# Copy Installer Files
Write-Host "Copying installer files..."
$ExeArtifactPath = "$ArtifactsPath\Greenshot-INSTALLER-$Version-RELEASE.exe"
if (-not (Test-Path $ArtifactsPath)) {
    New-Item -ItemType Directory -Force -Path $ArtifactsPath
}
Copy-Item "$RepoPath\installer\Greenshot-INSTALLER-*.exe" -Destination $ExeArtifactPath -Force
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to copy installer files."
    exit $LASTEXITCODE
}

# Prepare ZIP Archive
Write-Host "Preparing build artifacts for ZIP archive..."
if (-not (Test-Path $PortableFilesPath)) {
    New-Item -ItemType Directory -Force -Path $PortableFilesPath
}
./prepare-portable.ps1 -RepositoryRootPath . -BuildArtifactsPath $BuildArtifactsPath -OutputPath $PortableFilesPath
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to copy installer files."
    exit $LASTEXITCODE
}

# Create ZIP Archive
Write-Host "Creating ZIP archive..."
$ZipArtifactPath = "$ArtifactsPath\Greenshot-PORTABLE-$Version-RELEASE.zip"
Compress-Archive -Path "$PortableFilesPath/*" -DestinationPath $ZipArtifactPath -Force

# Create Git Tag
Write-Host "Creating Git tag..."
cd $RepoPath
git tag -a "v$Version" -m "v$Version"
git push origin "v$Version"
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to create Git tag."
    exit $LASTEXITCODE
}

# Create GitHub Release
Write-Host "Creating GitHub release..."
$Headers = @{
    Authorization = "Bearer $ReleaseToken"
    Accept        = "application/vnd.github+json"
}
$ReleaseData = @{
    tag_name              = "v$Version"
    name                  = "Greenshot $Version unstable"
    body                  = "Pre-release of Greenshot $Version."
    draft                 = $true
    prerelease            = $true
    generate_release_notes = $true
}
$ReleaseResponse = Invoke-RestMethod `
    -Uri "https://api.github.com/repos/greenshot/greenshot/releases" `
    -Method POST `
    -Headers $Headers `
    -Body (ConvertTo-Json $ReleaseData -Depth 10)

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to create GitHub release."
    exit $LASTEXITCODE
}

Write-Host "Release created successfully."

# Get the release ID from the response
$ReleaseId = $ReleaseResponse.id
Write-Host "Release ID: $ReleaseId"

# Upload .exe File to Release
Write-Host "Uploading .exe file to GitHub release..."

$FilesToUpload = @(
    $ExeArtifactPath,
    $ZipArtifactPath
)

foreach ($file in $FilesToUpload) {
	if (-Not (Test-Path $file)) {
		Write-Error "Artifact not found: $file"
		exit 1
	}

	# GitHub API for uploading release assets
	$UploadUrl = $ReleaseResponse.upload_url -replace "{.*}", ""

	# Upload the file
	$FileHeaders = @{
		Authorization = "Bearer $ReleaseToken"
		ContentType   = "application/octet-stream"
	}
	$FileName = [System.IO.Path]::GetFileName($file)

	Invoke-RestMethod `
		-Uri "$($UploadUrl)?name=$FileName" `
		-Method POST `
		-Headers $FileHeaders `
		-InFile $file `
		-ContentType "application/octet-stream"

	if ($LASTEXITCODE -ne 0) {
		Write-Error "Failed to upload $FileName to release."
		exit $LASTEXITCODE
	}

	Write-Host "File uploaded successfully: $FileName"
}
