# Quick Notes (qnotes)

A lightweight, fast desktop note-taking app with global hotkey support. Built with Tauri for Windows (with cross-platform support).

![Quick Notes](screenshot.png)

## Features

- ⚡ **Global Hotkey**: Press `Ctrl+Shift+N` anywhere to instantly open a new note
- 🪟 **Always on Top**: Floating window stays visible while you work
- 📝 **Markdown Support**: Type notes in markdown with live preview
- 💾 **Auto-save**: Notes automatically saved to `~/notes/` with timestamp filenames
- 🎯 **System Tray**: Minimize to system tray, right-click for quick actions
- 🌙 **Dark Theme**: Easy on the eyes, matches modern dev tools
- 🎨 **Clean UI**: Minimal, distraction-free interface

## Installation

### Windows

1. Download the latest release from [Releases](../../releases)
2. Run the `.msi` installer or use the portable `.exe`
3. The app will start automatically and add itself to system tray

### Build from Source

#### Prerequisites

- [Node.js](https://nodejs.org/) 18+
- [Rust](https://rustup.rs/)
- Platform-specific dependencies:
  - **Windows**: Microsoft Visual C++ Build Tools
  - **Linux**: `libwebkit2gtk-4.1-dev`, `libgtk-3-dev`, `pkg-config`
  - **macOS**: Xcode Command Line Tools

#### Build Steps

```bash
# Clone the repository
git clone https://github.com/yourusername/quick-notes.git
cd quick-notes

# Install dependencies
npm install

# Run in development mode
npm run dev

# Build for production
npm run build
```

The built application will be in `src-tauri/target/release/bundle/`.

## Usage

### Global Hotkey
- `Ctrl+Shift+N` - Open Quick Notes from anywhere

### Keyboard Shortcuts
| Shortcut | Action |
|----------|--------|
| `Ctrl+S` | Save note immediately |
| `Ctrl+P` | Toggle markdown preview |
| `Esc` | Hide window (minimize to tray) |

### Mouse
- **Drag title bar**: Move the window
- **Resize edges**: Resize the window
- **Click tray icon**: Show/hide the app
- **Right-click tray icon**: Open menu

### Note Storage
Notes are saved to:
- Windows: `%USERPROFILE%\notes\`
- Linux/macOS: `~/notes/`

Files are named with timestamps: `YYYY-MM-DD_HH-MM-SS.md`

## Development

### Project Structure

```
quick-notes/
├── src/                  # Frontend code
│   ├── index.html       # Main UI
│   ├── main.js          # App logic
│   └── styles.css       # Dark theme styles
├── src-tauri/           # Rust backend
│   ├── src/lib.rs       # Main application code
│   └── Cargo.toml       # Rust dependencies
└── package.json         # Node.js dependencies
```

### Tech Stack

- **Tauri v2**: Desktop app framework (Rust + Web)
- **Vanilla JS**: No heavy frontend framework
- **Marked.js**: Markdown parsing
- **Tauri Plugins**:
  - `global-shortcut`: System-wide hotkeys
  - `fs`: File system access
  - `tray`: System tray integration

### Customization

Edit `src-tauri/tauri.conf.json` to change:
- Window size and behavior
- App identifier
- Bundle settings

Edit `src/styles.css` to customize the theme.

## Auto-Start (Optional)

To make Quick Notes start with Windows:

1. Press `Win+R`, type `shell:startup`, press Enter
2. Create a shortcut to `Quick Notes.exe`
3. Copy the shortcut to the Startup folder

## Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

MIT License - see [LICENSE](LICENSE) for details.

## Acknowledgments

- Built with [Tauri](https://tauri.app/)
- Markdown parsing by [Marked](https://marked.js.org/)
