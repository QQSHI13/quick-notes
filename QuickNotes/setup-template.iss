; Inno Setup Script for QuickNotes Command Palette Extension
; This creates an installer for WinGet distribution

#define AppVersion "0.0.2.0"

[Setup]
AppId={{3c7f3c8e-2b4a-4c8d-9e1f-5a6b7c8d9e0f}}
AppName=Quick Notes Extension
AppVersion={#AppVersion}
AppPublisher=QQ
DefaultDirName={autopf}\QuickNotes
OutputDir=bin\Release\installer
OutputBaseFilename=QuickNotes-Setup-{#AppVersion}
Compression=lzma
SolidCompression=yes
MinVersion=10.0.19041
; Architectures will be set by build script for x64/arm64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "bin\Release\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\Quick Notes"; Filename: "{app}\QuickNotes.exe"

[Registry]
Root: HKCU; Subkey: "SOFTWARE\Classes\CLSID\{{3c7f3c8e-2b4a-4c8d-9e1f-5a6b7c8d9e0f}}"; ValueData: "QuickNotes"
Root: HKCU; Subkey: "SOFTWARE\Classes\CLSID\{{3c7f3c8e-2b4a-4c8d-9e1f-5a6b7c8d9e0f}}\LocalServer32"; ValueData: "{app}\QuickNotes.exe -RegisterProcessAsComServer"
