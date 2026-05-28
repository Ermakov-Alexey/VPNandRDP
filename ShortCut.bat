@set @x=0; /*
@cscript //nologo /e:jscript "%~f0"
@exit /b
 
*/
var oShell = new ActiveXObject("WScript.Shell");
var oShortcut = oShell.CreateShortcut(oShell.SpecialFolders("Desktop") + "\\VaR.lnk");
oShortcut.TargetPath = "C:\\IT\\VaR\\VaR.exe";
oShortcut.Save();