; requires Windows 10, Windows 7 Service Pack 1, Windows 8.1, Windows Server 2008 R2 SP1, Windows Server 2012, Windows Server 2012 R2, Windows Server 2016
; WARNING: express setup (downloads and installs the components depending on your OS) if you want to deploy it on cd or network download the full bootsrapper on website below
; https://dotnet.microsoft.com/download/dotnet-framework/net481

[CustomMessages]
dotnetfx481_title=.NET Framework 4.8.1

dotnetfx481_size=1 MB - 74 MB

[Code]
const
	dotnetfx48_url = 'https://download.microsoft.com/download/4/b/2/cd00d4ed-ebdd-49ee-8a33-eabc3d1030e3/NDP481-Web.exe';

procedure dotnetfx481(minVersion: integer);
begin
	if (not netfxinstalled(NetFx4x, '') or (netfxspversion(NetFx4x, '') < minVersion)) then
		AddProduct('dotnetfx481.exe',
			'/lcid ' + CustomMessage('lcid') + ' /passive /norestart',
			CustomMessage('dotnetfx481_title'),
			CustomMessage('dotnetfx481_size'),
			dotnetfx48_url,
			false, false, false);
end;

[Setup]
