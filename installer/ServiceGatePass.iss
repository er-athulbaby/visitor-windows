#define AppVersion "1.0.0"

[Setup]
AppId={{6F0C2B1E-8E0B-4C5A-9B57-3D4C1E2A9F10}
AppName=Service Gate Pass
AppVersion={#AppVersion}
AppPublisher=Service Gate Pass
DefaultDirName={autopf}\Service Gate Pass
DefaultGroupName=Service Gate Pass
OutputDir=..\dist
OutputBaseFilename=ServiceGatePass-Setup-{#AppVersion}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
Compression=lzma2
SolidCompression=yes
UninstallDisplayIcon={app}\ServiceGatePass.exe

[Dirs]
; Every Windows user on the gate PC must be able to write the database, backups-before-upgrade and logs.
Name: "{commonappdata}\ServiceGatePass"; Permissions: users-modify

[Files]
Source: "..\publish\*"; Excludes: "*.pdb"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\Service Gate Pass"; Filename: "{app}\ServiceGatePass.exe"
Name: "{autodesktop}\Service Gate Pass"; Filename: "{app}\ServiceGatePass.exe"

[Run]
Filename: "{app}\ServiceGatePass.exe"; Description: "Start Service Gate Pass"; Flags: nowait postinstall skipifsilent
