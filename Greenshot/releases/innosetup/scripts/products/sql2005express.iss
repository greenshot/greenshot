// requires Windows 2000 Service Pack 4, Windows Server 2003 Service Pack 1, Windows XP Service Pack 2
// SQL Express 2005 Service Pack 1+ should be installed for SQL Express 2005 to work on Vista
// requires windows installer 3.1
// http://www.microsoft.com/downloads/details.aspx?FamilyID=220549b5-0b07-4448-8848-dcc397514b41

[CustomMessages]
sql2005express_title=SQL Server 2005 Express

en.sql2005express_size=57.7 MB
de.sql2005express_size=57,7 MB


[Code]
const
	sql2005express_url = 'http://download.microsoft.com/download/f/1/0/f10c4f60-630e-4153-bd53-c3010e4c513b/SQLEXPR.EXE';

procedure sql2005express();
var
	version: cardinal;
begin
	//CHECK NOT FINISHED YET
	//RTM: 9.00.1399.06
	//Service Pack 1: 9.1.2047.00
	//Service Pack 2: 9.2.3042.00
	RegQueryDWordValue(HKLM, 'Software\Microsoft\Microsoft SQL Server\90\DTS\Setup', 'Install', version);
	if version <> 1 then
		AddProduct('sql2005express.exe',
			'/qb',
			CustomMessage('sql2005express_title'),
			CustomMessage('sql2005express_size'),
			sql2005express_url);
end;
