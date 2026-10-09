# Adds (or with -Remove, removes) a stale registration of Word's type library for the current user only.
# It mimics what an uninstalled or upgraded Office leaves behind: a newer minor version pointing to a missing file.
# HKCU\Software\Classes is merged into HKCR, so no admin rights are needed and nothing machine-wide changes.
param([switch]$Remove)
$typeLib = '{00020905-0000-0000-C000-000000000046}'     # Word type library
$appIid  = '{00020970-0000-0000-C000-000000000046}'     # Word _Application interface
$version = (Get-ItemProperty "Registry::HKEY_CLASSES_ROOT\Interface\$appIid\TypeLib").Version
$major = $version.Split('.')[0]
$key = "HKCU:\Software\Classes\TypeLib\$typeLib\$major.99"
if ($Remove) {
    Remove-Item $key -Recurse -ErrorAction SilentlyContinue
    Write-Host "Removed $key"
    return
}
New-Item "$key\0\win32" -Force | Out-Null
New-Item "$key\0\win64" -Force | Out-Null
Set-ItemProperty $key '(default)' 'Stale Word type library (Greenshot test)'
Set-ItemProperty "$key\0\win32" '(default)' 'C:\DoesNotExist\MSWORD.OLB'
Set-ItemProperty "$key\0\win64" '(default)' 'C:\DoesNotExist\MSWORD.OLB'
Write-Host "Added $key (Word's registered version is $version)"
