<#
.SYNOPSIS
    Signs every exe and dll in the output directory (plugins included) that has no signature yet.

.DESCRIPTION
    Smart App Control blocks unsigned files that Microsoft doesn't know yet, which every new release has
    (FileLoadException, HRESULT 0x800711C7). Files that are already signed (Microsoft) keep their signature.
    Run it after prune-plugin-files.ps1 and before the SBOM and checksum.SHA256 are made, so they describe the signed files.
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputDir,
    [Parameter(Mandatory = $true)]
    [string]$Thumbprint
)

Get-ChildItem -Path $OutputDir -Recurse -Include *.dll, *.exe |
    Where-Object { (Get-AuthenticodeSignature $_.FullName).Status -eq 'NotSigned' } |
    ForEach-Object {
        signtool.exe sign /sha1 $Thumbprint /tr http://time.certum.pl /td sha256 /fd sha256 /v $_.FullName
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }
    }
