; requires Windows 10, Windows 7 Service Pack 1, Windows 8, Windows 8.1, Windows Server 2008 R2 SP1, Windows Server 2008 Service Pack 2, Windows Server 2012, Windows Server 2012 R2, Windows Vista Service Pack 2
; WARNING: express setup (downloads and installs the components depending on your OS) if you want to deploy it on cd or network download the full bootsrapper on website below
; https://www.microsoft.com/en-US/download/details.aspx?id=49982

[CustomMessages]
dotnetfx46_title=.NET Framework 4.6.1

dotnetfx46_size=1 MB - 65 MB

;http://www.microsoft.com/globaldev/reference/lcid-all.mspx
en.dotnetfx46_lcid=
de.dotnetfx46_lcid=/lcid 1031

[Code]
const
	dotnetfx461_url = 'http://download.microsoft.com/download/3/5/9/35980F81-60F4-4DE3-88FC-8F962B97253B/NDP461-KB3102438-Web.exe';

procedure dotnetfx46(minVersion: integer);
begin
	if (not netfxinstalled(NetFx4x, '') or (netfxspversion(NetFx4x, '') < minVersion)) then
		AddProduct('dotnetfx46.exe',
			CustomMessage('dotnetfx46_lcid') + ' /passive /norestart',
			CustomMessage('dotnetfx46_title'),
			CustomMessage('dotnetfx46_size'),
			dotnetfx461_url,
			false, false, false);
end;

[Setup]
