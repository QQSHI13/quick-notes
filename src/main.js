const { invoke } = window.__TAURI__.core;
const { listen } = window.__TAURI__.event;
const { getCurrentWindow } = window.__TAURI__.window;

// DOM Elements
const editorInput = document.getElementById('editor-input');
const editorPreview = document.getElementById('editor-preview');
const statusMessage = document.getElementById('status-message');
const statusDot = document.getElementById('status-dot');
const wordCount = document.getElementById('word-count');
const saveBtn = document.getElementById('save-btn');
const newNoteBtn = document.getElementById('new-note-btn');
const minimizeBtn = document.getElementById('minimize-btn');
const closeBtn = document.getElementById('close-btn');
const alwaysOnTopBtn = document.getElementById('always-on-top-btn');
const formatBtns = document.querySelectorAll('.format-btn');

// State
let autoSaveTimeout = null;
let currentWindow = null;
let isUpdating = false;
let scrollTimeout = null;
let statusTimeout = null;
let isPreviewVisible = true;

// Configure marked for GitHub-flavored markdown
function configureMarked() {
  // Check if marked is loaded
  if (typeof marked === 'undefined') {
    console.error('Marked library not loaded');
    return false;
  }
  
  // Use marked.parse with options (v5+ compatible)
  marked.use({
    gfm: true,
    breaks: true
  });
  
  return true;
}

// Initialize
async function init() {
  try {
    currentWindow = getCurrentWindow();
    
    // Configure marked
    if (!configureMarked()) {
      setStatus('Error: Markdown library failed to load', 'error');
      return;
    }
    
    // Load saved content if any
    const savedContent = sessionStorage.getItem('currentNote');
    if (savedContent) {
      editorInput.value = savedContent;
      await updatePreview();
      updateWordCount();
    }
    
    // Setup event listeners
    setupEventListeners();
    
    // Listen for new-note event from global hotkey
    listen('new-note', () => {
      newNote();
    }).catch(err => {
      console.error('Failed to listen for new-note event:', err);
    });
    
    // Initial render
    await updatePreview();
    
    // Focus editor
    editorInput.focus();
    
    // Check initial always-on-top state
    updateAlwaysOnTopButton();
    
    setStatus('Ready');
  } catch (error) {
    console.error('Initialization error:', error);
    setStatus('Error initializing app', 'error');
  }
}

function setupEventListeners() {
  // Editor input - live preview update
  editorInput.addEventListener('input', handleInput);
  
  // Sync scroll positions with throttling
  editorInput.addEventListener('scroll', handleScroll);
  
  // Keyboard shortcuts
  editorInput.addEventListener('keydown', handleKeyDown);
  
  // Format toolbar buttons
  formatBtns.forEach(btn => {
    btn.addEventListener('click', () => {
      const action = btn.dataset.action;
      if (action) {
        applyFormat(action);
      }
    });
  });
  
  // Action buttons
  saveBtn?.addEventListener('click', saveNote);
  newNoteBtn?.addEventListener('click', newNote);
  minimizeBtn?.addEventListener('click', () => currentWindow?.minimize());
  closeBtn?.addEventListener('click', () => currentWindow?.hide());
  alwaysOnTopBtn?.addEventListener('click', toggleAlwaysOnTop);
  
  // Handle window events
  window.addEventListener('beforeunload', cleanup);
}

function cleanup() {
  // Clear all timeouts
  if (autoSaveTimeout) {
    clearTimeout(autoSaveTimeout);
    autoSaveTimeout = null;
  }
  if (scrollTimeout) {
    clearTimeout(scrollTimeout);
    scrollTimeout = null;
  }
  if (statusTimeout) {
    clearTimeout(statusTimeout);
    statusTimeout = null;
  }
}

async function handleInput() {
  if (isUpdating) return;
  
  isUpdating = true;
  
  try {
    await updatePreview();
    updateWordCount();
    
    // Auto-save to session storage
    sessionStorage.setItem('currentNote', editorInput.value);
    
    // Schedule file auto-save
    if (autoSaveTimeout) {
      clearTimeout(autoSaveTimeout);
    }
    
    autoSaveTimeout = setTimeout(() => {
      autoSave();
    }, 5000);
    
    setStatus('Unsaved changes', 'saving');
  } finally {
    isUpdating = false;
  }
}

function handleScroll() {
  // Throttle scroll sync
  if (scrollTimeout) {
    clearTimeout(scrollTimeout);
  }
  
  scrollTimeout = setTimeout(() => {
    editorPreview.scrollTop = editorInput.scrollTop;
  }, 16); // ~60fps
}

