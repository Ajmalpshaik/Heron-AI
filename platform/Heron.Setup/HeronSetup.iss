; Heron-Agent:  HERON-INS-ORC-001, HERON-INS-MCP-004
; Heron-Step:   18
; Heron-Status: DRAFT
; Heron-Since:  0.1.0
; Heron-Layer:  platform
; See docs/29-metadata-standard.md
;
; HERON'S SETUP FOR MODELLERS - D-165. One file, for this Windows user, and it
; never asks for administrator rights.
;
; IT DECIDES NOTHING ABOUT WHAT GOES WHERE. It puts the Heron folder in place
; and hands over to heron-install, the engine every other door uses (D-93, D-94,
; D-96): which products, which Revit years, the wait while Revit is open, the
; checksum of every payload, the second-copy check, putting the old install back
; when a copy fails half-way, Heron's Python side. A rule written in this file
; would be a rule outside everything that tests it - so the two things below
; that look like rules are only doors:
;
;   - "close Revit first" is asked before Install is pressed, so a modeller is
;     not left watching a window that waits; heron-install still waits by
;     itself if a Revit opens in between, and never closes one;
;   - the uninstaller shows heron-install's own sentence naming every product
;     and release before anything goes, and heron-install does the removing.
;
; BUILT BY tools/build-setup.py, NEVER BY HAND: it stages the folder and passes
; the defines below. Signing is NOT RUN until a certificate exists (D-165 item
; 6): with no sign command the build makes an unsigned Setup and names it so.

#ifndef Stage
  #error Build this with tools/build-setup.py - "Stage", the staged Heron folder, is not defined.
#endif
#ifndef AppVersion
  #error Build this with tools/build-setup.py - "AppVersion" is not defined.
#endif
#ifndef OutputDir
  #define OutputDir "."
#endif
#ifndef OutputName
  #define OutputName "HeronSetup-" + AppVersion
#endif
#ifndef IconFile
  #error Build this with tools/build-setup.py - "IconFile" is not defined.
#endif
#ifdef Sign
  #define SignFlag "sign"
#else
  #define SignFlag ""
#endif

