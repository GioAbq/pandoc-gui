; Inno Setup script for Pandoc GUI (.NET 10 / Avalonia, self-contained win-x64).
; Paths and version are injected by build-inno.ps1 via /D defines; defaults below
; let the script also compile standalone for quick local tweaks.

#define MyAppName "Pandoc GUI"
#define MyAppPublisher "Arsene Lapostolet"
#define MyAppExeName "PandocGui.exe"

#ifndef MyAppVersion
  #define MyAppVersion "4.0.0.0"
#endif
#ifndef MyAppURL
  #define MyAppURL "https://github.com/Ombrelin/pandoc-gui"
#endif
#ifndef SourceDir
  #define SourceDir "..\..\dist\publish\win-x64"
#endif
#ifndef OutputDir
  #define OutputDir "..\..\dist\releases\win-x64"
#endif

[Setup]
AppId={{FCD2F146-6DB8-4DDC-AAA2-E9E5DA061AA6}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
UsedUserAreasWarning=no
LicenseFile=..\..\LICENSE
OutputDir={#OutputDir}
OutputBaseFilename=pandoc-gui-setup
SetupIconFile=..\..\src\PandocGui\Assets\avalonia-logo.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
