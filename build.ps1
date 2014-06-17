################################################################
# Greenshot BUILD script, written for the Windows Power Shell
# Assumes the installation of Microsoft .NET Framework 4.5
################################################################
# Greenshot - a free and open source screenshot tool
# Copyright (C) 2007-2014 Thomas Braun, Jens Klingen, Robin Krom
# 
# For more information see: http://getgreenshot.org/
# The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
# 
# This program is free software: you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation, either version 1 of the License, or
# (at your option) any later version.
# 
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.
# 
# You should have received a copy of the GNU General Public License
# along with this program.  If not, see <http://www.gnu.org/licenses/>.
################################################################

$version=$env:APPVEYOR_BUILD_VERSION
$gitcommit=$env:APPVEYOR_REPO_COMMIT
$gitcommit=$gitcommit.SubString(0, [math]::Min($gitcommit.Length, 6))
$detailversion=$version + '-' + $gitcommit
$release=(([version]$version).build) % 2 -eq 1
$fileversion=$version
# Add the UNSTABLE, if we aren't a release -> the build is even. (major.minor.build.revision)
if ( ! $release) {
	$fileversion=$version + "-UNSTABLE"
	$detailversion=$detailversion + " UNSTABLE"
} else {
	$detailversion=$detailversion + " Release"
}
Write-Host "Building Greenshot $detailversion"

# Create a MD5 string for the supplied filename
Function MD5($filename) {
	$fileStream = new-object -TypeName System.IO.FileStream -ArgumentList "$filename", "Open", "Read", "Read"
	$MD5CryptoServiceProvider = new-object -TypeName System.Security.Cryptography.MD5CryptoServiceProvider
	$hash = $MD5CryptoServiceProvider.ComputeHash($fileStream)
	return [System.BitConverter]::ToString($hash) -replace "-", ""
}

# Fill the templates
Function FillTemplates {
	Write-Host "Filling templates for version $detailversion`n`n"
	
	Get-ChildItem . -recurse *.template | 
		foreach {
			$oldfile = $_.FullName
			$newfile = $_.FullName -replace '\.template',''
			Write-Host "Modifying file : $oldfile to $newfile"
			# Read the file
			$template = Get-Content $oldfile
			# Create an empty array, this will contain the replaced lines
			$newtext = @()
			foreach ($line in $template) {
				$newtext += $line -replace "\@VERSION\@", $version -replace "\@DETAILVERSION\@", $detailversion -replace "\@FILEVERSION\@", $fileversion
			}
			# Write the new information to the file
			$newtext | Set-Content $newfile -encoding UTF8
		}
}

# Create the MD5 checksum file
Function MD5Checksums {
	echo "MD5 Checksums:"
	$currentMD5 = MD5("$(get-location)\Greenshot\bin\Release\Greenshot.exe")
	echo "Greenshot.exe : $currentMD5"
	$currentMD5 = MD5("$(get-location)\Greenshot\bin\Release\GreenshotPlugin.dll")
	echo "GreenshotPlugin.dll : $currentMD5"
}

# This function creates the paf.exe
Function PackagePortable {
	$sourcebase = "$(get-location)\Greenshot\bin\Release"
	$destbase = "$(get-location)\Greenshot\releases"

	# Only remove the paf we are going to create, to prevent adding but keeping the history
	if (Test-Path  ("$destbase\GreenshotPortable-$version.paf.exe")) {
		Remove-Item "$destbase\GreenshotPortable-$version.paf.exe" -Confirm:$false
	}
	# Remove the directory to create the files in
	if (Test-Path  ("$destbase\portabletmp")) {
		Remove-Item "$destbase\portabletmp" -recurse -Confirm:$false
	}
	Copy-Item -Path "$destbase\portable" -Destination "$destbase\portabletmp" -Recurse

	$INCLUDE=@("*.gsp", "*.dll", "*.exe", "*.config")
	Get-ChildItem -Path "$sourcebase\Plugins\" -Recurse -Include $INCLUDE | foreach {
		$path = $_.fullname -replace ".*\\Plugins\\", "$destbase\portabletmp\App\Greenshot\Plugins\"
		New-Item -ItemType File -Path "$path" -Force | Out-Null
		Copy-Item -Path $_ -Destination "$path" -Force
	}
	
	$INCLUDE=@("help-*.html","language-*.xml")
	Get-ChildItem -Path "$(get-location)\Greenshot\Languages\" -Recurse -Include $INCLUDE -Exclude "*installer*","*website*" | foreach {
		$path = $_.fullname -replace ".*\\Languages\\", "$destbase\portabletmp\App\Greenshot\Languages\"
		New-Item -ItemType File -Path "$path" -Force | Out-Null
		Copy-Item -Path $_ -Destination "$path" -Force
	}
	Copy-Item -Path "$sourcebase\Languages\Plugins\" -Destination "$destbase\portabletmp\App\Greenshot\Languages\Plugins\" -Recurse
	
	@( "$sourcebase\checksum.MD5",
		"$sourcebase\Greenshot.exe.config",
		"$sourcebase\GreenshotPlugin.dll",
		"$sourcebase\log4net.dll",
		"$sourcebase\log4net.xml",
		"$destbase\additional_files\*.txt" ) | foreach { Copy-Item $_ "$destbase\portabletmp\App\Greenshot\" }

	Copy-Item -Path "$sourcebase\Languages\help-en-US.html" -Destination "$destbase\portabletmp\help.html"

	Copy-Item -Path "$sourcebase\Greenshot.exe" -Destination "$destbase\portabletmp"

	Copy-Item -Path "$destbase\appinfo.ini" -Destination "$destbase\portabletmp\App\AppInfo\appinfo.ini"
		
	$portableOutput = "$(get-location)\portable"
	$portableInstaller = "$(get-location)\greenshot\tools\PortableApps.comInstaller\PortableApps.comInstaller.exe"
	$arguments = @("$destbase\portabletmp")
	Write-Host "Starting $portableInstaller $arguments"
	$portableResult = Start-Process -wait -PassThru "$portableInstaller" -ArgumentList $arguments -NoNewWindow -RedirectStandardOutput "$portableOutput.log" -RedirectStandardError "$portableOutput.error"
	Write-Host "Log output:"
	Get-Content "$portableOutput.log"| Write-Host
	if ($portableResult.ExitCode -ne 0) {
		Write-Host "Error output:"
		Get-Content "$portableOutput.error"| Write-Host
		exit -1
	}
	Start-Sleep -m 1500
	Remove-Item "$destbase\portabletmp" -Recurse -Confirm:$false
	return
}