function handleKeyDown(e) {
  const isMac = navigator.platform.includes('Mac');
  const ctrl = isMac ? e.metaKey : e.ctrlKey;
  const shift = e.shiftKey;
  const alt = e.altKey;
  
  // Ctrl+B: Bold
  if (ctrl && e.key === 'b' && !shift && !alt) {
    e.preventDefault();
    applyFormat('bold');
    return;
  }
  
  // Ctrl+I: Italic
  if (ctrl && e.key === 'i' && !shift && !alt) {
    e.preventDefault();
    applyFormat('italic');
    return;
  }
  
  // Ctrl+K: Link
  if (ctrl && e.key === 'k' && !shift && !alt) {
    e.preventDefault();
    applyFormat('link');
    return;
  }
  
  // Ctrl+Shift+C: Code block (check for lowercase 'c' with shift)
  if (ctrl && shift && e.key === 'c') {
    e.preventDefault();
    applyFormat('codeblock');
    return;
  }
  
  // Ctrl+S: Save
  if (ctrl && e.key === 's' && !shift && !alt) {
    e.preventDefault();
    saveNote();
    return;
  }
  
  // Ctrl+N: New note
  if (ctrl && e.key === 'n' && !shift && !alt) {
    e.preventDefault();
    newNote();
    return;
  }
  
  // Ctrl+H: Heading
  if (ctrl && e.key === 'h' && !shift && !alt) {
    e.preventDefault();
    applyFormat('heading');
    return;
  }
  
  // Ctrl+L: List
  if (ctrl && e.key === 'l' && !shift && !alt) {
    e.preventDefault();
    applyFormat('list');
    return;
  }
  
  // Ctrl+Shift+Q: Quote
  if (ctrl && shift && e.key === 'q') {
    e.preventDefault();
    applyFormat('quote');
    return;
  }
  
  // Ctrl+P or Ctrl+Shift+P: Toggle always on top / Toggle preview
  if (ctrl && e.key === 'p' && !alt) {
    e.preventDefault();
    if (shift) {
      // Ctrl+Shift+P: Toggle preview visibility
      togglePreview();
    } else {
      // Ctrl+P: Toggle always on top
      toggleAlwaysOnTop();
    }
    return;
  }
  
  // Escape: Hide to tray
  if (e.key === 'Escape' && !ctrl && !shift && !alt) {
    e.preventDefault();
    currentWindow?.hide();
    return;
  }
  
  // Tab key handling for lists/code
  if (e.key === 'Tab' && !ctrl && !alt) {
    e.preventDefault();
    handleTab(e.shiftKey);
    return;
  }
  
  // Enter key handling for auto-continuation
  if (e.key === 'Enter' && !ctrl && !alt) {
    handleEnter(e);
  }
}

function handleTab(shift) {
  const start = editorInput.selectionStart;
  const end = editorInput.selectionEnd;
  const value = editorInput.value;
  const selectedText = value.substring(start, end);
  
  if (start !== end) {
    // Multi-line selection
    const lines = selectedText.split('\n');
    if (shift) {
      // Outdent
      const modifiedLines = lines.map(line => 
        line.startsWith('  ') ? line.substring(2) : line.replace(/^\t/, '')
      );
      const newText = modifiedLines.join('\n');
      editorInput.setRangeText(newText, start, end, 'select');
    } else {
      // Indent
      const modifiedLines = lines.map(line => '  ' + line);
      const newText = modifiedLines.join('\n');
      editorInput.setRangeText(newText, start, end, 'select');
    }
  } else {
    // Single cursor - insert tab/spaces
    if (shift) {
      // Try to remove indentation at cursor
      const lineStart = value.lastIndexOf('\n', start - 1) + 1;
      const beforeCursor = value.substring(lineStart, start);
      if (beforeCursor.startsWith('  ')) {
        editorInput.setRangeText('', lineStart, lineStart + 2, 'end');
      } else if (beforeCursor.startsWith('\t')) {
        editorInput.setRangeText('', lineStart, lineStart + 1, 'end');
      }
    } else {
      editorInput.setRangeText('  ', start, end, 'end');
    }
  }
  
  updatePreview();
  updateWordCount();
}

