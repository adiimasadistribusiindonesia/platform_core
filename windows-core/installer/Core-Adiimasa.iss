#define MyAppName "Core Adiimasa"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Adiimasa"
#define MyAppExeName "Core Adiimasa.exe"

[Setup]
AppId={{D6C8C6E4-9F2B-4B19-9F0C-COREADIIMASA2026}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\Core Adiimasa
DefaultGroupName=Core Adiimasa
OutputDir=..\artifacts
OutputBaseFilename=Core Adiimasa Setup
SetupIconFile=Core Adiimasa.ico
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
DisableProgramGroupPage=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}

[Files]
Source: "..\publish-core\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion
Source: "..\MicrosoftEdgeWebView2Setup.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall
Source: "..\vc_redist.x64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Icons]
Name: "{autodesktop}\Core Adiimasa"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Core Adiimasa"; Filename: "{app}\{#MyAppExeName}"

[Run]
Filename: "{tmp}\vc_redist.x64.exe"; Parameters: "/install /quiet /norestart"; StatusMsg: "Memeriksa Microsoft Visual C++ Runtime..."; Flags: waituntilterminated
Filename: "{tmp}\MicrosoftEdgeWebView2Setup.exe"; Parameters: "/silent /install"; StatusMsg: "Memeriksa Microsoft Edge WebView2 Runtime..."; Flags: waituntilterminated
Filename: "{app}\{#MyAppExeName}"; Description: "Jalankan Core Adiimasa"; Flags: nowait postinstall skipifsilent
