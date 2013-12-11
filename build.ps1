################################################################
# Greenshot BUILD script, written for the Windows Power Shell
# Assumes the installation of Microsoft .NET Framework 4.5
################################################################
# Greenshot - a free and open source screenshot tool
# Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom
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

Add-Type -Assembly System.ServiceModel.Web,System.Runtime.Serialization

# Collect GIT information
$gitversion = git describe --long
$gittag = $gitversion -replace '-.*',''
$commitversion = $gitversion -replace ($gittag + '-'),'' -replace '-.*',''
$githash = $gitversion -replace '.*-',''
$version = $gittag + '.' + $commitversion
$detailversion = $version + '-' + $githash

	
Function WaitForKey {
	$x = $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
	return
}

# Create a MD5 string for the supplied filename
Function MD5($filename) {
	$fileStream = new-object -TypeName System.IO.FileStream -ArgumentList "$filename", "Open", "Read", "Read"
	$MD5CryptoServiceProvider = new-object -TypeName System.Security.Cryptography.MD5CryptoServiceProvider
	$hash = $MD5CryptoServiceProvider.ComputeHash($fileStream)
	return [System.BitConverter]::ToString($hash) -replace "-", ""
}

Function Convert-JsonToXml([string]$json) {
	$bytes = [byte[]][char[]]$json
	$quotas = [System.Xml.XmlDictionaryReaderQuotas]::Max
	$jsonReader = [System.Runtime.Serialization.Json.JsonReaderWriterFactory]::CreateJsonReader($bytes,$quotas)
	try {
		$xml = new-object System.Xml.XmlDocument
		$xml.Load($jsonReader)
	$xml
	} finally {
		$jsonReader.Close()
	}
}

# Create releasenotes for the commits
Function ReleaseNotes {
	$gitlog = (git shortlog "$gittag..HEAD")
	$jiras = $gitlog | foreach { [regex]::match($_,'[a-zA-Z0-9]+-[0-9]{1,5}').value } | Where {$_ -match '\S'} | sort-object| select-object $_ -unique
	
	$proxy = [System.Net.WebRequest]::GetSystemWebProxy()
	$proxy.Credentials = [System.Net.CredentialCache]::DefaultCredentials

	$wc = new-object system.net.WebClient
	$wc.proxy = $proxy

	$jiras | foreach {
		$jira = $_
		#echo "https://greenshot.atlassian.net/browse/$_"
		$jiraJson = $wc.DownloadString("https://greenshot.atlassian.net/rest/api/2/issue/$jira")
		$xml = Convert-JsonToXml $jiraJson
		$summary = $xml.root.fields.summary."#text"
		echo "$jira : $summary"
	}
}

# Fill the templates
Function FillTemplates {
	echo "Git $gittag - $githash commit $commitversion"
	
	$releaseNotes = ReleaseNotes
	Get-ChildItem . -recurse *.template | 
		foreach {
			$oldfile = $_.FullName
			$newfile = $_.FullName -replace '\.template',''
			echo "Modifying file : $oldfile to $newfile"
			(Get-Content $oldfile) -replace "\@GITVERSION\@", $version -replace "\@GITDETAILVERSION\@", $detailversion -replace "\@RELEASENOTES\@", $releaseNotes | Set-Content $newfile -encoding UTF8
		}
}

Function Build {
	$msBuild = "C:\Windows\Microsoft.NET\Framework64\v3.5\MSBuild"
	if (-not (Test-Path("$msBuild"))) {
		$msBuild = "C:\Windows\Microsoft.NET\Framework\v3.5\MSBuild"
	}
	$parameters = @('Greenshot\Greenshot.sln', '/t:Clean;Build', '/p:Configuration="Release"', '/p:Platform="Any CPU"')
	$buildOutput = "$(get-location)\build.log"
	echo "Calling: $msBuild $parameters"
	$buildResult = Start-Process -wait -PassThru "$msBuild" -ArgumentList $parameters -NoNewWindow -RedirectStandardOutput $buildOutput
	if ($buildResult.ExitCode -ne 0) {
		echo "An error occured, please check $BuildOutput for errors!"
		exit -1
	}
	return
}