function handleEnter(e) {
  const start = editorInput.selectionStart;
  const value = editorInput.value;
  const lineStart = value.lastIndexOf('\n', start - 1) + 1;
  const currentLine = value.substring(lineStart, start);
  
  // Check for list items
  const listMatch = currentLine.match(/^(\s*)([-*]|\d+\.)\s+/);
  if (listMatch) {
    e.preventDefault();
    const [, indent, marker] = listMatch;
    
    // If line is empty after marker, remove the marker
    if (currentLine.trim() === marker) {
      editorInput.setRangeText('\n', lineStart, start, 'end');
    } else {
      // Continue the list
      let newMarker = marker;
      if (/^\d+\./.test(marker)) {
        const num = parseInt(marker) + 1;
        newMarker = num + '.';
      }
      editorInput.setRangeText('\n' + indent + newMarker + ' ', start, start, 'end');
    }
    updatePreview();
    updateWordCount();
    return;
  }
  
  // Check for blockquotes
  const quoteMatch = currentLine.match(/^(\s*\>\s*)/);
  if (quoteMatch) {
    e.preventDefault();
    if (currentLine.trim() === '>') {
      editorInput.setRangeText('\n', lineStart, start, 'end');
    } else {
      editorInput.setRangeText('\n' + quoteMatch[1], start, start, 'end');
    }
    updatePreview();
    updateWordCount();
  }
}