[Setup]
; THE APP ID IS FOR EVER. Windows finds an installed Heron by it, so a new
; Setup replaces the old one rather than installing beside it (D-94). Never
; change it.
AppId={{6E0B2B4C-8F3A-4C59-9A2E-3D7C1B5F9A10}
AppName=Heron AI
AppVersion={#AppVersion}
AppVerName=Heron AI {#AppVersion}
AppPublisher=Ajmal PS - Heron AI
AppPublisherURL=https://github.com/Ajmalpshaik/Heron-AI
AppSupportURL=https://github.com/Ajmalpshaik/Heron-AI/issues
; {userpf} is %LOCALAPPDATA%\Programs - this user's own programs folder, which
; needs no administrator. The folder is not asked for: one place, every PC.
DefaultDirName={userpf}\Heron AI
DisableDirPage=yes
DefaultGroupName=Heron AI
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputDir={#OutputDir}
OutputBaseFilename={#OutputName}
SetupIconFile={#IconFile}
UninstallDisplayIcon={app}\HeronInstaller.exe
UninstallDisplayName=Heron AI
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
; IT NEVER CLOSES A PROGRAM FOR THE MODELLER. Revit may hold unsaved work;
; Claude may be mid-chat. Both are asked to be closed, never closed.
CloseApplications=no
RestartApplications=no
SetupLogging=yes
#ifdef Sign
SignTool=heronsign
SignedUninstaller=yes
#endif

[Messages]
ConfirmUninstall=Remove Heron AI from this PC?%n%nYour own Heron data is not touched - settings, the audit log and anything Heron has learned all stay where they are.

[Tasks]
; LEFT OFF BY DEFAULT. On, EVERY Claude chat in EVERY folder starts Heron's
; server - and docs/WHAT-TO-INSTALL measured 2.0 to 2.3 GB per open chat on a
; 16 GB laptop. Opening the Heron folder in Claude keeps working without it.
Name: "claude"; Description: "Also add Heron to Claude Code for my user, so every chat in any folder can use it (close Claude first; each open chat then runs Heron)"; Flags: unchecked

[Files]
Source: "{#Stage}\*"; DestDir: "{app}"; Excludes: "HeronInstaller.exe,heron-install.exe"; Flags: recursesubdirs createallsubdirs ignoreversion
Source: "{#Stage}\HeronInstaller.exe"; DestDir: "{app}"; Flags: ignoreversion {#SignFlag}
Source: "{#Stage}\heron-install.exe"; DestDir: "{app}"; Flags: ignoreversion {#SignFlag}

[InstallDelete]
; INSTALL REPLACES (D-94). Inno Setup copies over a folder and leaves every file
; the new version dropped - a retired fragment would stay in brain\ and keep
; being read. So Heron's own folders go first and come back whole. Nothing of
; the user's lives in any of them: their data is %USERPROFILE%\.heron and
; %APPDATA%\Heron (D-135), never the program folder.
Type: filesandordirs; Name: "{app}\brain"
Type: filesandordirs; Name: "{app}\mcp"
Type: filesandordirs; Name: "{app}\docs"
Type: filesandordirs; Name: "{app}\platform"
Type: filesandordirs; Name: "{app}\tools"
Type: filesandordirs; Name: "{app}\payload"

[Icons]
Name: "{autoprograms}\Heron AI\Heron Installer"; Filename: "{app}\HeronInstaller.exe"; WorkingDir: "{app}"

[UninstallDelete]
; What Heron itself wrote INSIDE its program folder after install - compiled
; Python, the Setup's report. Only {app}: the user's data lives elsewhere
; (%APPDATA%\Heron, %USERPROFILE%\.heron) and is never reached from here.
Type: filesandordirs; Name: "{app}"

[Code]
var
  ReportPage: TOutputMsgMemoWizardPage;

{ Every Revit running for this user, one line each, or '' when none is.
  WMI, because Inno Setup has no process list of its own. A WMI that cannot be
  asked says '' - heron-install looks again itself and waits, so this only ever
  saves a modeller from watching a window that waits. }
function RunningRevits(): String;
var
  Locator, Service, Items, Item: Variant;
  I: Integer;
begin
  Result := '';
  try
    Locator := CreateOleObject('WbemScripting.SWbemLocator');
    Service := Locator.ConnectServer('.', 'root\CIMV2');
    Items := Service.ExecQuery('SELECT ExecutablePath FROM Win32_Process WHERE Name = ''Revit.exe''');
    for I := 0 to Items.Count - 1 do
    begin
      Item := Items.ItemIndex(I);
      if VarIsNull(Item.ExecutablePath) then
        Result := Result + #13#10 + '    Revit'
      else
        Result := Result + #13#10 + '    ' + Item.ExecutablePath;
    end;
  except
    Result := '';
  end;
end;

function CloseRevitFirst(const Open: String): String;
begin
  Result := 'Close Revit first. Heron cannot replace an add-in Revit has loaded, and it never ' +
            'closes Revit for you - your model may have unsaved work.' + #13#10 + #13#10 +
            'Open now:' + Open + #13#10 + #13#10 +
            'Save your work, close it, then try again.';
end;

function LinesOf(const Output: TExecOutput): String;
var
  I: Integer;
begin
  Result := '';
  for I := 0 to GetArrayLength(Output.StdOut) - 1 do
    Result := Result + Output.StdOut[I] + #13#10;
  for I := 0 to GetArrayLength(Output.StdErr) - 1 do
    Result := Result + Output.StdErr[I] + #13#10;
end;

{ heron-install, hidden, with everything it says captured. Its exit code is
  returned: 0 did what was asked, 1 refused before touching anything, 2 ran and
  something failed, 3 could not run because Revit stayed open. -1 means it
  could not be started at all. }
function RunHeronInstall(const Params: String; var Said: String): Integer;
var
  Output: TExecOutput;
  Code: Integer;
begin
  Said := '';
  if not ExecAndCaptureOutput(ExpandConstant('{app}\heron-install.exe'), Params,
                              ExpandConstant('{app}'), SW_HIDE, ewWaitUntilTerminated,
                              Code, Output) then
  begin
    Said := 'heron-install could not be started: ' + SysErrorMessage(Code);
    Result := -1;
    exit;
  end;
  Said := LinesOf(Output);
  Result := Code;
end;

procedure InitializeWizard();
begin
  ReportPage := CreateOutputMsgMemoPage(wpInfoAfter,
    'What Heron did',
    'For each Revit on this PC, and Heron''s Python side.',
    'Heron''s own report, word for word. It is also kept as setup-report.txt in Heron''s folder.',
    '');
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  Open: String;
begin
  Result := True;
  if CurPageID = wpReady then
  begin
    Open := RunningRevits();
    if Open <> '' then
    begin
      MsgBox(CloseRevitFirst(Open), mbInformation, MB_OK);
      Result := False;
    end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  Said, ClaudeSaid, Verdict, Report: String;
  Code: Integer;
begin
  if CurStep <> ssPostInstall then exit;

  WizardForm.StatusLabel.Caption :=
    'Installing Heron into each Revit, then Heron''s Python side. This can take a few minutes.';

  { --no-check: the Setup carries its own payload and needs no network. }
  Code := RunHeronInstall('--from "' + ExpandConstant('{app}\payload') + '" --no-check', Said);
  case Code of
    0: Verdict := 'Heron is installed. Start Revit and look for the Heron tab.';
    3: Verdict := 'Heron was NOT installed: a Revit stayed open. Close it and run this Setup again.';
  else
    Verdict := 'Something did not install - Heron''s report below says what, and what to do.';
  end;

  ClaudeSaid := '';
  if WizardIsTaskSelected('claude') then
    RunHeronInstall('--add-to-claude', ClaudeSaid);

  Report := Verdict + #13#10 + #13#10 + Said;
  if ClaudeSaid <> '' then Report := Report + #13#10 + 'Claude Code:' + #13#10 + ClaudeSaid;
  ReportPage.RichEditViewer.Lines.Text := Report;
  SaveStringToFile(ExpandConstant('{app}\setup-report.txt'), Report, False);
end;

{ Before anything is removed: no Revit open, and the modeller has read what
  will go - heron-install's own sentence, naming every product and release. }
function InitializeUninstall(): Boolean;
var
  Open, Said: String;
begin
  Result := True;

  Open := RunningRevits();
  if Open <> '' then
  begin
    SuppressibleMsgBox(CloseRevitFirst(Open), mbInformation, MB_OK, IDOK);
    Result := False;
    exit;
  end;

  if RunHeronInstall('--uninstall --list', Said) <> 0 then
  begin
    SuppressibleMsgBox('Heron could not say what it would remove, so nothing was removed:' +
                       #13#10 + #13#10 + Said, mbError, MB_OK, IDOK);
    Result := False;
    exit;
  end;

  if not UninstallSilent then
    if MsgBox(Said + #13#10 + 'Remove it now?', mbConfirmation, MB_YESNO) <> IDYES then
      Result := False;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Said: String;
begin
  if CurUninstallStep <> usUninstall then exit;
  if RunHeronInstall('--uninstall', Said) <> 0 then
    SuppressibleMsgBox('Not everything of Heron came out of Revit. Heron''s report:' + #13#10 + #13#10 +
                       Said, mbError, MB_OK, IDOK);
end;
