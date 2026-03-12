# Quick Notes

A Windows Command Palette extension for quick note-taking.

## Features

- **Instant note creation** - Create timestamped markdown notes with a single command
- **Command Palette integration** - Access via Windows Command Palette (Win + Alt + Space)
- **Customizable notes directory** - Choose where your notes are saved
- **Opens in default editor** - Automatically opens notes in your preferred markdown editor

## Installation

1. Install [Windows Command Palette](https://github.com/microsoft/PowerToys) (part of PowerToys)
2. Download the latest release from [Releases](../../releases)
3. Install the `.msix` package
4. Restart Command Palette

## Usage

1. Open Command Palette: **Win + Alt + Space**
2. Type "Quick Notes" or "Create New Note"
3. Press Enter
4. A new markdown file will be created and opened in your default editor

## Configuration

The extension creates notes in your configured directory with timestamps:
```
Note_2026-03-12_14-30-00.md
```

Default location: `%USERPROFILE%\Documents\QuickNotes`

## Building from Source

### Requirements
- Windows 10/11 (Build 19041+)
- .NET 9 SDK
- Visual Studio 2022 (optional)

### Build
```bash
dotnet build
```

### Package
```bash
dotnet publish -c Release
```

## License

GPL-3.0 - See [LICENSE](LICENSE)

---

Built with ❤️ by QQ & [Nova ☄️](https://qqshi13.github.io/nova/)
