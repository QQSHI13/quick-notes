# Quick Notes 📝

A Windows Command Palette extension for instant note-taking. Create timestamped markdown notes with a single keystroke.

---

## ✨ Features

- **⚡ Instant Note Creation** — Create timestamped markdown notes with a single command
- **🎨 Command Palette Integration** — Access via Windows Command Palette (Win + Alt + Space)
- **📁 Customizable Directory** — Choose where your notes are saved
- **🖊️ Opens in Default Editor** — Automatically opens notes in your preferred markdown editor
- **🔍 Quick Search** — Find and open existing notes from the Command Palette
- **📅 Date-Based Organization** — Notes organized by date for easy retrieval

---

## 📥 Installation

### Method 0: Microsoft Store — Easiest

Install directly from the Microsoft Store:  
[Quick Notes on Microsoft Store](https://apps.microsoft.com/store/detail/9P8M54K17J8L?cid=DevShareMCLPCS)

### Method 1: Windows Package Manager (winget) — Recommended

```powershell
winget install QuickNotes
```

### Method 2: Manual Install

1. Install [PowerToys](https://github.com/microsoft/PowerToys) (includes Windows Command Palette)
2. Download the latest `.msixbundle` from [Releases](https://github.com/QQSHI13/quick-notes/releases)
3. Double-click to install the package
4. Restart Command Palette (Win + Alt + Space)

---

## 🚀 Usage

1. Press `Win + Alt + Space` to open Windows Command Palette
2. Type "Quick Notes" or "note"
3. Select an action:
   - **Create Note** — Creates a new note with current timestamp
   - **Open Notes Folder** — Opens your notes directory in File Explorer
   - **Search Notes** — Find existing notes by name or content

### Default Hotkey

- `Win + Alt + Space` → Open Command Palette
- Type `note` → Quick Notes commands

---

## 🛠️ Building from Source

### Prerequisites

- Windows 11 (for Command Palette support)
- Visual Studio 2022 with Windows SDK
- .NET 8.0 or later

### Build Steps

```powershell
# Clone the repository
git clone https://github.com/QQSHI13/quick-notes.git
cd quick-notes

# Build the solution
dotnet build QuickNotes.sln

# Create MSIX bundle
.\build-bundle.ps1
```

See [BUILD.md](./BUILD.md) for detailed build instructions.

---

## ⚙️ Configuration

After installation, configure your notes directory:

1. Open Command Palette (`Win + Alt + Space`)
2. Search for "Quick Notes Settings"
3. Set your preferred notes folder path
4. (Optional) Configure your default markdown editor

---

## 🛠️ Technologies

- **Framework**: .NET 8.0
- **UI**: Windows App SDK / WinUI 3
- **Platform**: Windows Command Palette (PowerToys)
- **Language**: C#

---

## 📝 License

This project is licensed under the **GNU General Public License v3.0 (GPL-3.0)**.

See [LICENSE](./LICENSE) for details.

---

## 🙏 Credits

Built with ❤️ by **QQ** and **Nova** ☄️

Powered by [OpenClaw](https://openclaw.ai)

---

## 🐛 Issues & Feature Requests

Found a bug or have an idea? [Open an issue](https://github.com/QQSHI13/quick-notes/issues) on GitHub!
