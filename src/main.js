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

// Initialize
async function init() {
  currentWindow = getCurrentWindow();
  
  // Load saved content if any
  const savedContent = sessionStorage.getItem('currentNote');
  if (savedContent) {
    editorInput.value = savedContent;
    updatePreview();
    updateWordCount();
  }
  
  // Setup event listeners
  setupEventListeners();
  
  // Listen for new-note event from global hotkey
  listen('new-note', () => {
    newNote();
  });
  
  // Initial render
  updatePreview();
  
  // Focus editor
  editorInput.focus();
}

function setupEventListeners() {
  // Editor input - live preview update
  editorInput.addEventListener('input', () => {
    if (!isUpdating) {
      updatePreview();
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
    }
  });
  
  // Sync scroll positions
  editorInput.addEventListener('scroll', () => {
    editorPreview.scrollTop = editorInput.scrollTop;
  });
  
  // Keyboard shortcuts
  editorInput.addEventListener('keydown', handleKeyDown);
  
  // Format toolbar buttons
  formatBtns.forEach(btn => {
    btn.addEventListener('click', () => {
      const action = btn.dataset.action;
      applyFormat(action);
    });
  });
  
  // Action buttons
  saveBtn.addEventListener('click', saveNote);
  newNoteBtn.addEventListener('click', newNote);
  minimizeBtn.addEventListener('click', () => currentWindow?.minimize());
  closeBtn.addEventListener('click', () => currentWindow?.hide());
  alwaysOnTopBtn.addEventListener('click', toggleAlwaysOnTop);
}

function handleKeyDown(e) {
  const isMac = navigator.platform.includes('Mac');
  const ctrl = isMac ? e.metaKey : e.ctrlKey;
  const shift = e.shiftKey;
  
  // Ctrl+B: Bold
  if (ctrl && e.key === 'b') {
    e.preventDefault();
    applyFormat('bold');
    return;
  }
  
  // Ctrl+I: Italic
  if (ctrl && e.key === 'i') {
    e.preventDefault();
    applyFormat('italic');
    return;
  }
  
  // Ctrl+K: Link
  if (ctrl && e.key === 'k') {
    e.preventDefault();
    applyFormat('link');
    return;
  }
  
  // Ctrl+Shift+C: Code block
  if (ctrl && shift && e.key === 'C') {
    e.preventDefault();
    applyFormat('codeblock');
    return;
  }
  
  // Ctrl+S: Save
  if (ctrl && e.key === 's') {
    e.preventDefault();
    saveNote();
    return;
  }
  
  // Ctrl+N: New note
  if (ctrl && e.key === 'n') {
    e.preventDefault();
    newNote();
    return;
  }
  
  // Escape: Hide to tray
  if (e.key === 'Escape') {
    currentWindow?.hide();
    return;
  }
  
  // Tab key handling for lists/code
  if (e.key === 'Tab') {
    e.preventDefault();
    handleTab(e.shiftKey);
    return;
  }
  
  // Enter key handling for auto-continuation
  if (e.key === 'Enter') {
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
    const [fullMatch, indent, marker] = listMatch;
    
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
      
    case 'heading':
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
      
    case 'quote':
      newText = selectedText.split('\n').map(line => '> ' + line).join('\n');
      cursorOffset = 0;
      break;
      
    case 'list':
      newText = selectedText.split('\n').map(line => '- ' + line).join('\n');
      cursorOffset = 0;
      break;
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

function updatePreview() {
  const content = editorInput.value;
  
  // Configure marked for GitHub-flavored markdown
  marked.setOptions({
    gfm: true,
    breaks: true,
    headerIds: false,
    mangle: false,
    sanitize: false
  });
  
  // Render markdown
  editorPreview.innerHTML = marked.parse(content);
}

function updateWordCount() {
  const text = editorInput.value.trim();
  const count = text ? text.split(/\s+/).length : 0;
  wordCount.textContent = `${count} word${count !== 1 ? 's' : ''}`;
}

function setStatus(message, type = 'normal') {
  statusMessage.textContent = message;
  statusDot.className = 'status-indicator' + (type === 'saving' ? ' saving' : '');
  statusDot.style.background = type === 'saved' ? 'var(--status-success)' : 
                               type === 'error' ? 'var(--accent-red)' : 
                               'var(--accent-blue)';
}

async function toggleAlwaysOnTop() {
  if (!currentWindow) return;
  
  const isAlwaysOnTop = await currentWindow.isAlwaysOnTop();
  await currentWindow.setAlwaysOnTop(!isAlwaysOnTop);
  alwaysOnTopBtn.classList.toggle('active', !isAlwaysOnTop);
}

async function autoSave() {
  const content = editorInput.value.trim();
  if (!content) return;
  
  try {
    setStatus('Saving...', 'saving');
    await invoke('save_note', { content });
    setStatus('Saved', 'saved');
    sessionStorage.removeItem('currentNote');
    
    setTimeout(() => {
      setStatus('Ready');
    }, 2000);
  } catch (error) {
    console.error('Auto-save failed:', error);
    setStatus('Save failed', 'error');
  }
}

async function saveNote() {
  const content = editorInput.value.trim();
  if (!content) {
    setStatus('Nothing to save', 'error');
    setTimeout(() => setStatus('Ready'), 2000);
    return;
  }
  
  try {
    setStatus('Saving...', 'saving');
    const filepath = await invoke('save_note', { content });
    setStatus('Saved to notes folder', 'saved');
    sessionStorage.removeItem('currentNote');
    
    setTimeout(() => setStatus('Ready'), 3000);
  } catch (error) {
    console.error('Save failed:', error);
    setStatus('Save failed: ' + error, 'error');
  }
}

function newNote() {
  editorInput.value = '';
  sessionStorage.removeItem('currentNote');
  updatePreview();
  updateWordCount();
  setStatus('New note', 'normal');
  editorInput.focus();
  
  setTimeout(() => setStatus('Ready'), 2000);
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', init);
