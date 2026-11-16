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
; and hands over to the engine every other door uses (D-93, D-94, D-96): which
; products, which Revit years, the wait while Revit is open, the checksum of
; every payload, the second-copy check, putting the old install back when a copy
; fails half-way, Heron's Python side. A rule written in this file would be a
; rule outside everything that tests it.
;
; ITS FACE IS HERON'S OWN WINDOW - the owner's word, 2026-11-16 (D-167,
; appended). The Setup copies the folder, opens HeronInstaller.exe with --from
; its payload, and closes; that branded window asks everything - the Revit
; years, the two ticks - and installs. Run SILENT (/VERYSILENT, for a managed
; PC), no window can be asked, so heron-install does the same install itself.
;
;   - the uninstaller shows heron-install's own sentence naming every product
;     and release before anything goes, and heron-install does the removing;
;   - a Heron Installer window left open from the folder stops both Setup and
;     uninstall before a file is touched - its exe is in that folder.
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
; NO PAGES OF ITS OWN - the face is HeronInstaller.exe (above). Only the short
; copying page shows, then the Setup closes and the window opens.
DisableWelcomePage=yes
DisableReadyPage=yes
DisableFinishedPage=yes
; THE BRAND (D-149, docs/brand/README.md) on what is left - the copying page:
; Mist, Ink text, the heron in the header, the orange line under it. Both
; pictures are made from the brand kit's own files by make-images.ps1.
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

; THE TWO TICKS ARE IN HERON'S WINDOW NOW (D-167 items 5 and 5a, appended
; 2026-11-16) - "Set up Heron's AI side" and "Add Heron to Claude Code", both
; on. Each open chat then starts Heron's server: docs/WHAT-TO-INSTALL measured
; 2.0 to 2.3 GB per chat on a 16 GB laptop WITH the optional search add-ons,
; which this Setup does not install - a plain install is not measured.

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
; WITH --from, like the Setup opens it: without it the window would look for
; builds, find none, and take the download route this Setup exists to avoid.
Name: "{autoprograms}\Heron AI\Heron Installer"; Filename: "{app}\HeronInstaller.exe"; Parameters: "--from ""{app}\payload"""; WorkingDir: "{app}"

[Run]
; THE FACE (above): Heron's own window, opened as the Setup closes. Not when
; silent - nobody is there to press Install; CurStepChanged installs instead.
Filename: "{app}\HeronInstaller.exe"; Parameters: "--from ""{app}\payload"""; WorkingDir: "{app}"; Flags: nowait; Check: not WizardSilent

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
  BrandLine: TBitmapImage;
  ReadyPassed: Boolean;

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

{ Every Heron Installer or heron-install running FROM Folder, one line each,
  or '' when none is. Their exes are in that folder, so replacing or removing
  it while one runs fails half-way on a locked file. WMI, as RunningRevits. }
function OpenFromFolder(const Folder: String): String;
var
  Locator, Service, Items, Item: Variant;
  I: Integer;
  Where, Path: String;
begin
  Result := '';
  Where := Lowercase(AddBackslash(Folder));
  try
    Locator := CreateOleObject('WbemScripting.SWbemLocator');
    Service := Locator.ConnectServer('.', 'root\CIMV2');
    Items := Service.ExecQuery('SELECT ExecutablePath FROM Win32_Process WHERE Name = ''HeronInstaller.exe'' OR Name = ''heron-install.exe''');
    for I := 0 to Items.Count - 1 do
    begin
      Item := Items.ItemIndex(I);
      if not VarIsNull(Item.ExecutablePath) then
      begin
        Path := Item.ExecutablePath;
        if Pos(Where, Lowercase(Path)) = 1 then
          Result := Result + #13#10 + '    ' + Path;
      end;
    end;
  except
    Result := '';
  end;
end;

function CloseHeronFirst(const Open: String): String;
begin
  Result := 'Close the Heron Installer window first. It runs from Heron''s folder, ' +
            'which this would replace or remove.' + #13#10 + #13#10 +
            'Open now:' + Open + #13#10 + #13#10 +
            'Close it, then try again. Nothing was changed.';
end;

procedure InitializeWizard();
begin
  ApplyBrand();
end;

{ NO READY PAGE - one Install button, and it is Heron's window's. Inno Setup
  IGNORES DisableReadyPage when Ready would be the first page shown (its own
  help, read 2026-11-16), and with no pages of its own it is. Neither skipping
  it in ShouldSkipPage nor pressing Next from CurPageChanged moved it -
  measured the same day with a throwaway Setup into a scratch folder: it sat on
  Ready for 25 seconds. Pressed 50 ms after it is shown, by a one-shot timer,
  it went on to copying and closed by itself. ONCE only. With a Heron Installer
  open from the folder it stopped on the Preparing page with CloseHeronFirst's
  words and only Cancel to press, nothing copied (the same test). }
function SetTimer(hWnd: LongWord; nIDEvent, uElapse: LongWord; lpTimerFunc: LongWord): LongWord;
  external 'SetTimer@user32.dll stdcall';
function KillTimer(hWnd: LongWord; uIDEvent: LongWord): BOOL;
  external 'KillTimer@user32.dll stdcall';

var
  ReadyTimer: LongWord;

procedure PressInstall(Wnd: LongWord; Msg: LongWord; IdEvent: LongWord; Time: LongWord);
begin
  KillTimer(0, ReadyTimer);
  if WizardForm.CurPageID = wpReady then
    WizardForm.NextButton.OnClick(WizardForm.NextButton);
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if (CurPageID = wpReady) and not ReadyPassed then
  begin
    ReadyPassed := True;
    ReadyTimer := SetTimer(0, 0, 50, CreateCallback(@PressInstall));
  end;
end;

{ Before a file is copied. The wizard has no pages of its own, so this is the
  one place left to stop - and what it returns is shown, and nothing copied. }
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  Open: String;
begin
  Result := '';
  Open := OpenFromFolder(ExpandConstant('{app}'));
  if Open <> '' then Result := CloseHeronFirst(Open);
end;

{ SILENT ONLY. A Setup run with /SILENT or /VERYSILENT has nobody to press
  Install in Heron's window, so heron-install does the same install itself,
  both ticks' work included, and its report is kept as setup-report.txt in
  Heron's folder. --no-check: the Setup carries its own payload. }
procedure CurStepChanged(CurStep: TSetupStep);
var
  Said, ClaudeSaid, Report: String;
  Code: Integer;
begin
  if CurStep <> ssPostInstall then exit;
  if not WizardSilent then exit;

  Code := RunHeronInstall('--from "' + ExpandConstant('{app}\payload') + '" --no-check', Said);
  ClaudeSaid := '';
  if Code = 0 then
    RunHeronInstall('--add-to-claude', ClaudeSaid);

  Report := 'heron-install exit code ' + IntToStr(Code) + #13#10 + #13#10 + Said;
  if ClaudeSaid <> '' then Report := Report + #13#10 + 'Claude Code:' + #13#10 + ClaudeSaid;
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

  Open := OpenFromFolder(ExpandConstant('{app}'));
  if Open <> '' then
  begin
    SuppressibleMsgBox(CloseHeronFirst(Open), mbInformation, MB_OK, IDOK);
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