function applyFormat(format) {
  const start = editorInput.selectionStart;
  const end = editorInput.selectionEnd;
  const value = editorInput.value;
  const selectedText = value.substring(start, end);
  
  let newText = '';
  let cursorOffset = 0;
  let newCursorPos = null;
  
  switch (format) {
    case 'bold':
      if (selectedText.startsWith('**') && selectedText.endsWith('**')) {
        newText = selectedText.slice(2, -2);
        cursorOffset = -2;
      } else {
        newText = `**${selectedText || 'bold text'}**`;
        cursorOffset = selectedText ? 0 : -2;
      }
      break;
      
    case 'italic':
      if (selectedText.startsWith('*') && selectedText.endsWith('*') && 
          !(selectedText.startsWith('**') && selectedText.endsWith('**'))) {
        newText = selectedText.slice(1, -1);
        cursorOffset = -1;
      } else {
        newText = `*${selectedText || 'italic text'}*`;
        cursorOffset = selectedText ? 0 : -1;
      }
      break;
      
    case 'code':
      if (selectedText.startsWith('`') && selectedText.endsWith('`')) {
        newText = selectedText.slice(1, -1);
        cursorOffset = -1;
      } else {
        newText = `\`${selectedText || 'code'}\``;
        cursorOffset = selectedText ? 0 : -1;
      }
      break;
      
    case 'link':
      if (selectedText) {
        newText = `[${selectedText}](url)`;
        cursorOffset = -1;
      } else {
        newText = '[link text](url)';
        cursorOffset = -5;
      }
      break;
      
    case 'codeblock':
      if (selectedText.includes('\n')) {
        newText = `\`\`\`\n${selectedText}\n\`\`\``;
        cursorOffset = 0;
      } else {
        newText = '\`\`\`\n// code here\n\`\`\`';
        cursorOffset = -14;
      }
      break;
      
    case 'heading': {
      // Check if current line is already a heading
      const lineStart = value.lastIndexOf('\n', start - 1) + 1;
      const currentLine = value.substring(lineStart, start);
      const headingMatch = currentLine.match(/^#{1,6}\s*/);
      
      if (headingMatch) {
        // Remove heading or increase level
        const currentLevel = headingMatch[0].trim().length;
        if (currentLevel < 6) {
          newText = '#' + headingMatch[0] + value.substring(start, end);
          cursorOffset = 1;
        } else {
          newText = value.substring(start, end);
          cursorOffset = -headingMatch[0].length;
        }
        editorInput.setRangeText(newText, lineStart + headingMatch[0].length, end, 'end');
        updatePreview();
        updateWordCount();
        return;
      } else {
        newText = `# ${selectedText || 'Heading'}`;
        cursorOffset = selectedText ? 0 : 0;
      }
      break;
    }
      
    case 'quote':
      newText = selectedText.split('\n').map(line => '> ' + line).join('\n');
      cursorOffset = 0;
      break;
      
    case 'list':
      newText = selectedText.split('\n').map(line => '- ' + line).join('\n');
      cursorOffset = 0;
      break;
      
    default:
      return;
  }
  
  editorInput.setRangeText(newText, start, end, 'end');
  
  // Adjust cursor position
  if (!selectedText && cursorOffset !== 0) {
    const newPos = editorInput.selectionStart + cursorOffset;
    editorInput.setSelectionRange(newPos, newPos);
  }
  
  updatePreview();
  updateWordCount();
  editorInput.focus();
}

async function updatePreview() {
  if (!isPreviewVisible) return;
  
  const content = editorInput.value;
  
  try {
    // marked.parse returns a Promise in v5+, await it
    const html = await marked.parse(content || '');
    editorPreview.innerHTML = html;
  } catch (error) {
    console.error('Markdown parsing error:', error);
    editorPreview.textContent = content; // Fallback to plain text
  }
}

function updateWordCount() {
  const text = editorInput.value.trim();
  const count = text ? text.split(/\s+/).filter(w => w.length > 0).length : 0;
  wordCount.textContent = `${count} word${count !== 1 ? 's' : ''}`;
}

function setStatus(message, type = 'normal') {
  // Clear any existing status timeout
  if (statusTimeout) {
    clearTimeout(statusTimeout);
    statusTimeout = null;
  }
  
  statusMessage.textContent = message;
  statusDot.className = 'status-indicator' + (type === 'saving' ? ' saving' : '');
  
  const colors = {
    saved: 'var(--status-success)',
    error: 'var(--accent-red)',
    saving: 'var(--accent-yellow)',
    normal: 'var(--accent-blue)'
  };
  
  statusDot.style.background = colors[type] || colors.normal;
}

function setTemporaryStatus(message, type, duration = 2000) {
  setStatus(message, type);
  
  statusTimeout = setTimeout(() => {
    setStatus('Ready');
  }, duration);
}

async function updateAlwaysOnTopButton() {
  if (!currentWindow) return;
  
  try {
    const isAlwaysOnTop = await currentWindow.isAlwaysOnTop();
    alwaysOnTopBtn?.classList.toggle('active', isAlwaysOnTop);
  } catch (error) {
    console.error('Failed to get always-on-top state:', error);
  }
}

function togglePreview() {
  isPreviewVisible = !isPreviewVisible;
  editorPreview.style.display = isPreviewVisible ? 'block' : 'none';
  
  if (isPreviewVisible) {
    updatePreview();
    setTemporaryStatus('Preview on', 'normal', 1500);
  } else {
    setTemporaryStatus('Preview off', 'normal', 1500);
  }
}

async function toggleAlwaysOnTop() {
  if (!currentWindow) {
    setTemporaryStatus('Window not ready', 'error');
    return;
  }
  
  try {
    const isAlwaysOnTop = await currentWindow.isAlwaysOnTop();
    await currentWindow.setAlwaysOnTop(!isAlwaysOnTop);
    alwaysOnTopBtn?.classList.toggle('active', !isAlwaysOnTop);
    setTemporaryStatus(!isAlwaysOnTop ? 'Always on top' : 'Not always on top', 'saved', 1500);
  } catch (error) {
    console.error('Failed to toggle always-on-top:', error);
    setTemporaryStatus('Failed to toggle', 'error');
  }
}

async function autoSave() {
  const content = editorInput.value.trim();
  if (!content) return;
  
  try {
    setStatus('Auto-saving...', 'saving');
    await invoke('save_note', { content });
    setStatus('Auto-saved', 'saved');
    sessionStorage.removeItem('currentNote');
    
    statusTimeout = setTimeout(() => {
      setStatus('Ready');
    }, 2000);
  } catch (error) {
    console.error('Auto-save failed:', error);
    setStatus('Auto-save failed', 'error');
  }
}

async function saveNote() {
  const content = editorInput.value.trim();
  if (!content) {
    setTemporaryStatus('Nothing to save', 'error');
    return;
  }
  
  // Clear auto-save timeout since we're manually saving
  if (autoSaveTimeout) {
    clearTimeout(autoSaveTimeout);
    autoSaveTimeout = null;
  }
  
  try {
    setStatus('Saving...', 'saving');
    const filepath = await invoke('save_note', { content });
    setStatus(`Saved: ${filepath.split('/').pop()}`, 'saved');
    sessionStorage.removeItem('currentNote');
    
    statusTimeout = setTimeout(() => {
      setStatus('Ready');
    }, 3000);
  } catch (error) {
    console.error('Save failed:', error);
    setStatus(`Save failed: ${error}`, 'error');
  }
}

function newNote() {
  // Clear auto-save timeout
  if (autoSaveTimeout) {
    clearTimeout(autoSaveTimeout);
    autoSaveTimeout = null;
  }
  
  editorInput.value = '';
  sessionStorage.removeItem('currentNote');
  updatePreview();
  updateWordCount();
  setTemporaryStatus('New note created', 'normal');
  editorInput.focus();
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', init);

// Handle errors gracefully
window.addEventListener('error', (e) => {
  console.error('Global error:', e.error);
  setStatus('An error occurred', 'error');
});

window.addEventListener('unhandledrejection', (e) => {
  console.error('Unhandled rejection:', e.reason);
  setStatus('An error occurred', 'error');
});
