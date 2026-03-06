const { invoke } = window.__TAURI__.core;
const { listen } = window.__TAURI__.event;
const { getCurrentWindow } = window.__TAURI__.window;

// DOM Elements
const editor = document.getElementById('editor');
const preview = document.getElementById('preview');
const editorContainer = document.getElementById('editor-container');
const previewContainer = document.getElementById('preview-container');
const previewToggle = document.getElementById('preview-toggle');
const saveBtn = document.getElementById('save-btn');
const newNoteBtn = document.getElementById('new-note-btn');
const minimizeBtn = document.getElementById('minimize-btn');
const closeBtn = document.getElementById('close-btn');
const alwaysOnTopBtn = document.getElementById('always-on-top-btn');
const statusText = document.getElementById('status-text');
const wordCount = document.getElementById('word-count');

// State
let isPreviewMode = false;
let autoSaveTimeout = null;
let currentWindow = null;

// Initialize
async function init() {
  currentWindow = getCurrentWindow();
  
  // Load saved content if any (from session storage)
  const savedContent = sessionStorage.getItem('currentNote');
  if (savedContent) {
    editor.value = savedContent;
    updateWordCount();
  }
  
  // Setup event listeners
  setupEventListeners();
  
  // Listen for new-note event from global hotkey
  listen('new-note', () => {
    newNote();
  });
  
  // Focus editor
  editor.focus();
}

function setupEventListeners() {
  // Editor input - auto-save
  editor.addEventListener('input', () => {
    updateWordCount();
    
    // Auto-save to session storage
    sessionStorage.setItem('currentNote', editor.value);
    
    // Clear existing timeout
    if (autoSaveTimeout) {
      clearTimeout(autoSaveTimeout);
    }
    
    // Set new timeout for auto-save to file
    autoSaveTimeout = setTimeout(() => {
      autoSave();
    }, 5000); // Auto-save after 5 seconds of inactivity
    
    // Update preview if visible
    if (isPreviewMode) {
      updatePreview();
    }
  });
  
  // Preview toggle
  previewToggle.addEventListener('click', togglePreview);
  
  // Save button
  saveBtn.addEventListener('click', saveNote);
  
  // New note button
  newNoteBtn.addEventListener('click', newNote);
  
  // Window controls
  minimizeBtn.addEventListener('click', () => {
    currentWindow?.minimize();
  });
  
  closeBtn.addEventListener('click', () => {
    currentWindow?.hide();
  });
  
  // Always on top toggle
  alwaysOnTopBtn.addEventListener('click', toggleAlwaysOnTop);
  
  // Keyboard shortcuts
  document.addEventListener('keydown', (e) => {
    // Ctrl/Cmd + S to save
    if ((e.ctrlKey || e.metaKey) && e.key === 's') {
      e.preventDefault();
      saveNote();
    }
    
    // Ctrl/Cmd + P to toggle preview
    if ((e.ctrlKey || e.metaKey) && e.key === 'p') {
      e.preventDefault();
      togglePreview();
    }
    
    // Escape to close
    if (e.key === 'Escape') {
      currentWindow?.hide();
    }
  });
}

function updateWordCount() {
  const text = editor.value.trim();
  const count = text ? text.split(/\s+/).length : 0;
  wordCount.textContent = `${count} word${count !== 1 ? 's' : ''}`;
}

function updatePreview() {
  if (typeof marked !== 'undefined') {
    preview.innerHTML = marked.parse(editor.value);
  }
}

function togglePreview() {
  isPreviewMode = !isPreviewMode;
  
  if (isPreviewMode) {
    editorContainer.classList.add('hidden');
    previewContainer.classList.remove('hidden');
    previewToggle.classList.add('active');
    updatePreview();
  } else {
    editorContainer.classList.remove('hidden');
    previewContainer.classList.add('hidden');
    previewToggle.classList.remove('active');
    editor.focus();
  }
}

async function toggleAlwaysOnTop() {
  if (!currentWindow) return;
  
  const isAlwaysOnTop = await currentWindow.isAlwaysOnTop();
  await currentWindow.setAlwaysOnTop(!isAlwaysOnTop);
  alwaysOnTopBtn.classList.toggle('active', !isAlwaysOnTop);
}

async function autoSave() {
  const content = editor.value.trim();
  if (!content) return;
  
  try {
    statusText.textContent = 'Auto-saving...';
    await invoke('save_note', { content });
    statusText.textContent = 'Auto-saved';
    setTimeout(() => {
      statusText.textContent = 'Ready';
    }, 2000);
  } catch (error) {
    console.error('Auto-save failed:', error);
    statusText.textContent = 'Auto-save failed';
  }
}

async function saveNote() {
  const content = editor.value.trim();
  if (!content) {
    statusText.textContent = 'Nothing to save';
    setTimeout(() => {
      statusText.textContent = 'Ready';
    }, 2000);
    return;
  }
  
  try {
    statusText.textContent = 'Saving...';
    const filepath = await invoke('save_note', { content });
    statusText.textContent = `Saved to notes folder`;
    
    // Clear session storage after successful save
    sessionStorage.removeItem('currentNote');
    
    setTimeout(() => {
      statusText.textContent = 'Ready';
    }, 3000);
  } catch (error) {
    console.error('Save failed:', error);
    statusText.textContent = 'Save failed: ' + error;
  }
}

function newNote() {
  editor.value = '';
  sessionStorage.removeItem('currentNote');
  updateWordCount();
  statusText.textContent = 'New note';
  
  if (isPreviewMode) {
    togglePreview();
  }
  
  editor.focus();
  
  setTimeout(() => {
    statusText.textContent = 'Ready';
  }, 2000);
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', init);
