# Changelog

All notable changes to Quick Notes will be documented here.

## [0.3.5.0] - 2026-06-27

### Added
- **White `#` icon** — rendered from the Material pound.svg at all scales; clean, readable, no-background glyph.
- **Auto-detected installed editors** — `EditorConfigurationPage` now scans common install paths (VS Code, Obsidian, Notepad++, Typora, Sublime, Cursor) and lists each found one with its full path, alongside the quick presets (Notepad, WordPad).
- **Visible settings validation** — clicking "Validate Settings" opens a results page showing each issue (or "All settings are valid") instead of logging to invisible `Debug.WriteLine`.
- **Store-submission workflow** — `.github/workflows/store-submission.yml` builds an unsigned multi-arch (x64 + arm64) `.msixbundle` for upload to Partner Center.
- **Arm64 support** — the release workflow now builds both x64 and arm64 bundles (`v*` tags).
- **Single-source version** — `-p:QuickNotesVersion=x.y.z.w` bumps the MSIX package without touching the manifest; default lives in `Directory.Build.props`.

### Fixed
- **Parent-menu refresh** — `ListPage.RaiseItemsChanged()` is called after Delete / Sync Title / Create New, so child actions no longer leave stale state visible.
- **Settings cache staleness** — `SettingsService.GetSettings()` now detects file changes and reloads instead of holding the cached value forever.
- **"Configure Editor"** — previously a no-op toast; now wired to `EditorConfigurationPage` with working presets and auto-detection.
- **No-op `ConfigureEditorCommand` removed** — replaced by the working editor page.
- **Reset Directory + editor changes now refresh the settings page** immediately.
- **APPX1707 winmd warning suppressed** — `<AppxHarvestWinmdRegistration>false</AppxHarvestWinmdRegistration>` (the COM server is declared explicitly in the manifest).
- **Release workflow cert subject fixed** — the CI self-signed cert now matches the manifest publisher (`CN=B5D1629E-…`) so the signed package installs correctly.

### Changed
- **Icon** → white `#` glyph on transparent (Material pound SVG).
- **`QuickNotesVersion`** is the single source of truth for MSIX version; `Package.appxmanifest` Version is overwritten at build time.
- **PublishSingleFile** gated on `$(IsPublishing)` so plain `build`/`test` don't require a RuntimeIdentifier.

### Removed
- ~79 MB of tracked build artifacts (`AppPackages/`, `msix_contents/`, `BundleInput/`, …) — now gitignored.
- `ConfigureEditorCommand` (dead no-op).
- `ValidateSettingsCommand` (replaced by `ValidationResultsPage`).
- Dead "type a custom path" UI in editor config (no text-input Form API in the SDK).

## [0.3.0.0 - 0.3.0.5] - Pre-release

Initial development cycle: created the Command Palette extension, wired COM registration, added note commands, settings persistence, directory watching, and the original icon set. See the commit history for details.

[0.3.5.0]: https://github.com/QQSHI13/quick-notes/releases/tag/v0.3.5.0