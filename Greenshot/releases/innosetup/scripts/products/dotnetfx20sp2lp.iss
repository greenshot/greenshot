//http://www.microsoft.com/downloads/details.aspx?FamilyID=c69789e0-a4fa-4b2e-a6b5-3b3695825992

[CustomMessages]
de.dotnetfx20sp2lp_title=.NET Framework 2.0 SP2 Sprachpaket: Deutsch

de.dotnetfx20sp2lp_size=3,4 MB

;http://www.microsoft.com/globaldev/reference/lcid-all.mspx
de.dotnetfx20sp2lp_lcid=1031

de.dotnetfx20sp2lp_url=http://download.microsoft.com/download/0/b/1/0b175c8e-34bd-46c0-bfcd-af8d33770c58/netfx20sp2_x86de.exe
de.dotnetfx20sp2lp_url_x64=http://download.microsoft.com/download/4/e/c/4ec67a11-879d-4550-9c25-fd9ab4261b46/netfx20sp2_x64de.exe
de.dotnetfx20sp2lp_url_ia64=http://download.microsoft.com/download/a/3/3/a3349a2d-36e4-4797-8297-4394e6fbd677/NetFx20SP2_ia64de.exe


[Code]
procedure dotnetfx20sp2lp();
var
	version: cardinal;
begin
	if ActiveLanguage() <> 'en' then begin
		RegQueryDWordValue(HKLM, 'Software\Microsoft\NET Framework Setup\NDP\v2.0.50727\' + CustomMessage('dotnetfx20sp2lp_lcid'), 'SP', version);
		
		if version < 2 then
			AddProduct(ExpandConstant('dotnetfx20sp2_langpack.exe'),
				'/lang:enu /qb /norestart"',
				CustomMessage('dotnetfx20sp2lp_title'),
				CustomMessage('dotnetfx20sp2lp_size'),
				GetURL(CustomMessage('dotnetfx20sp2lp_url'), CustomMessage('dotnetfx20sp2lp_url_x64'), CustomMessage('dotnetfx20sp2lp_url_ia64')));
	end;
end;