#define MyAppName "Junction VIII"
#define MyAppPublisher "Tsunamods"
#define MyAppURL "https://github.com/tsunamods-codes/Junction-VIII"

#ifndef MyAppVersion
  #define MyAppVersion "0.0.0.0"
#endif

#ifndef MyAppPath
  #define MyAppPath "..\AppUI"
#endif

#ifndef MyAppRelease
  #define MyAppRelease "Debug"
#endif

#ifndef MyAppTargetFramework
  #define MyAppTargetFramework "net9.0-windows7.0"
#endif

#define public Dependency_Path_NetCoreCheck ""
#include "CodeDependencies.iss"

[Setup]
AppId={{818294FB-45D7-4A56-A0C0-AB85AFBC125E}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppCopyright="Microsoft Public License (MS-PL)"
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={localappdata}\Programs\{#MyAppName}
PrivilegesRequired=lowest
DefaultGroupName={#MyAppName}
Compression=lzma2/ultra
InternalCompressLevel=ultra
SolidCompression=true
VersionInfoCompany={#MyAppPublisher}
VersionInfoVersion={#MyAppVersion}
SetupIconFile="{#MyAppPath}\App.ico"
UninstallDisplayIcon="{app}\uninstall.ico"
UninstallDisplayName={#MyAppName}
ArchitecturesInstallIn64BitMode=x64

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 0,6.1

[Files]
Source: "{#MyAppPath}\bin\{#MyAppRelease}\{#MyAppTargetFramework}\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion
Source: "{#MyAppPath}\App.ico"; DestDir: "{app}"; DestName: "uninstall.ico"

[Icons]
Name: "{group}\Junction VIII"; Filename: "{app}\Junction VIII.exe";
Name: "{group}\TurBoLog"; Filename: "{app}\TurBoLog.exe";
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"; IconFilename: "{app}\uninstall.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\Junction VIII.exe"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\Junction VIII.exe"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\Junction VIII.exe"; Flags: nowait postinstall runascurrentuser skipifsilent; Description: "Launch {#MyAppName}"

[InstallDelete]
Name: "{app}\J8Workshop\catalog.xml"; Type: files


[UninstallDelete]
Name: "{app}\J8Workshop"; Type: filesandordirs

[Code]
function InitializeSetup: Boolean;
begin
  Dependency_ForceX86 := True;
  Dependency_AddVC2015To2022;
  Dependency_AddDotNet90Desktop;

  Dependency_ForceX86 := False;
  Dependency_AddVC2015To2022;
  Dependency_AddDotNet90Desktop;

  Result := True;
end;
