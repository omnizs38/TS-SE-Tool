#ifndef MyAppVersion
  #define MyAppVersion "0.0.0-dev"
#endif
#ifndef SourceDir
  #define SourceDir "..\artifacts\portable"
#endif
#ifndef OutputDir
  #define OutputDir "..\artifacts\setup"
#endif

[Setup]
AppId={{2C28A270-6DA9-4C8E-A8D0-8F4D54A73301}
AppName=TS SE Tool
AppVersion={#MyAppVersion}
AppPublisher=omnizs38 and contributors
AppPublisherURL=https://github.com/omnizs38/TS-SE-Tool
AppSupportURL=https://github.com/omnizs38/TS-SE-Tool/issues
AppUpdatesURL=https://github.com/omnizs38/TS-SE-Tool/releases
DefaultDirName={localappdata}\Programs\TS SE Tool
DefaultGroupName=TS SE Tool
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir={#OutputDir}
OutputBaseFilename=TS-SE-Tool-{#MyAppVersion}-setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
MinVersion=10.0.19045
UninstallDisplayIcon={app}\TS SE Tool.exe
VersionInfoVersion=1.63.0.0
VersionInfoCompany=omnizs38 and contributors
VersionInfoDescription=TS SE Tool Setup
VersionInfoProductName=TS SE Tool
VersionInfoProductVersion=1.63.0.0

[InstallDelete]
Type: filesandordirs; Name: "{app}\libs\x86"
Type: filesandordirs; Name: "{app}\libs\amd64"
Type: files; Name: "{app}\libs\System.Data.SqlServerCe.dll"
Type: files; Name: "{app}\libs\OpenPainter.ColorPicker.dll"
Type: files; Name: "{app}\libs\ICSharpCode.SharpZipLib.dll"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Excludes: "dbs\*,gameref\*,config.cfg"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#SourceDir}\dbs\*"; DestDir: "{app}\dbs"; Flags: onlyifdoesntexist uninsneveruninstall recursesubdirs createallsubdirs skipifsourcedoesntexist
Source: "{#SourceDir}\gameref\*"; DestDir: "{app}\gameref"; Flags: onlyifdoesntexist uninsneveruninstall recursesubdirs createallsubdirs skipifsourcedoesntexist
Source: "{#SourceDir}\config.cfg"; DestDir: "{app}"; Flags: onlyifdoesntexist uninsneveruninstall skipifsourcedoesntexist

[Icons]
Name: "{autoprograms}\TS SE Tool"; Filename: "{app}\TS SE Tool.exe"; WorkingDir: "{app}"
Name: "{userdesktop}\TS SE Tool"; Filename: "{app}\TS SE Tool.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Run]
Filename: "{app}\TS SE Tool.exe"; Description: "Launch TS SE Tool"; Flags: nowait postinstall skipifsilent
