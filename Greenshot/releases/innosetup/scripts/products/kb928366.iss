// WARNING: Windows Update is better because there are different versions for different OS
// (optional) critical security hotfix for .NET Framework 1.1 Service Pack 1 on Windows 2000/XP
// http://support.microsoft.com/?id=928366
// http://www.microsoft.com/downloads/details.aspx?familyid=281FB2CD-C715-4F05-A01F-0455D2D9EBFB

[CustomMessages]
en.kb928366_title=.NET Framework 1.1 SP1 Security Update (KB928366)
de.kb928366_title=.NET Framework 1.1 SP1 Sicherheitsupdate (KB928366)

en.kb928366_size=8.8 MB
de.kb928366_size=8,8 MB


[Code]
const
	kb928366_url = 'http://download.microsoft.com/download/6/d/0/6d0e5797-91eb-401a-a61f-58b369302018/NDP1.1sp1-KB928366-X86.exe';

procedure kb928366();
var
	version: cardinal;
begin
	RegQueryDWordValue(HKLM, 'Software\Microsoft\NET Framework Setup\NDP\v1.1.4322', 'SP', version);
	if version = 1 then begin
		RegQueryDWordValue(HKLM, 'Software\Microsoft\Updates\.NETFramework\1.1\M928366', 'Installed', version);
		if version <> 1 then
			AddProduct('kb928366.exe',
				'/q',
				CustomMessage('kb928366_title'),
				CustomMessage('kb928366_size'),
				kb928366_url);
	end;
end;