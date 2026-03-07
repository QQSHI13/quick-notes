use tauri::{
    menu::{Menu, MenuItem},
    tray::{MouseButton, MouseButtonState, TrayIconBuilder},
    Manager, Emitter,
};
use tauri_plugin_global_shortcut::{Code, Modifiers, ShortcutState, GlobalShortcutExt};
use chrono::Local;
use dirs;

// Load tray icon at compile time
const TRAY_ICON: &[u8] = include_bytes!("../icons/icon.png");

// Save note to ~/notes/ with timestamp filename
#[tauri::command]
fn save_note(content: String) -> Result<String, String> {
    if content.trim().is_empty() {
        return Err("Cannot save empty note".to_string());
    }
    
    let home_dir = dirs::home_dir().ok_or("Could not get home directory")?;
    let notes_dir = home_dir.join("notes");
    
    // Create notes directory if it doesn't exist
    std::fs::create_dir_all(&notes_dir)
        .map_err(|e| format!("Failed to create notes directory: {}", e))?;
    
    // Generate filename with timestamp: YYYY-MM-DD_HH-MM-SS.md
    let timestamp = Local::now().format("%Y-%m-%d_%H-%M-%S").to_string();
    let filename = format!("{}.md", timestamp);
    let filepath = notes_dir.join(&filename);
    
    // Write the note content with proper error handling
    std::fs::write(&filepath, &content)
        .map_err(|e| format!("Failed to write file: {}", e))?;
    
    // Verify the file was written
    if !filepath.exists() {
        return Err("File was not created successfully".to_string());
    }
    
    Ok(filepath.to_string_lossy().to_string())
}

// Get the notes directory path
#[tauri::command]
fn get_notes_dir() -> Result<String, String> {
    let home_dir = dirs::home_dir().ok_or("Could not get home directory")?;
    let notes_dir = home_dir.join("notes");
    
    // Create directory if it doesn't exist
    std::fs::create_dir_all(&notes_dir)
        .map_err(|e| format!("Failed to create notes directory: {}", e))?;
    
    Ok(notes_dir.to_string_lossy().to_string())
}

// List all notes in the notes directory
#[tauri::command]
fn list_notes() -> Result<Vec<(String, String, u64)>, String> {
    let home_dir = dirs::home_dir().ok_or("Could not get home directory")?;
    let notes_dir = home_dir.join("notes");
    
    if !notes_dir.exists() {
        return Ok(vec![]);
    }
    
    let mut notes = vec![];
    let entries = std::fs::read_dir(&notes_dir)
        .map_err(|e| format!("Failed to read notes directory: {}", e))?;
    
    for entry in entries {
        if let Ok(entry) = entry {
            let path = entry.path();
            if path.extension().and_then(|e| e.to_str()) == Some("md") {
                if let Ok(metadata) = entry.metadata() {
                    if let Ok(modified) = metadata.modified() {
                        if let Ok(content) = std::fs::read_to_string(&path) {
                            let filename = path.file_stem()
                                .and_then(|s| s.to_str())
                                .unwrap_or("unknown")
                                .to_string();
                            let preview = content.lines().next()
                                .unwrap_or("")
                                .chars()
                                .take(50)
                                .collect::<String>();
                            let timestamp = modified
                                .duration_since(std::time::UNIX_EPOCH)
                                .unwrap_or_default()
                                .as_secs();
                            notes.push((filename, preview, timestamp));
                        }
                    }
                }
            }
        }
    }
    
    // Sort by timestamp descending (newest first)
    notes.sort_by(|a, b| b.2.cmp(&a.2));
    Ok(notes)
}

// Load a specific note by filename
#[tauri::command]
fn load_note(filename: String) -> Result<String, String> {
    let home_dir = dirs::home_dir().ok_or("Could not get home directory")?;
    let notes_dir = home_dir.join("notes");
    let filepath = notes_dir.join(format!("{}.md", filename));
    
    // Security check: ensure the file is within notes directory
    let canonical_path = filepath.canonicalize()
        .map_err(|e| format!("Invalid file path: {}", e))?;
    let canonical_notes_dir = notes_dir.canonicalize()
        .map_err(|e| format!("Invalid notes directory: {}", e))?;
    
    if !canonical_path.starts_with(&canonical_notes_dir) {
        return Err("Access denied: file outside notes directory".to_string());
    }
    
    std::fs::read_to_string(&canonical_path)
        .map_err(|e| format!("Failed to read note: {}", e))
}

