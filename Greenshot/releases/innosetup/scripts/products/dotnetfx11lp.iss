[CustomMessages]
de.dotnetfx11lp_title=.NET Framework 1.1 Sprachpaket: Deutsch

de.dotnetfx11lp_size=1,4 MB

;http://www.microsoft.com/globaldev/reference/lcid-all.mspx
de.dotnetfx11lp_lcid=1031

de.dotnetfx11lp_url=http://download.microsoft.com/download/6/8/2/6821e687-526a-4ef8-9a67-9a402ec5ac9e/langpack.exe


[Code]
procedure dotnetfx11lp();
var
	version: cardinal;
begin
	if ActiveLanguage() <> 'en' then begin
		RegQueryDWordValue(HKLM, 'Software\Microsoft\NET Framework Setup\NDP\v1.1.4322\' + CustomMessage('dotnetfx11lp_lcid'), 'Install', version);
		
		if version <> 1 then
			AddProduct(ExpandConstant('dotnetfx11_langpack.exe'),
				'/q:a /c:"inst.exe /qb /l"',
				CustomMessage('dotnetfx11lp_title'),
				CustomMessage('dotnetfx11lp_size'),
				CustomMessage('dotnetfx11lp_url'));
	end;
end;
