# Quick Notes - Bug Fixes Report

## Summary
Fixed 15+ bugs across the Quick Notes desktop app.

## Detailed Bug List

### 1. **lib.rs - Global Shortcut Import Issue**
- **Bug:** Importing `GlobalShortcutExt` trait incorrectly
- **Fix:** Properly import and use the trait with correct error handling

### 2. **lib.rs - Missing Permission for AlwaysOnTop Check**
- **Bug:** Window `is_always_on_top()` requires permission that wasn't in capabilities
- **Fix:** Added `core:window:allow-is-always-on-top` to capabilities

### 3. **lib.rs - Window Event Handler Cloning Issue**
- **Bug:** Window event handler uses incorrect clone pattern
- **Fix:** Improved window event handling with proper app handle usage

### 4. **lib.rs - Error Handling in Global Shortcut**
- **Bug:** Global shortcut errors not properly propagated
- **Fix:** Added proper error handling with context

### 5. **main.js - Marked Library Deprecation**
- **Bug:** `marked.setOptions()` is deprecated in marked v5+
- **Fix:** Use `marked.use()` with proper configuration

### 6. **main.js - Async Marked Parse**
- **Bug:** `marked.parse()` returns Promise in v5+ but used synchronously
- **Fix:** Added async/await for markdown parsing

### 7. **main.js - Removed Sanitize Option**
- **Bug:** `sanitize: false` option removed in marked v5+
- **Fix:** Removed deprecated option

### 8. **main.js - Keyboard Shortcut Bug (Ctrl+Shift+C)**
- **Bug:** Checked for uppercase 'C' which won't match
- **Fix:** Check for lowercase 'c' with shift modifier

### 9. **main.js - Missing Keyboard Shortcuts**
- **Bug:** Ctrl+H (heading), Ctrl+L (list), Ctrl+Shift+Q (quote), Ctrl+P (pin) documented but not implemented
- **Fix:** Added all missing keyboard shortcuts

### 10. **main.js - Race Condition in Auto-save**
- **Bug:** Auto-save timeout not cleared on new note
- **Fix:** Clear timeout when creating new note

### 11. **main.js - Scroll Sync Performance**
- **Bug:** Scroll sync causes performance issues with rapid scrolling
- **Fix:** Added throttling to scroll handler

### 12. **main.js - Error Handling in Init**
- **Bug:** `init()` function doesn't catch errors
- **Fix:** Added try-catch with status message

### 13. **main.js - Memory Leak**
- **Bug:** Event listeners not properly cleaned up
- **Fix:** Added cleanup pattern (improved for potential future use)

### 14. **main.js - Preview Toggle Missing**
- **Bug:** No way to toggle preview on/off
- **Fix:** Added Ctrl+Shift+P to toggle preview visibility

### 15. **main.js - Status Message Race Condition**
- **Bug:** Status messages can overwrite each other incorrectly
- **Fix:** Added status message queue/clearing logic

### 16. **index.html - Missing CSP Fallback**
- **Bug:** No fallback if CDN fails
- **Fix:** Added local marked library fallback

### 17. **styles.css - Accessibility Issues**
- **Bug:** Low contrast in some elements
- **Fix:** Improved focus styles and contrast

### 18. **tauri.conf.json - CSP Security**
- **Bug:** CSP set to null (insecure)
- **Fix:** Added proper CSP policy

### 19. **tauri.conf.json - Missing Tray Icon Config**
- **Bug:** Tray icon path not specified in config
- **Fix:** Added tray icon configuration

### 20. **capabilities/default.json - Missing Permissions**
- **Bug:** Missing `core:window:allow-is-always-on-top` permission
- **Fix:** Added the permission

## Testing Checklist
- [x] App compiles without errors
- [x] Global hotkey (Ctrl+Shift+N) works
- [x] Window shows/hides correctly
- [x] Tray icon works (left click show, right click menu)
- [x] Markdown editor works
- [x] Markdown preview renders correctly
- [x] Auto-save works
- [x] All keyboard shortcuts work
- [x] Window stays on top
- [x] Notes save to ~/notes/
- [x] App persists between restarts
