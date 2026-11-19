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

[Tasks]
; Shown only when the CPR card reader service is not installed yet. Ticked by default.
Name: "cardreader"; Description: "Install the CPR card reader service (CIO GCC CardRead Server)"; Check: not CardReaderInstalled

[Files]
Source: "..\publish\*"; Excludes: "*.pdb"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
; Copy of a working card reader install, Bahrain SDK included. Staged into ..\vendor\CardReadServer before building (see docs/install.md).
Source: "..\vendor\CardReadServer\*"; Excludes: "*.InstallLog"; DestDir: "{commonpf32}\CIO\GCC CardRead Server"; Flags: ignoreversion recursesubdirs createallsubdirs uninsneveruninstall; Tasks: cardreader

[Icons]
Name: "{group}\Service Gate Pass"; Filename: "{app}\ServiceGatePass.exe"
Name: "{autodesktop}\Service Gate Pass"; Filename: "{app}\ServiceGatePass.exe"

[Run]
Filename: "{sys}\sc.exe"; Parameters: "create SCardReadServer binPath= ""\""{commonpf32}\CIO\GCC CardRead Server\SCardReadWebApi.exe\"""" start= auto DisplayName= ""CIO GCC CardRead Server"""; StatusMsg: "Installing the card reader service..."; Flags: runhidden waituntilterminated; Tasks: cardreader
Filename: "{sys}\sc.exe"; Parameters: "start SCardReadServer"; StatusMsg: "Starting the card reader service..."; Flags: runhidden waituntilterminated; Tasks: cardreader
Filename: "{app}\ServiceGatePass.exe"; Description: "Start Service Gate Pass"; Flags: nowait postinstall skipifsilent

[Code]
function CardReaderInstalled: Boolean;
begin
  Result := RegKeyExists(HKLM, 'SYSTEM\CurrentControlSet\Services\SCardReadServer');
end;
