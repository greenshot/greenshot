// WARNING: Windows Update is better because there are different versions for different OS
// (optional) critical security hotfix for .NET Framework 1.1 Service Pack 1 on Windows 2000/XP/2003
// http://support.microsoft.com/default.aspx?scid=kb;en-us;886903
// http://www.microsoft.com/downloads/details.aspx?familyid=8EC6FB8A-29EB-49CF-9DBC-1A0DC2273FF9

[CustomMessages]
en.kb886903_title=.NET Framework 1.1 SP1 Security Update (KB886903)
de.kb886903_title=.NET Framework 1.1 SP1 Sicherheitsupdate (KB886903)

en.kb886903_size=1.5 MB
de.kb886903_size=1,5 MB


[Code]
const
	kb886903_url = 'http://download.microsoft.com/download/e/1/4/e14c0c02-591b-4696-8552-eb710c26a3cd/NDP1.1sp1-KB886903-X86.exe';

procedure kb886903();
var
	version: cardinal;
begin
	RegQueryDWordValue(HKLM, 'Software\Microsoft\NET Framework Setup\NDP\v1.1.4322', 'SP', version);
	if version = 1 then begin
		RegQueryDWordValue(HKLM, 'Software\Microsoft\Updates\.NETFramework\1.1\M886903', 'Installed', version);
		if version <> 1 then
			AddProduct('kb886903.exe',
				'/q',
				CustomMessage('kb886903_title'),
				CustomMessage('kb886903_size'),
				kb886903_url);
	end;
end;