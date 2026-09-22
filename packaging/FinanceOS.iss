; Inno Setup script for Finance OS Desktop. Wraps the Standalone Windows IL2CPP player built by
; Assets/Scripts/Editor/BuildScript.cs (Build/Windows/) into a single installer executable.
; See docs/04-Stack_technique.md §9 and docs/09-Decisions_techniques.md ADR-136.
;
; Build order:
;   1. Unity.exe -batchmode -quit -projectPath . -executeMethod FinanceOS.EditorTools.BuildScript.BuildWindowsPlayer
;   2. ISCC.exe packaging\FinanceOS.iss
; Output: packaging\Output\FinanceOS-Setup-<version>.exe (gitignored, rebuilt from source each time)

#define AppVersion "1.0.10"

[Setup]
AppId={{3330EEC9-D1D8-44DB-B2E5-10E37473E979}}
AppName=Finance OS
AppVersion={#AppVersion}
AppPublisher=Florent Barbaouat
DefaultDirName={userpf}\Finance OS
DefaultGroupName=Finance OS
DisableProgramGroupPage=yes
; Per-user install (no admin/UAC prompt) — the target audience is friends/family testing on
; their own PC, most of whom won't have (or want to enter) an administrator password.
PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=Output
OutputBaseFilename=FinanceOS-Setup-{#AppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\FinanceOS.exe
; Covers the installer being run manually (e.g. a fresh download re-run) while FinanceOS.exe still
; holds GameAssembly.dll/UnityPlayer.dll locked — CloseApplications lets Windows Restart Manager
; detect and close it instead of the copy silently failing. RestartApplications then relaunches
; whatever Restart Manager itself closed — it does NOT cover the self-update flow (ADR-139/
; ADR-151), where AppBootstrap already quits FinanceOS.exe itself before this installer starts, so
; Restart Manager never sees it running in the first place; the [Run] section below is what
; actually relaunches the app after a self-update.
CloseApplications=yes
RestartApplications=yes

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "desktopicon"; Description: "Créer un raccourci sur le Bureau"; GroupDescription: "Raccourcis :"

[Files]
; The "_BackUpThisFolder_ButDontShipItWithYourGame" folder is IL2CPP debug/backup data
; (~500MB, larger than the actual player) — Unity's own build output says not to ship it.
Source: "..\Build\Windows\*"; DestDir: "{app}"; Excludes: "FinanceOS_BackUpThisFolder_ButDontShipItWithYourGame\*"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Finance OS"; Filename: "{app}\FinanceOS.exe"
Name: "{group}\Désinstaller Finance OS"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Finance OS"; Filename: "{app}\FinanceOS.exe"; Tasks: desktopicon

[Run]
; skipifsilent restored: this entry is the interactive "Lancer Finance OS" checkbox on the
; Setup Completed wizard page only — it never fires during a silent/very silent install either
; way (confirmed empirically, ADR-151: removing skipifsilent alone did NOT make it fire under
; /VERYSILENT, despite what Inno Setup's own docs suggest — the [Code] section below is the
; actual, verified fix for the silent case, not this flag).
Filename: "{app}\FinanceOS.exe"; Description: "Lancer Finance OS"; Flags: nowait postinstall skipifsilent

[Code]
// ADR-151: the self-update flow (ADR-139) always installs /VERYSILENT, and
// AppBootstrap.InstallUpdateAndRestart quits FinanceOS.exe itself right after launching this
// installer — by the time Windows Restart Manager (CloseApplications/RestartApplications above)
// would otherwise detect and later relaunch it, the process is already gone, so that mechanism
// never restarts anything in this path. The [Run] section's own postinstall checkbox only ever
// fires on an interactive install, confirmed empirically not to fire under /VERYSILENT even
// without skipifsilent. This explicit, unconditional launch is the one mechanism proven (via a
// real install-then-check-the-process test, not just documentation) to actually bring the app
// back after a silent self-update.
procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if (CurStep = ssDone) and WizardSilent then
  begin
    Exec(ExpandConstant('{app}\FinanceOS.exe'), '', '', SW_SHOW, ewNoWait, ResultCode);
  end;
end;
