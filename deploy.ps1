################################################################
# Greenshot DEPLOY script, written for the Windows Power Shell
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

# This script needs some environment variables to work:
# $env:sourceforge_host with the hostname
# $env:sourceforge_user with the username
# $env:sourceforge_password with the password for the username
# $env:sourceforge_hostkey with the hosts key (ssh-rsa 2048 xx:xx:xx:xx:...)
# $env:sourceforge_targetpath with the target location

try {
    # Load WinSCP .NET assembly
	Add-Type -Path "Greenshot\tools\WinSCP\WinSCPnet.dll"
 
    # Setup session options
    $sessionOptions = New-Object WinSCP.SessionOptions
    $sessionOptions.Protocol = [WinSCP.Protocol]::Sftp
    $sessionOptions.HostName = $env:sourceforge_host
    $sessionOptions.UserName = $env:sourceforge_user
    $sessionOptions.Password = $env:sourceforge_password
    $sessionOptions.SshHostKeyFingerprint = $env:sourceforge_hostkey

    $session = New-Object WinSCP.Session
 
    try {
        # Connect
        $session.Open($sessionOptions)
 
        # Upload files
        $transferOptions = New-Object WinSCP.TransferOptions
        $transferOptions.TransferMode = [WinSCP.TransferMode]::Binary

		$artifactbase = "$(get-location)\Greenshot\releases"
		# The list of all the artifacts that need to be uploaded
		@(
			"$artifactbase\Greenshot-INSTALLER*.exe",
			"$artifactbase\Greenshot-NO-INSTALLER*.zip",
			"$artifactbase\Greenshot_for_PortableApps*.exe",
			"$artifactbase\additional_files\readme.txt"
		) | foreach {
			$transferResult = $session.PutFiles($_ , $env:sourceforge_targetpath, $False, $transferOptions)
	 
			# Throw on any error
			$transferResult.Check()
	 
			# Print results
			foreach ($transfer in $transferResult.Transfers) {
				Write-Host ("Upload of {0} to {1} succeeded" -f $transfer.FileName, $env:sourceforge_host)
			}
		}
    } finally {
        # Disconnect, clean up
        $session.Dispose()
    }
 
    exit 0
} catch [Exception] {
    Write-Host "Error: "$_.Exception
    exit 1
}