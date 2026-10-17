; Inno Setup 6 script for Decimal Selector. Built by build.cmd.
; Installs per user (no admin rights needed) into %LOCALAPPDATA%\Programs\Decimal Selector.

#define AppName "Decimal Selector"
#define AppVersion "1.0.0"
#define AppExe "DecimalSelector.exe"

[Setup]
AppId={{026385C0-ABEE-4064-A882-6BD2EB77FBA3}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName}
DefaultDirName={autopf}\{#AppName}
DisableDirPage=yes
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=dist
OutputBaseFilename=DecimalSelector-Setup
SetupIconFile=icon.ico
UninstallDisplayIcon={app}\{#AppExe}
WizardStyle=modern
Compression=lzma2
SolidCompression=yes
CloseApplications=no

[Tasks]
Name: "startup"; Description: "Start {#AppName} automatically when Windows starts"

[Files]
Source: "build\{#AppExe}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "DecimalSelector"; ValueData: """{app}\{#AppExe}"""; Tasks: startup

[Run]
Filename: "{app}\{#AppExe}"; Description: "Start {#AppName} now"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{sys}\taskkill.exe"; Parameters: "/f /im {#AppExe}"; Flags: runhidden; RunOnceId: "StopApp"

[Code]
// Stop a running copy so an upgrade can replace the exe.
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/f /im {#AppExe}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := '';
end;

// The app can enable "Start with Windows" itself, so always clean up its registry entries.
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'DecimalSelector');
    RegDeleteKeyIncludingSubkeys(HKCU, 'Software\DecimalSelector');
  end;
end;