Function MD5Checksums {
	echo "MD5 Checksums:"
	$currentMD5 = MD5("$(get-location)\Greenshot\bin\Release\Greenshot.exe")
	echo "Greenshot.exe : $currentMD5"
	$currentMD5 = MD5("$(get-location)\Greenshot\bin\Release\GreenshotPlugin.dll")
	echo "GreenshotPlugin.dll : $currentMD5"
}

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

	$INCLUDE=@("*.gsp", "*.dll", "*.exe")
	Get-ChildItem -Path "$sourcebase\Plugins\" -Recurse -Include $INCLUDE | foreach {
		$path = $_.fullname -replace ".*\\Plugins\\", "$destbase\portabletmp\App\Greenshot\Plugins\"
		New-Item -ItemType File -Path "$path" -Force | Out-Null
		Copy-Item -Path $_ -Destination "$path" -Force
	}
	
	$INCLUDE=@("help-*.html","language-*.xml")
	Get-ChildItem -Path "$sourcebase\Languages\" -Recurse -Include $INCLUDE | foreach {
		$path = $_.fullname -replace ".*\\Languages\\", "$destbase\portabletmp\App\Greenshot\Languages\"
		New-Item -ItemType File -Path "$path" -Force | Out-Null
		Copy-Item -Path $_ -Destination "$path" -Force
	}
	Copy-Item -Path "$sourcebase\Languages\Plugins\" -Destination "$destbase\portabletmp\App\Greenshot\Languages\Plugins\" -Recurse
	
	@( "$sourcebase\checksum.MD5",
		"$sourcebase\Greenshot.exe.config",
		"$sourcebase\GreenshotPlugin.dll",
		"$sourcebase\GreenshotEditor.dll",
		"$destbase\additional_files\*.txt" ) | foreach { Copy-Item $_ "$destbase\portabletmp\App\Greenshot\" }

	Copy-Item -Path "$sourcebase\Languages\help-en-US.html" -Destination "$destbase\portabletmp\help.html"

	Copy-Item -Path "$sourcebase\Greenshot.exe" -Destination "$destbase\portabletmp"

	Copy-Item -Path "$destbase\appinfo.ini" -Destination "$destbase\portabletmp\App\AppInfo\appinfo.ini"
		
	$portableOutput = "$(get-location)\portable"
	$portableInstaller = "$(get-location)\greenshot\tools\PortableApps.comInstaller\PortableApps.comInstaller.exe"
	$arguments = @("$destbase\portabletmp")
	echo "Starting $portableInstaller $arguments"
	$portableResult = Start-Process -wait -PassThru "$portableInstaller" -ArgumentList $arguments -NoNewWindow -RedirectStandardOutput "$portableOutput.log" -RedirectStandardError "$portableOutput.error"
	if ($portableResult.ExitCode -ne 0) {
		echo "An error occured, please check $portableOutput.log and $portableOutput.error for errors!"
		exit -1
	}
	Remove-Item "$destbase\portabletmp" -recurse -Confirm:$false
	return
}

