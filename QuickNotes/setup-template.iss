; Inno Setup Script for QuickNotes Command Palette Extension
; This creates an installer for WinGet distribution

#define AppVersion "0.0.1.0"

[Setup]
AppId={{AB9CB241-4C93-413F-96AF-43B7F5EF8E47}}
AppName=Quick Notes
AppVersion={#AppVersion}
AppPublisher=QQ
DefaultDirName={autopf}\QuickNotes
OutputDir=bin\Release\installer
OutputBaseFilename=QuickNotes-Setup-{#AppVersion}
Compression=lzma
SolidCompression=yes
MinVersion=10.0.19041
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "bin\Release\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\Quick Notes"; Filename: "{app}\QuickNotes.exe"

[Registry]
Root: HKCU; Subkey: "SOFTWARE\Classes\CLSID\{{AB9CB241-4C93-413F-96AF-43B7F5EF8E47}}"; ValueData: "QuickNotes"
Root: HKCU; Subkey: "SOFTWARE\Classes\CLSID\{{AB9CB241-4C93-413F-96AF-43B7F5EF8E47}}\LocalServer32"; ValueData: "{app}\QuickNotes.exe -RegisterProcessAsComServer"