# This function creates the .zip
Function PackageZip {
	$sourcebase = "$(get-location)\Greenshot\bin\Release"
	$destbase = "$(get-location)\Greenshot\releases"
	$destinstaller = "$destbase\NO-INSTALLER"

	# Only remove the zip we are going to create, to prevent adding but keeping the history
	if (Test-Path  ("$destbase\Greenshot-NO-INSTALLER-$fileversion.zip")) {
		Remove-Item "$destbase\Greenshot-NO-INSTALLER-$fileversion.zip" -Confirm:$false
	}
	# Remove the directory to create the files in
	if (Test-Path  ("$destinstaller")) {
		Remove-Item "$destinstaller" -recurse -Confirm:$false
	}
	New-Item -ItemType directory -Path "$destinstaller" | Out-Null

	echo ";dummy config, used to make greenshot store the configuration in this directory" | Set-Content "$destinstaller\greenshot.ini" -encoding UTF8
	echo ";In this file you should add your default settings" | Set-Content "$destinstaller\greenshot-defaults.ini" -encoding UTF8
	echo ";In this file you should add your fixed settings" | Set-Content "$destinstaller\greenshot-fixed.ini" -encoding UTF8

	$INCLUDE=@("*.gsp", "*.dll", "*.exe", "*.config")
	Get-ChildItem -Path "$sourcebase\Plugins\" -Recurse -Include $INCLUDE | foreach {
		$path = $_.fullname -replace ".*\\Plugins\\", "$destinstaller\Plugins\"
		New-Item -ItemType File -Path "$path" -Force | Out-Null
		Copy-Item -Path $_ -Destination "$path" -Force
	}
	
	$INCLUDE=@("help-*.html","language-*.xml")
	Get-ChildItem -Path "$(get-location)\Greenshot\Languages\" -Recurse -Include $INCLUDE -Exclude "*installer*","*website*" | foreach {
		$path = $_.fullname -replace ".*\\Languages\\", "$destinstaller\Languages\"
		New-Item -ItemType File -Path "$path" -Force | Out-Null
		Copy-Item -Path $_ -Destination "$path" -Force
	}
	Copy-Item -Path "$sourcebase\Languages\Plugins\" -Destination "$destinstaller\Languages\Plugins\" -Recurse
	
	@( "$sourcebase\checksum.MD5",
		"$sourcebase\Greenshot.exe",
		"$sourcebase\Greenshot.exe.config",
		"$sourcebase\GreenshotPlugin.dll",
		"$sourcebase\log4net.dll",
		"$sourcebase\log4net.xml",
		"$destbase\additional_files\*.txt" ) | foreach { Copy-Item $_ "$destinstaller\" }

	$zipOutput = "$(get-location)\zip"
	$zip7 = "$(get-location)\greenshot\tools\7zip\7za.exe"
	$arguments = @('a', '-mx9', '-tzip', '-r', "$destbase\Greenshot-NO-INSTALLER-$fileversion.zip", "$destinstaller\*")
	Write-Host "Starting $zip7 $arguments"
	$zipResult = Start-Process -wait -PassThru "$zip7" -ArgumentList $arguments -NoNewWindow -RedirectStandardOutput "$zipOutput.log" -RedirectStandardError "$zipOutput.error"
	Write-Host "Log output:"
	Get-Content "$zipOutput.log"| Write-Host
	if ($zipResult.ExitCode -ne 0) {
		Write-Host "Error output:"
		Get-Content "$zipOutput.error"| Write-Host
		exit -1
	}
	Start-Sleep -m 1500
	Remove-Item "$destinstaller" -Recurse -Confirm:$false
	return
}

# This function creates the installer
Function PackageInstaller {
	$setupOutput = "$(get-location)\setup"
	$innoSetup = "$(get-location)\greenshot\tools\innosetup\ISCC.exe"
	$innoSetupFile = "$(get-location)\greenshot\releases\innosetup\setup.iss"
	Write-Host "Starting $innoSetup $innoSetupFile"
	$setupResult = Start-Process -wait -PassThru "$innoSetup" -ArgumentList "$innoSetupFile" -NoNewWindow -RedirectStandardOutput "$setupOutput.log" -RedirectStandardError "$setupOutput.error"
	Write-Host "Log output:"
	Get-Content "$setupOutput.log"| Write-Host
	if ($setupResult.ExitCode -ne 0) {
		Write-Host "Error output:"
		Get-Content "$setupOutput.error"| Write-Host
		exit -1
	}
	return
}

FillTemplates

echo "Generating MD5"

MD5Checksums | Set-Content "$(get-location)\Greenshot\bin\Release\checksum.MD5" -encoding UTF8

echo "Generating Installer"
PackageInstaller

echo "Generating ZIP"
PackageZip

echo "Generating Portable"
PackagePortable