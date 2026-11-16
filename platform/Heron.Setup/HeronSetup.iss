; Heron-Agent:  HERON-INS-ORC-001, HERON-INS-MCP-004
; Heron-Step:   18
; Heron-Status: DRAFT
; Heron-Since:  0.1.0
; Heron-Layer:  platform
; See docs/29-metadata-standard.md
;
; HERON'S SETUP FOR MODELLERS - D-167. One file, for this Windows user, and it
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
; the defines below. Signing is NOT RUN until a certificate exists (D-167 item
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
; THE BRAND (D-149, docs/brand/README.md) - the same look as HeronInstaller.exe:
; the logo on Mist with the orange line, Ink text. Both pictures are made from
; the brand kit's own files by make-images.ps1, never drawn new. The welcome
; page is shown so the first thing the Setup says is whose it is.
DisableWelcomePage=no
WizardImageFile=images\panel-202x386.png,images\panel-404x772.png
WizardSmallImageFile=images\icon-55.png,images\icon-110.png
; Mist, #F7F5F2 - Inno Setup writes a colour as $BBGGRR.
WizardImageBackColor=$F2F5F7
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
; BOTH ON BY DEFAULT - the owner's word, 2026-11-16: a new modeller must never
; have to find Heron's program folder, which sits under the hidden AppData.
;
; python: Heron's Python side - the pinned packages, the knowledge store and a
; readiness check (PythonSetup). Off passes --no-python: for a PC whose Python
; somebody else manages, and for the owner's own test, where building the store
; from this folder would change what every other chat on the PC reads.
Name: "python"; Description: "Set up Heron's Python side - its packages, its search index and a readiness check (needs Python 3.11 to 3.14)"
; claude: Heron's one user-scope entry in Claude Code, so a chat in ANY folder
; can use it. Each open chat then starts Heron's server: docs/WHAT-TO-INSTALL
; measured 2.0 to 2.3 GB per chat on a 16 GB laptop WITH the optional search
; add-ons, which this Setup does not install - a plain install is not measured.
Name: "claude"; Description: "Add Heron to Claude Code for my user, so every chat in any folder can use it (close Claude first - each open chat then runs Heron in the background)"

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
const
  { THE BRAND'S COLOURS, from the table in docs/brand/README.md and nowhere
    else (D-149). Pascal writes a colour as $BBGGRR, so each is the brand's
    hex with its pairs reversed. }
  BrandMist   = $F2F5F7;   { Mist   #F7F5F2 - the light background }
  BrandInk    = $100F0F;   { Ink    #0F0F10 - text }
  BrandAccent = $1A64FD;   { Accent #FD641A - the one highlight: the line under the header }

var
  ReportPage: TOutputMsgMemoWizardPage;
  BrandLine: TBitmapImage;

{ Every page in the brand: Mist behind, Ink text, and the orange line under the
  header where Windows draws a grey one - HeronInstaller.exe's masthead. }
procedure ApplyBrand();
begin
  WizardForm.Color := BrandMist;
  WizardForm.MainPanel.Color := BrandMist;
  WizardForm.InnerPage.Color := BrandMist;
  WizardForm.WelcomePage.Color := BrandMist;
  WizardForm.FinishedPage.Color := BrandMist;
  WizardForm.TasksList.Color := BrandMist;

  WizardForm.PageNameLabel.Font.Color := BrandInk;
  WizardForm.PageDescriptionLabel.Font.Color := BrandInk;
  WizardForm.WelcomeLabel1.Font.Color := BrandInk;
  WizardForm.WelcomeLabel2.Font.Color := BrandInk;
  WizardForm.FinishedHeadingLabel.Font.Color := BrandInk;
  WizardForm.FinishedLabel.Font.Color := BrandInk;

  { INSIDE THE HEADER, along its bottom edge - so it shows exactly where the
    header shows, and never across the Welcome and Finished pictures. A
    coloured panel on the window itself never showed: the page was drawn
    over it (photographed twice, 2026-11-16). A picture filled with the
    Accent cannot be re-themed by Windows. }
  BrandLine := TBitmapImage.Create(WizardForm);
  BrandLine.Parent := WizardForm.MainPanel;
  BrandLine.Left := 0;
  BrandLine.Width := WizardForm.MainPanel.Width;
  BrandLine.Height := ScaleY(2);
  BrandLine.Top := WizardForm.MainPanel.Height - BrandLine.Height;
  BrandLine.Bitmap.Width := BrandLine.Width;
  BrandLine.Bitmap.Height := BrandLine.Height;
  BrandLine.Bitmap.Canvas.Brush.Color := BrandAccent;
  BrandLine.Bitmap.Canvas.Pen.Color := BrandAccent;
  BrandLine.Bitmap.Canvas.Rectangle(0, 0, BrandLine.Width, BrandLine.Height);
  WizardForm.Bevel1.Visible := False;
end;

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
  ApplyBrand();
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
  Said, ClaudeSaid, Verdict, Report, Params: String;
  Code: Integer;
begin
  if CurStep <> ssPostInstall then exit;

  WizardForm.StatusLabel.Caption :=
    'Installing Heron into each Revit. This can take a few minutes.';

  { --no-check: the Setup carries its own payload and needs no network.
    --no-python when its tick was turned off - heron-install's own flag. }
  Params := '--from "' + ExpandConstant('{app}\payload') + '" --no-check';
  if not WizardIsTaskSelected('python') then
    Params := Params + ' --no-python';
  Code := RunHeronInstall(Params, Said);
  case Code of
    0: if WizardIsTaskSelected('python') then
         Verdict := 'Heron is installed. Start Revit and look for the Heron tab.'
       else
         Verdict := 'Heron''s add-in is installed. Its Python side was left out, as asked - run this Setup again with that tick on to add it.';
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
    { NO LINE MAY START WITH #13 - Inno Setup's preprocessor reads a line
      beginning with # as one of its own directives (found by its compiler,
      2026-11-16: "Unknown preprocessor directive"). }
    SuppressibleMsgBox('Heron could not say what it would remove, so nothing was removed:' + #13#10 + #13#10 + Said,
                       mbError, MB_OK, IDOK);
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
