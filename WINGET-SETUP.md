# WinGet Setup Guide for QuickNotes

This guide explains how to publish QuickNotes to WinGet for easy distribution.

## 📁 Files Created

1. **setup-template.iss** - Inno Setup script for creating installers
2. **build-exe.ps1** - PowerShell script to build x64 and ARM64 installers
3. **.github/workflows/release-extension.yml** - GitHub Actions for automated builds
4. **.github/workflows/update-winget.yml** - GitHub Actions for WinGet updates

## 🚀 Quick Start

### Prerequisites
- Windows 10/11
- .NET 9 SDK
- Inno Setup 6 (for local builds)
- GitHub CLI (`gh`)

### Local Build (Test)

```powershell
cd QuickNotes
.\build-exe.ps1 -Version "0.0.1.0"
```

This creates:
- `bin/Release/installer/QuickNotes-Setup-0.0.1.0-x64.exe`
- `bin/Release/installer/QuickNotes-Setup-0.0.1.0-arm64.exe`

### GitHub Actions Build (Recommended)

1. Push all files to GitHub
2. Go to Actions → "QuickNotes Extension - Build EXE Installer"
3. Click "Run workflow"
4. Enter version (or leave blank for auto-detect)
5. The workflow will:
   - Build both x64 and ARM64 installers
   - Create a GitHub Release
   - Upload the .exe files

## 📦 First WinGet Submission (Manual)

**Important**: First submission must be manual using `wingetcreate`

1. Install wingetcreate:
   ```powershell
   winget install Microsoft.WingetCreate
   ```

2. Get the GitHub Release URLs:
   - Go to your GitHub release
   - Right-click the x64 .exe → "Copy link address"
   - Right-click the arm64 .exe → "Copy link address"

3. Create the manifest:
   ```powershell
   wingetcreate new "https://github.com/QQSHI13/quick-notes/releases/download/QuickNotes-v0.0.1.0/QuickNotes-Setup-0.0.1.0-x64.exe" "https://github.com/QQSHI13/quick-notes/releases/download/QuickNotes-v0.0.1.0/QuickNotes-Setup-0.0.1.0-arm64.exe"
   ```

4. When prompted:
   - Press **Enter** to accept defaults for most fields
   - Answer **No** to optional modifications
   - Answer **Yes** to submit to WinGet repository

5. wingetcreate will:
   - Fork microsoft/winget-pkgs to your account
   - Create a new branch
   - Open a Pull Request automatically

6. Wait for WinGet team review (usually 1-3 days)

## 🔄 Future Updates (Automated)

After the first manual submission, updates are automated:

1. Create a new release using GitHub Actions
2. The `update-winget.yml` workflow will automatically:
   - Detect the new release
   - Update the WinGet manifest
   - Submit a PR to microsoft/winget-pkgs

Or manually trigger:
```powershell
gh workflow run update-winget.yml -f version="0.0.2.0"
```

## ✅ WinGet Manifest Requirements

Your manifest will include:
- **PackageIdentifier**: `QQSHI13.QuickNotesExtension`
- **Tags**: `windows-commandpalette-extension` (required for discovery)
- **Dependencies**: Windows App SDK (if needed)
- **Architectures**: x64 and ARM64

## 📝 Important Notes

- **CLSID**: `AB9CB241-4C93-413F-96AF-43B7F5EF8E47` (used in registry)
- **Installer Type**: EXE (Inno Setup)
- **Scope**: User (HKCU registry)
- **Elevation**: Required for COM server registration

## 🐛 Troubleshooting

### Build fails
- Ensure .NET 9 SDK is installed: `dotnet --version`
- Ensure Inno Setup is installed: `Test-Path "${env:ProgramFiles(x86)}\Inno Setup 6\iscc.exe"`

### WinGet submission fails
- Check that both x64 and ARM64 URLs are accessible
- Ensure version numbers match between GitHub release and manifest
- Verify the installer runs correctly before submitting

## 📚 References

- [Microsoft Docs: Publish Command Palette Extensions](https://learn.microsoft.com/en-us/windows/powertoys/command-palette/publish-extension)
- [WinGet Package Repository](https://github.com/microsoft/winget-pkgs)
- [Inno Setup Documentation](https://jrsoftware.org/ishelp/)
