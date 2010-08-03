// requires Windows Server 2003 Service Pack 1, Windows Server 2008, Windows Vista, Windows XP Service Pack 2
// requires windows installer 3.1
// WARNING: express setup (downloads and installs the components depending on your OS) if you want to deploy it on cd or network download the full bootsrapper on website below
// http://www.microsoft.com/downloads/details.aspx?FamilyId=333325FD-AE52-4E35-B531-508D977D32A6

[CustomMessages]
dotnetfx35_title=.NET Framework 3.5

en.dotnetfx35_size=3 MB - 197 MB
de.dotnetfx35_size=3 MB - 197 MB


[Code]
const
	dotnetfx35_url = 'http://download.microsoft.com/download/7/0/3/703455ee-a747-4cc8-bd3e-98a615c3aedb/dotNetFx35setup.exe';

procedure dotnetfx35();
var
	version: cardinal;
begin
	RegQueryDWordValue(HKLM, 'Software\Microsoft\NET Framework Setup\NDP\v3.5', 'Install', version);
	if version <> 1 then
		AddProduct('dotnetfx35.exe',
			'/lang:enu /qb /norestart',
			CustomMessage('dotnetfx35_title'),
			CustomMessage('dotnetfx35_size'),
			dotnetfx35_url);
end;