// Delete a note by filename
#[tauri::command]
fn delete_note(filename: String) -> Result<(), String> {
    let home_dir = dirs::home_dir().ok_or("Could not get home directory")?;
    let notes_dir = home_dir.join("notes");
    let filepath = notes_dir.join(format!("{}.md", filename));
    
    // Security check
    let canonical_path = filepath.canonicalize()
        .map_err(|e| format!("Invalid file path: {}", e))?;
    let canonical_notes_dir = notes_dir.canonicalize()
        .map_err(|e| format!("Invalid notes directory: {}", e))?;
    
    if !canonical_path.starts_with(&canonical_notes_dir) {
        return Err("Access denied: file outside notes directory".to_string());
    }
    
    std::fs::remove_file(&canonical_path)
        .map_err(|e| format!("Failed to delete note: {}", e))
}
    std::fs::create_dir_all(&notes_dir)
        .map_err(|e| format!("Failed to create notes directory: {}", e))?;
    
    Ok(notes_dir.to_string_lossy().to_string())
}

// Show the main window
fn show_window(app: &tauri::AppHandle) {
    if let Some(window) = app.get_webview_window("main") {
        // Show window with proper error handling
        if let Err(e) = window.show() {
            eprintln!("Failed to show window: {}", e);
            return;
        }
        if let Err(e) = window.set_focus() {
            eprintln!("Failed to focus window: {}", e);
            return;
        }
        // Only center on first show, not on every hotkey press
        // Check if window is already visible to avoid repositioning
        match window.is_visible() {
            Ok(false) | Err(_) => {
                let _ = window.center();
            }
            _ => {}
        }
    }
}

pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_opener::init())
        .plugin(tauri_plugin_fs::init())
        .plugin(tauri_plugin_global_shortcut::Builder::new().build())
        .invoke_handler(tauri::generate_handler![save_note, get_notes_dir, list_notes, load_note, delete_note])
        .setup(|app| {
            // Create menu items for the tray
            let open_item = MenuItem::with_id(app, "open", "Open", true, None::<&str>)
                .map_err(|e| {
                    eprintln!("Failed to create open menu item: {}", e);
                    e
                })?;
            let quit_item = MenuItem::with_id(app, "quit", "Quit", true, None::<&str>)
                .map_err(|e| {
                    eprintln!("Failed to create quit menu item: {}", e);
                    e
                })?;
            
            // Create the tray menu
            let menu = Menu::with_items(app, &[&open_item, &quit_item])
                .map_err(|e| {
                    eprintln!("Failed to create tray menu: {}", e);
                    e
                })?;
            
            // Create the tray icon with proper error handling
            let tray_result = TrayIconBuilder::new()
                .icon(tauri::image::Image::from_bytes(TRAY_ICON).map_err(|e| {
                    eprintln!("Failed to load tray icon: {}", e);
                    e
                })?)
                .menu(&menu)
                .show_menu_on_left_click(false)  // Right-click shows menu
                .on_menu_event(|app, event| {
                    match event.id.as_ref() {
                        "open" => {
                            show_window(app);
                        }
                        "quit" => {
                            app.exit(0);
                        }
                        _ => {}
                    }
                })
                .on_tray_icon_event(|tray, event| {
                    if let tauri::tray::TrayIconEvent::Click {
                        button: MouseButton::Left,
                        button_state: MouseButtonState::Up,
                        ..
                    } = event
                    {
                        show_window(tray.app_handle());
                    }
                })
                .build(app);
            
            if let Err(e) = tray_result {
                eprintln!("Failed to create tray icon: {}. Continuing without tray.", e);
                // Continue without tray - app can still function
            }
            
            // Register global hotkey (Ctrl+Shift+N)
            let shortcut = tauri_plugin_global_shortcut::Shortcut::new(
                Some(Modifiers::CONTROL | Modifiers::SHIFT), 
                Code::KeyN
            );
            
            // Register the global shortcut with proper error handling
            match app.global_shortcut().register(shortcut) {
                Ok(_) => {
                    let handle = app.handle().clone();
                    if let Err(e) = app.global_shortcut().on_shortcut(shortcut, move |_app, _shortcut, event| {
                        if event.state == ShortcutState::Pressed {
                            show_window(&handle);
                            // Emit event to clear the editor
                            let _ = handle.emit("new-note", ());
                        }
                    }) {
                        eprintln!("Failed to set up global shortcut handler: {}", e);
                    }
                }
                Err(e) => {
                    eprintln!("Failed to register global shortcut: {}. App will work but hotkey won't be available.", e);
                    // Continue without global shortcut - app can still function
                }
            }
            
            // Hide window instead of closing when X is clicked
            if let Some(window) = app.get_webview_window("main") {
                let window_clone = window.clone();
                window.on_window_event(move |event| {
                    if let tauri::WindowEvent::CloseRequested { api, .. } = event {
                        api.prevent_close();
                        if let Err(e) = window_clone.hide() {
                            eprintln!("Failed to hide window: {}", e);
                        }
                    }
                });
                
                // Ensure window starts visible
                let _ = window.show();
                let _ = window.set_focus();
            }
            
            Ok(())
        })
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
