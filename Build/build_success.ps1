################################################################
# Greenshot cholocatey script, written for the Windows Power Shell
# Assumes the installation of Microsoft .NET Framework 4.5
################################################################
# Greenshot - a free and open source screenshot tool
# Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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

$builddir = "$(get-location)\Build"

# This function builds & uploads the chocolatey package
Function ChocolateyPackage {
	pushd "$builddir\chocolatey"
	Try
	{
		Write-Host "Generating chocolatey package"
	    choco pack
		Write-Host "Set chocolatey key"
		choco apiKey -k $env:choco_api_key -source https://chocolatey.org/
		Write-Host "Pushing chocolatey package"
		Get-ChildItem -Recurse -Include *.nupkg | foreach {
			choco push $_.fullname
		}
	}
	Catch [system.exception]
	{
		#error logging
	}
	Finally
	{
		popd
	}
}

if ($env:build_type -eq "RELEASE") {
	Write-Host "Building & uploading chocolatey package"
	ChocolateyPackage
}
