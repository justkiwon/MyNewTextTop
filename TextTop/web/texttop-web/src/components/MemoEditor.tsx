import { useEffect, useRef, type KeyboardEvent } from 'react';
import type { MemoDraft } from '../models/memo.ts';
import { StatusBadge } from './StatusBadge.tsx';

interface MemoEditorProps {
  memo?: MemoDraft;
  message: string;
  onChange: (memo: MemoDraft) => void;
  onSave: () => void;
  onDelete: () => void;
}

export function MemoEditor({ memo, message, onChange, onSave, onDelete }: MemoEditorProps) {
  const editorRef = useRef<HTMLDivElement | null>(null);
  const selectionRef = useRef<Range | null>(null);

  useEffect(() => {
    if (editorRef.current && memo) {
      editorRef.current.innerHTML = toEditableHtml(memo.content);
    }
  }, [memo?.id]);

  if (!memo) {
    return <section className="editor empty">메모를 선택하거나 새 메모를 만드세요.</section>;
  }

  const toggleStrikethrough = () => {
    const editor = editorRef.current;
    if (!editor) {
      return;
    }

    editor.focus();
    restoreEditorSelection();
    document.execCommand('strikeThrough');
    onChange({ ...memo, content: editor.innerHTML, status: 'editing' });
  };

  const applyFontSize = (fontSize: string) => {
    const editor = editorRef.current;
    if (!editor) {
      return;
    }

    editor.focus();
    restoreEditorSelection();
    document.execCommand('fontSize', false, '7');
    normalizeFontTags(editor, fontSize);
    saveEditorSelection();
    onChange({ ...memo, content: editor.innerHTML, status: 'editing' });
  };

  const saveEditorSelection = () => {
    const editor = editorRef.current;
    const selection = window.getSelection();
    if (!editor || !selection || selection.rangeCount === 0) {
      return;
    }

    const range = selection.getRangeAt(0);
    if (editor.contains(range.commonAncestorContainer)) {
      selectionRef.current = range.cloneRange();
    }
  };

  const restoreEditorSelection = () => {
    const selection = window.getSelection();
    if (!selection || !selectionRef.current) {
      return;
    }

    selection.removeAllRanges();
    selection.addRange(selectionRef.current);
  };

  const handleEditorInput = () => {
    const editor = editorRef.current;
    if (!editor) {
      return;
    }

    saveEditorSelection();
    onChange({ ...memo, content: editor.innerHTML, status: 'editing' });
  };

  const handleContentKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    if (event.ctrlKey && event.shiftKey && event.key.toLowerCase() === 'x') {
      event.preventDefault();
      toggleStrikethrough();
    }
  };

  return (
    <section className="editor">
      <div className="editor-toolbar">
        <input
          className="title-input"
          value={memo.title}
          onChange={(event) => onChange({ ...memo, title: event.target.value, status: 'editing' })}
        />
        <label className="topmost-check">
          <input
            type="checkbox"
            checked={memo.is_topmost}
            onChange={(event) => onChange({ ...memo, is_topmost: event.target.checked, status: 'editing' })}
          />
          Topmost
        </label>
        <button
          className="secondary format-button"
          type="button"
          onClick={toggleStrikethrough}
          title="Strikethrough (Ctrl+Shift+X)"
          aria-label="Strikethrough"
        >
          <span className="strike-icon">S</span>
        </button>
        <button className="save-button" onClick={onSave}>
          SAVE
        </button>
        <button className="danger" onClick={onDelete} disabled={memo.isLocalOnly}>
          Delete
        </button>
      </div>
      <div className="editor-status">
        <StatusBadge status={memo.status} />
        <label className="font-size-control">
          <span>FontSize</span>
          <select
            defaultValue="16"
            onMouseDown={saveEditorSelection}
            onFocus={saveEditorSelection}
            onChange={(event) => applyFontSize(event.target.value)}
          >
            <option value="12px">12</option>
            <option value="14px">14</option>
            <option value="16px">16</option>
            <option value="18px">18</option>
            <option value="20px">20</option>
            <option value="24px">24</option>
            <option value="32px">32</option>
          </select>
        </label>
        <span>{message}</span>
      </div>
      <div
        ref={editorRef}
        className="content-editor"
        contentEditable
        role="textbox"
        aria-multiline="true"
        suppressContentEditableWarning
        onKeyDown={handleContentKeyDown}
        onKeyUp={saveEditorSelection}
        onMouseUp={saveEditorSelection}
        onInput={handleEditorInput}
      />
    </section>
  );
}

function normalizeFontTags(editor: HTMLDivElement, fontSize: string) {
  editor.querySelectorAll('font[size="7"]').forEach((fontTag) => {
    const span = document.createElement('span');
    span.style.fontSize = fontSize;
    span.innerHTML = fontTag.innerHTML;
    fontTag.replaceWith(span);
  });
}

function toEditableHtml(content: string) {
  if (/<\/?[a-z][\s\S]*>/i.test(content)) {
    return content;
  }

  return escapeHtml(content)
    .replace(/~~(.+?)~~/g, '<s>$1</s>')
    .replace(/\r?\n/g, '<br>');
}

function escapeHtml(value: string) {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');
}