Function PackageZip {
	$sourcebase = "$(get-location)\Greenshot\bin\Release"
	$destbase = "$(get-location)\Greenshot\releases"
	$destinstaller = "$destbase\NO-INSTALLER"

	# Only remove the zip we are going to create, to prevent adding but keeping the history
	if (Test-Path  ("$destbase\Greenshot-NO-INSTALLER-$version.zip")) {
		Remove-Item "$destbase\Greenshot-NO-INSTALLER-$version.zip" -Confirm:$false
	}
	# Remove the directory to create the files in
	if (Test-Path  ("$destinstaller")) {
		Remove-Item "$destinstaller" -recurse -Confirm:$false
	}
	New-Item -ItemType directory -Path "$destinstaller" | Out-Null

	echo ";dummy config, used to make greenshot store the configuration in this directory" | Set-Content "$destinstaller\greenshot.ini" -encoding UTF8
	echo ";In this file you should add your default settings" | Set-Content "$destinstaller\greenshot-defaults.ini" -encoding UTF8
	echo ";In this file you should add your fixed settings" | Set-Content "$destinstaller\greenshot-fixed.ini" -encoding UTF8

	$INCLUDE=@("*.gsp", "*.dll", "*.exe")
	Get-ChildItem -Path "$sourcebase\Plugins\" -Recurse -Include $INCLUDE | foreach {
		$path = $_.fullname -replace ".*\\Plugins\\", "$destinstaller\Plugins\"
		New-Item -ItemType File -Path "$path" -Force | Out-Null
		Copy-Item -Path $_ -Destination "$path" -Force
	}
	
	$INCLUDE=@("help-*.html","language-*.xml")
	Get-ChildItem -Path "$sourcebase\Languages\" -Recurse -Include $INCLUDE | foreach {
		$path = $_.fullname -replace ".*\\Languages\\", "$destinstaller\Languages\"
		New-Item -ItemType File -Path "$path" -Force | Out-Null
		Copy-Item -Path $_ -Destination "$path" -Force
	}
	Copy-Item -Path "$sourcebase\Languages\Plugins\" -Destination "$destinstaller\Languages\Plugins\" -Recurse
	
	@( "$sourcebase\checksum.MD5",
		"$sourcebase\Greenshot.exe",
		"$sourcebase\Greenshot.exe.config",
		"$sourcebase\GreenshotPlugin.dll",
		"$sourcebase\GreenshotEditor.dll",
		"$destbase\additional_files\*.txt" ) | foreach { Copy-Item $_ "$destinstaller\" }

	$zipOutput = "$(get-location)\zip"
	$zip7 = "$(get-location)\greenshot\tools\7zip\7za.exe"
	$arguments = @('a', '-mx9', '-tzip', '-r', "$destbase\Greenshot-NO-INSTALLER-$version.zip", "$destinstaller\*")
	echo "Starting $zip7 $arguments"
	$zipResult = Start-Process -wait -PassThru "$zip7" -ArgumentList $arguments -NoNewWindow -RedirectStandardOutput "$zipOutput.log" -RedirectStandardError "$zipOutput.error"
	if ($zipResult.ExitCode -ne 0) {
		echo "An error occured, please check $zipOutput.log and $zipOutput.error for errors!"
		exit -1
	}
	Remove-Item "$destinstaller" -recurse -Confirm:$false
	return
}

Function PackageInstaller {
	$setupOutput = "$(get-location)\setup"
	$innoSetup = "$(get-location)\greenshot\tools\innosetup\ISCC.exe"
	$innoSetupFile = "$(get-location)\greenshot\releases\innosetup\setup.iss"
	echo "Starting $innoSetup $innoSetupFile"
	$setupResult = Start-Process -wait -PassThru "$innoSetup" -ArgumentList "$innoSetupFile" -NoNewWindow -RedirectStandardOutput "$setupOutput.log" -RedirectStandardError "$setupOutput.error"
	if ($setupResult.ExitCode -ne 0) {
		echo "An error occured, please check $setupOutput.log and $setupOutput.error for errors!"
		exit -1
	}
	return
}

FillTemplates

Build

echo "Generating MD5"

MD5Checksums | Set-Content "$(get-location)\Greenshot\bin\Release\checksum.MD5" -encoding UTF8

echo "Generating Installer"
PackageInstaller

echo "Generating ZIP"
PackageZip

echo "Generating Portable"
PackagePortable

echo "Ready, press any key to continue!"

WaitForKey


