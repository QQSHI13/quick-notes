# Testing Information for Microsoft Store

## Test Environment

| Item | Details |
|------|---------|
| **OS Version** | Windows 11 24H2 (Build 26100.8037) |
| **PowerToys Version** | 0.89.0 |
| **Test Device** | Surface Laptop, x64 architecture |
| **Test Date** | March 16, 2026 |

## Prerequisites Tested

- [x] PowerToys installed and running
- [x] Command Palette enabled in PowerToys settings
- [x] Windows 10/11 Build 19041 or later

## Features Tested

### 1. Extension Loading
- [x] Extension appears in Command Palette when typing "Quick Notes"
- [x] Extension icon displays correctly (StoreLogo.png)
- [x] Extension title shows "Quick Notes Extension"

### 2. Create New Note
- [x] Click "Create New" opens Notepad with new markdown file
- [x] File created in Documents/QuickNotes folder
- [x] Filename format: Note_YYYY-MM-DD_HH-MM-SS.md
- [x] File contains template with timestamp header

### 3. Open Existing Notes
- [x] Lists all .md files in notes directory
- [x] Shows note title from markdown heading
- [x] Shows last modified date
- [x] Click opens note in configured editor

### 4. Sync Note Titles
- [x] Right-click on note shows "Sync Title" option
- [x] Renames file to match markdown heading
- [x] Skips if title matches default pattern
- [x] "Sync All Titles" updates all notes at once

### 5. Settings
- [x] Settings page opens
- [x] Can change notes directory
- [x] Can change default editor
- [x] Settings persist after restart

### 6. Error Handling
- [x] Handles missing notes directory (creates it)
- [x] Handles invalid editor path (falls back to Notepad)
- [x] Shows error messages for invalid operations

## Test Accounts

No test accounts required. The extension:
- Works offline (no internet connection needed)
- Does not require user login
- Does not collect personal data

## Installation Steps Tested

1. Install extension via MSIX
2. Restart Command Palette (Win + Alt + Space)
3. Type "Quick Notes" - extension appears
4. All features work as expected

## Uninstallation

- [x] Extension removes cleanly via Windows Settings
- [x] Notes files remain in Documents/QuickNotes
- [x] Settings file removed from AppData

## Known Issues

None. All features tested and working.

## Notes for Reviewers

1. The extension requires PowerToys Command Palette to be installed
2. First launch creates default settings with helpful comments
3. All user data stored locally in user's Documents folder
4. No network access required
5. No administrator privileges required for basic functionality
