; Requires Windows 7, Windows Server 2003 R2 (32-bit x86), Windows Server 2003 Service Pack 2, Windows Server 2008 R2, Windows Server 2008 Service Pack 2, Windows Vista Service Pack 2, Windows XP Service Pack 3
; x86 download page: https://www.microsoft.com/en-us/download/details.aspx?id=8328
; x64 download page: https://www.microsoft.com/en-us/download/details.aspx?id=13523
; IA64 download page: https://www.microsoft.com/en-us/download/details.aspx?id=21051

[CustomMessages]
vcredist2010_title=Visual C++ 2010 SP1 Redistributable
vcredist2010_title_x64=Visual C++ 2010 SP1 64-Bit Redistributable
vcredist2010_title_ia64=Visual C++ 2010 SP1 Itanium Redistributable

en.vcredist2010_size=4.8 MB
de.vcredist2010_size=4,8 MB

en.vcredist2010_size_x64=5.4 MB
de.vcredist2010_size_x64=5,4 MB

en.vcredist2010_size_ia64=2.2 MB
de.vcredist2010_size_ia64=2,2 MB

[Code]
const
	vcredist2010_url_x86 = 'http://download.microsoft.com/download/C/6/D/C6D0FD4E-9E53-4897-9B91-836EBA2AACD3/vcredist_x86.exe';
	vcredist2010_url_x64 = 'http://download.microsoft.com/download/A/8/0/A80747C3-41BD-45DF-B505-E9710D2744E0/vcredist_x64.exe';
	vcredist2010_url_ia64 = 'http://download.microsoft.com/download/7/7/3/77332C03-CC6C-45E5-A7B6-E02504B93847/vcredist_IA64.exe';

	vcredist2010_productcode_x86 = '{F0C3E5D1-1ADE-321E-8167-68EF0DE699A5}';
	vcredist2010_productcode_x64 = '{1D8E6291-B0D5-35EC-8441-6616F567A0F7}';
	vcredist2010_productcode_ia64 = '{88C73C1C-2DE5-3B01-AFB8-B46EF4AB41CD}';

procedure vcredist2010();
begin
	if (not msiproduct(GetString(vcredist2010_productcode_x86, vcredist2010_productcode_x64, vcredist2010_productcode_ia64))) then
		AddProduct('vcredist2010' + GetArchitectureString() + '.exe',
			'/passive /norestart',
			CustomMessage('vcredist2010_title' + GetArchitectureString()),
			CustomMessage('vcredist2010_size' + GetArchitectureString()),
			GetString(vcredist2010_url_x86, vcredist2010_url_x64, vcredist2010_url_ia64),
			false, false, false);
end;

[Setup]
