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
$ArtifactsPath = "$RepoPath\artifacts"
$SolutionFile = "$RepoPath\src\Greenshot.sln"

# Secrets - Replace these with your actual values
#$Env:Box13_ClientId = "your_box13_client_id"
#$Env:Box13_ClientSecret = "your_box13_client_secret"
#$Env:DropBox13_ClientId = "your_dropbox13_client_id"
#$Env:DropBox13_ClientSecret = "your_dropbox13_client_secret"
#$Env:Flickr_ClientId = "your_flickr_client_id"
#$Env:Flickr_ClientSecret = "your_flickr_client_secret"
#$Env:Imgur13_ClientId = "your_imgur13_client_id"
#$Env:Imgur13_ClientSecret = "your_imgur13_client_secret"
#$Env:Photobucket_ClientId = "your_photobucket_client_id" 
#$Env:Photobucket_ClientSecret = "your_photobucket_client_secret"
#$Env:Picasa_ClientId = "your_picasa_client_id"
#$Env:Picasa_ClientSecret = "your_picasa_client_secret"

# Step 0: Update Local Repository
git pull

# Step 1: Restore NuGet Packages
Write-Host "Restoring NuGet packages..."
msbuild "$SolutionFile" /p:Configuration=Release /restore /t:PrepareForBuild
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to restore NuGet packages."
    exit $LASTEXITCODE
}

# Step 2: Build and Package
Write-Host "Building and packaging the solution..."
msbuild "$SolutionFile" /p:Configuration=Release /t:Rebuild /v:normal
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed."
    exit $LASTEXITCODE
}

# Step 3: Copy Installer Files
Write-Host "Copying installer files..."
if (-not (Test-Path $ArtifactsPath)) {
    New-Item -ItemType Directory -Force -Path $ArtifactsPath
}
Copy-Item "$RepoPath\installer\Greenshot-INSTALLER-*.exe" -Destination $ArtifactsPath -Force
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to copy installer files."
    exit $LASTEXITCODE
}

# Step 4: Extract Version from File Name
Write-Host "Extracting version from installer file name..."
$InstallerFile = Get-ChildItem $ArtifactsPath -Filter "Greenshot-INSTALLER-*.exe" | Select-Object -Last 1
if (-not $InstallerFile) {
    Write-Error "No matching installer file found in '$ArtifactsPath'."
    exit 1
}

if ($InstallerFile.Name -match "Greenshot-INSTALLER-([\d\.]+).*\.exe") {
    $Version = $matches[1]
    Write-Host "Extracted version: $Version"
} else {
    Write-Error "Version number could not be extracted from file name: $($InstallerFile.Name)"
    exit 1
}

# Step 5: Create Git Tag
Write-Host "Creating Git tag..."
cd $RepoPath
#git config user.name "local-script"
#git config user.email "local-script@example.com"
git tag -a "v$Version" -m "v$Version"
git push origin "v$Version"
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to create Git tag."
    exit $LASTEXITCODE
}

# Step 6: Create GitHub Release
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
    -Uri "https://api.github.com/repos/jklingen/greenshot/releases" `
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

# Step 7: Upload .exe File to Release
Write-Host "Uploading .exe file to GitHub release..."
$ExeFilePath = "$ArtifactsPath\$($InstallerFile.Name)"
if (-Not (Test-Path $ExeFilePath)) {
    Write-Error "Built .exe file not found: $ExeFilePath"
    exit 1
}

# GitHub API for uploading release assets
$UploadUrl = $ReleaseResponse.upload_url -replace "{.*}", ""

# Upload the file
$FileHeaders = @{
    Authorization = "Bearer $ReleaseToken"
    ContentType   = "application/octet-stream"
}
$FileName = [System.IO.Path]::GetFileName($ExeFilePath)

Invoke-RestMethod `
    -Uri "$($UploadUrl)?name=$FileName" `
    -Method POST `
    -Headers $FileHeaders `
    -InFile $ExeFilePath `
    -ContentType "application/octet-stream"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to upload .exe file to release."
    exit $LASTEXITCODE
}

Write-Host "File uploaded successfully: $FileName"

# Step 7: Trigger GitHub Pages Rebuild
#Write-Host "Triggering GitHub Pages rebuild..."
#Invoke-RestMethod `
#    -Uri "https://api.github.com/repos/jklingen/greenshot/pages/builds" `
#    -Method POST `
#    -Headers $Headers
#if ($LASTEXITCODE -ne 0) {
#    Write-Error "Failed to trigger GitHub Pages rebuild."
#    exit $LASTEXITCODE
#}
#
#Write-Host "GitHub Pages rebuild triggered successfully."