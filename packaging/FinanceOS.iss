; Inno Setup script for Finance OS Desktop. Wraps the Standalone Windows IL2CPP player built by
; Assets/Scripts/Editor/BuildScript.cs (Build/Windows/) into a single installer executable.
; See docs/04-Stack_technique.md §9 and docs/09-Decisions_techniques.md ADR-136.
;
; Build order:
;   1. Unity.exe -batchmode -quit -projectPath . -executeMethod FinanceOS.EditorTools.BuildScript.BuildWindowsPlayer
;   2. ISCC.exe packaging\FinanceOS.iss
; Output: packaging\Output\FinanceOS-Setup-<version>.exe (gitignored, rebuilt from source each time)

#define AppVersion "1.0.8"

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
; The self-update flow (ADR-139) launches this installer /VERYSILENT while FinanceOS.exe may
; still be running (and holding GameAssembly.dll/UnityPlayer.dll locked) — CloseApplications lets
; Windows Restart Manager detect and close it automatically instead of the copy silently failing,
; and RestartApplications relaunches it once installation finishes, silent runs included.
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
Filename: "{app}\FinanceOS.exe"; Description: "Lancer Finance OS"; Flags: nowait postinstall skipifsilent
