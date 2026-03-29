; =============================================================================
; FarmGame Windows Installer (x64) — Inno Setup Script
;
; Container mount layout:
;   /work/setup.iss          <- this file
;   /work/source/            <- dist/win-x64 contents
;   /work/output/            <- installer output
;
; VC++ Redistributable detection:
;   Checks registry for Visual C++ 2015-2022 Redistributable (x64).
;   If missing, shows a download prompt before installation proceeds.
; =============================================================================

#define AppName "Farm Game"
#define AppExeName "FarmGame.exe"
#define AppPublisher "FarmGame"
#define AppURL "https://github.com/tsunejui/farm-game"
#define VCRedistURL "https://aka.ms/vs/17/release/vc_redist.x64.exe"
#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif

[Setup]
AppId={{B7F1A3D2-4E5C-6F78-9A0B-C1D2E3F4A5B6}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
AllowNoIcons=yes
OutputDir=output
OutputBaseFilename=FarmGame_Setup_{#AppVersion}_x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "source\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[Code]
function IsVCRedistInstalled: Boolean;
var
  Installed: Cardinal;
begin
  Result := False;
  { Check Visual C++ 2015-2022 Redistributable (x64) registry key }
  if RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64', 'Installed', Installed) then
    Result := (Installed = 1);
  if not Result then
  begin
    if RegQueryDWordValue(HKLM, 'SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\x64', 'Installed', Installed) then
      Result := (Installed = 1);
  end;
end;

function InitializeSetup: Boolean;
var
  ErrorCode: Integer;
begin
  Result := True;
  if not IsVCRedistInstalled then
  begin
    if MsgBox('This game requires the Microsoft Visual C++ Redistributable (x64).' + #13#10 +
              #13#10 +
              'It does not appear to be installed on your system.' + #13#10 +
              #13#10 +
              'Please download and install it from:' + #13#10 +
              '{#VCRedistURL}' + #13#10 +
              #13#10 +
              'Click OK to open the download page, or Cancel to continue anyway.',
              mbInformation, MB_OKCANCEL) = IDOK then
    begin
      ShellExec('open', '{#VCRedistURL}', '', '', SW_SHOW, ewNoWait, ErrorCode);
      MsgBox('After installing the Visual C++ Redistributable, run this installer again.', mbInformation, MB_OK);
      Result := False;
    end;
  end;
end;
