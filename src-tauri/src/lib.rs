use tauri::{
    menu::{Menu, MenuItem},
    tray::{MouseButton, MouseButtonState, TrayIconBuilder},
    Manager, Emitter, 
};
use chrono::Local;
use dirs;

// Save note to ~/notes/ with timestamp filename
#[tauri::command]
fn save_note(content: String) -> Result<String, String> {
    let home_dir = dirs::home_dir().ok_or("Could not get home directory")?;
    let notes_dir = home_dir.join("notes");
    
    // Create notes directory if it doesn't exist
    std::fs::create_dir_all(&notes_dir).map_err(|e| format!("Failed to create notes directory: {}", e))?;
    
    // Generate filename with timestamp: YYYY-MM-DD_HH-MM-SS.md
    let timestamp = Local::now().format("%Y-%m-%d_%H-%M-%S").to_string();
    let filename = format!("{}.md", timestamp);
    let filepath = notes_dir.join(&filename);
    
    // Write the note content
    std::fs::write(&filepath, content).map_err(|e| format!("Failed to write file: {}", e))?;
    
    Ok(filepath.to_string_lossy().to_string())
}

// Get the notes directory path
#[tauri::command]
fn get_notes_dir() -> Result<String, String> {
    let home_dir = dirs::home_dir().ok_or("Could not get home directory")?;
    let notes_dir = home_dir.join("notes");
    Ok(notes_dir.to_string_lossy().to_string())
}

// Show the main window
fn show_window(app: &tauri::AppHandle) {
    if let Some(window) = app.get_webview_window("main") {
        let _ = window.show();
        let _ = window.set_focus();
        let _ = window.center();
    }
}

pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_opener::init())
        .plugin(tauri_plugin_fs::init())
        .plugin(tauri_plugin_global_shortcut::Builder::new().build())
        .invoke_handler(tauri::generate_handler![save_note, get_notes_dir])
        .setup(|app| {
            // Create menu items for the tray
            let open_item = MenuItem::with_id(app, "open", "Open", true, None::<&str>)?;
            let quit_item = MenuItem::with_id(app, "quit", "Quit", true, None::<&str>)?;
            
            // Create the tray menu
            let menu = Menu::with_items(app, &[&open_item, &quit_item])?;
            
            // Create the tray icon
            let _tray = TrayIconBuilder::new()
                .menu(&menu)
                .show_menu_on_left_click(false)
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
                .build(app)?;
            
            // Register global hotkey (Ctrl+Shift+N)
            use tauri_plugin_global_shortcut::{Shortcut, Code, Modifiers};
            
            let shortcut = Shortcut::new(Some(Modifiers::CONTROL | Modifiers::SHIFT), Code::KeyN);
            let app_handle = app.handle().clone();
            
            app.global_shortcut()
                .on_shortcut(shortcut, move |app, _shortcut, _event| {
                    show_window(app);
                    // Also emit an event to clear the editor
                    let _ = app.emit("new-note", ());
                })?;
            
            // Hide window instead of closing when X is clicked
            let window = app.get_webview_window("main").unwrap();
            let window_clone = window.clone();
            window.on_window_event(move |event| {
                if let tauri::WindowEvent::CloseRequested { api, .. } = event {
                    api.prevent_close();
                    let _ = window_clone.hide();
                }
            });
            
            Ok(())
        })
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
