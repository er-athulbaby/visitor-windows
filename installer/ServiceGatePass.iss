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
SetupIconFile=..\src\ServiceGatePass\Resources\app.ico

[Dirs]
; Every Windows user on the gate PC must be able to write the database, backups-before-upgrade and logs.
Name: "{commonappdata}\ServiceGatePass"; Permissions: users-modify

[Tasks]
; Shown when the CPR card reader service is missing or not running. Ticked by default.
Name: "cardreader"; Description: "Install the CPR card reader service (CIO GCC CardRead Server) with the new Bahrain SDK"; Check: not CardReaderRunning

[Files]
Source: "..\publish\*"; Excludes: "*.pdb"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
; Card reader: the vendor's own setup (run from [Code] before files are copied), then the new Bahrain SDK
; copied over it, exactly as the vendor's notes say. Source files: ..\SCardReadServer (see docs/install.md).
Source: "..\SCardReadServer\SCardReadServer.exe"; Flags: dontcopy
Source: "..\SCardReadServer\BahrainSDK\Bahrain Smart Card SDK-V1.1\DotNet\*"; DestDir: "{code:ReaderDir}"; Flags: ignoreversion recursesubdirs createallsubdirs uninsneveruninstall; Tasks: cardreader

[Icons]
Name: "{group}\Service Gate Pass"; Filename: "{app}\ServiceGatePass.exe"
Name: "{autodesktop}\Service Gate Pass"; Filename: "{app}\ServiceGatePass.exe"

[Run]
Filename: "{sys}\sc.exe"; Parameters: "config SCardReadServer start= auto"; Flags: runhidden waituntilterminated; Tasks: cardreader
Filename: "{sys}\net.exe"; Parameters: "start SCardReadServer"; StatusMsg: "Starting the card reader service..."; Flags: runhidden waituntilterminated; Tasks: cardreader
Filename: "{app}\ServiceGatePass.exe"; Description: "Start Service Gate Pass"; Flags: nowait postinstall skipifsilent

[Code]
const
  ServiceKey = 'SYSTEM\CurrentControlSet\Services\SCardReadServer';

function ServiceExists: Boolean;
begin
  Result := RegKeyExists(HKLM, ServiceKey);
end;

function CardReaderRunning: Boolean;
var
  rc: Integer;
begin
  Result := ServiceExists and
    Exec(ExpandConstant('{cmd}'), '/c sc query SCardReadServer | find "RUNNING" >nul', '', SW_HIDE, ewWaitUntilTerminated, rc) and
    (rc = 0);
end;

{ Folder the vendor setup installed the service into, from the service's ImagePath. }
function ReaderDir(Param: String): String;
var
  path: String;
begin
  Result := ExpandConstant('{commonpf32}\CIO\GCC CardRead Server');
  if RegQueryStringValue(HKLM, ServiceKey, 'ImagePath', path) then
    Result := ExtractFileDir(RemoveQuotes(path));
end;

procedure RunHidden(const Exe, Params: String);
var
  rc: Integer;
begin
  Exec(ExpandConstant('{sys}\' + Exe), Params, '', SW_HIDE, ewWaitUntilTerminated, rc);
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  rc, waited: Integer;
begin
  if (CurStep <> ssInstall) or not WizardIsTaskSelected('cardreader') then
    Exit;

  { A service that exists but is not running (a broken earlier install) is removed so the vendor setup can install cleanly. }
  if ServiceExists then
  begin
    RunHidden('net.exe', 'stop SCardReadServer');
    RunHidden('sc.exe', 'delete SCardReadServer');
    Sleep(2000);
  end;

  ExtractTemporaryFile('SCardReadServer.exe');
  if not Exec(ExpandConstant('{tmp}\SCardReadServer.exe'), '', '', SW_SHOW, ewWaitUntilTerminated, rc) then
  begin
    MsgBox('The card reader setup could not start: ' + SysErrorMessage(rc), mbError, MB_OK);
    Exit;
  end;

  { The vendor setup may hand off to a child process; give the service up to 2 minutes to appear. }
  waited := 0;
  while not ServiceExists and (waited < 120) do
  begin
    Sleep(1000);
    waited := waited + 1;
  end;
  if not ServiceExists then
  begin
    MsgBox('The card reader service was not installed. You can install it later by hand (see the install guide).', mbInformation, MB_OK);
    Exit;
  end;

  { Stop it so the new Bahrain SDK files can replace the old ones; [Run] starts it again. }
  RunHidden('net.exe', 'stop SCardReadServer');
end;