# SIG # Begin signature block
# MIIEtAYJKoZIhvcNAQcCoIIEpTCCBKECAQExCzAJBgUrDgMCGgUAMGkGCisGAQQB
# gjcCAQSgWzBZMDQGCisGAQQBgjcCAR4wJgIDAQAABBAfzDtgWUsITrck0sYpfvNR
# AgEAAgEAAgEAAgEAAgEAMCEwCQYFKw4DAhoFAAQUrj4gCFRpu7VRoh4bzyfOF1yt
# w+6gggK+MIICujCCAaagAwIBAgIQyoRJHMJDVbNFmmfObt+Y4DAJBgUrDgMCHQUA
# MCwxKjAoBgNVBAMTIVBvd2VyU2hlbGwgTG9jYWwgQ2VydGlmaWNhdGUgUm9vdDAe
# Fw0xMzExMjYxOTMxMTVaFw0zOTEyMzEyMzU5NTlaMBoxGDAWBgNVBAMTD1Bvd2Vy
# U2hlbGwgVXNlcjCBnzANBgkqhkiG9w0BAQEFAAOBjQAwgYkCgYEA0SEsL7kNLoYA
# rMLe99Tf1SFA6beQsB+fPSpNrL+DtmsZpAs/TeQH+PVary/DaBNoIqarcdXjGVQL
# Qti2kBijaUtEyyz/knVXWBqgHrgWg5eVjMpH8qZMANdEvrQLGNFq6WR8MOGN6RsA
# jbaNU21u5Jc1CfYlJBYeIAB4q2oTyskCAwEAAaN2MHQwEwYDVR0lBAwwCgYIKwYB
# BQUHAwMwXQYDVR0BBFYwVIAQbri48VHBMqk4a9t3MsaQeqEuMCwxKjAoBgNVBAMT
# IVBvd2VyU2hlbGwgTG9jYWwgQ2VydGlmaWNhdGUgUm9vdIIQczTeDT/eHolM3f6j
# E3BklzAJBgUrDgMCHQUAA4IBAQCVP9YdhOKo4sKWtXNJcMPHjXdkDkykDWhxgcyy
# J1Hnol7b38EF//6RxN59cecywzD4IuZGnwLyIzcDMGiLfjq88EwzsiCOkehNbZPW
# ZICftFPIqUISGJMNmY743IVSHslx+gx8ESgMjTFnXbbRDvic7+9/G8Wa6uKPi/1S
# GJH4DqHGCuPWYZzufElHBztSSt6QprjJp3oaJEHkLy3luZIvZ0Fe53ZO1tjyX/TZ
# SArUpzoFWLG1SqiFqI1oSAhHsn10u/ZtvBIQgM19jXKS5/ER8/FAvJz+D5aB4k4I
# DBoedHwxDT9Sdres42t+pjP86nS00FMSLWBlsNErcxxTV7hFMYIBYDCCAVwCAQEw
# QDAsMSowKAYDVQQDEyFQb3dlclNoZWxsIExvY2FsIENlcnRpZmljYXRlIFJvb3QC
# EMqESRzCQ1WzRZpnzm7fmOAwCQYFKw4DAhoFAKB4MBgGCisGAQQBgjcCAQwxCjAI
# oAKAAKECgAAwGQYJKoZIhvcNAQkDMQwGCisGAQQBgjcCAQQwHAYKKwYBBAGCNwIB
# CzEOMAwGCisGAQQBgjcCARUwIwYJKoZIhvcNAQkEMRYEFH5N3rkoNAqfwfm9+NWz
# e956GSjEMA0GCSqGSIb3DQEBAQUABIGAnNDXqL2r7NpwSzqCO8ceO9mhf1b3hlG7
# OaavA85n73okZ/+V0xrCh9no3HZGOiQop8ER+QqKcOaoePwMkEySt2lhBkCB8hyr
# kH3qaJdksld5E/m1LXm/b0A/6dmDahcn1LMDKndisayPup43K2SNCv4iewnp6hhB
# uI1p0ciFDRc=
# SIG # End signature block
