[CustomMessages]
de.dotnetfx35lp_title=.NET Framework 3.5 Sprachpaket: Deutsch

de.dotnetfx35lp_size=13 MB - 51 MB

;http://www.microsoft.com/globaldev/reference/lcid-all.mspx
de.dotnetfx35lp_lcid=1031

de.dotnetfx35lp_url=http://download.microsoft.com/download/d/1/e/d1e617c3-c7f4-467e-a7de-af832450efd3/dotnetfx35langpack_x86de.exe


[Code]
procedure dotnetfx35lp();
var
	version: cardinal;
begin
	if ActiveLanguage() <> 'en' then begin
		RegQueryDWordValue(HKLM, 'Software\Microsoft\NET Framework Setup\NDP\v3.5\' + CustomMessage('dotnetfx35lp_lcid'), 'Install', version);
		
		if version <> 1 then
			AddProduct('dotnetfx35_langpack.exe',
				'/lang:enu /qb /norestart',
				CustomMessage('dotnetfx35lp_title'),
				CustomMessage('dotnetfx35lp_size'),
				CustomMessage('dotnetfx35lp_url'));
	end;
end;
