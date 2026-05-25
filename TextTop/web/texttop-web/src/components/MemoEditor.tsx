import { useRef } from 'react';
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
  const textareaRef = useRef<HTMLTextAreaElement | null>(null);

  if (!memo) {
    return <section className="editor empty">메모를 선택하거나 새 메모를 만드세요.</section>;
  }

  const toggleStrikethrough = () => {
    const textarea = textareaRef.current;
    if (!textarea) {
      return;
    }

    const start = textarea.selectionStart;
    const end = textarea.selectionEnd;
    const value = memo.content;
    const selected = value.slice(start, end);
    const before = value.slice(Math.max(0, start - 2), start);
    const after = value.slice(end, end + 2);

    let nextValue = value;
    let cursorPosition = end;

    if (selected && before === '~~' && after === '~~') {
      nextValue = value.slice(0, start - 2) + selected + value.slice(end + 2);
      cursorPosition = end - 2;
    } else if (selected) {
      nextValue = value.slice(0, start) + `~~${selected}~~` + value.slice(end);
      cursorPosition = end + 4;
    } else {
      nextValue = value.slice(0, start) + '~~~~' + value.slice(end);
      cursorPosition = start + 2;
    }

    onChange({ ...memo, content: nextValue, status: 'editing' });

    requestAnimationFrame(() => {
      if (textareaRef.current) {
        textareaRef.current.selectionStart = cursorPosition;
        textareaRef.current.selectionEnd = cursorPosition;
        textareaRef.current.focus();
      }
    });
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
        <button className="secondary" onClick={toggleStrikethrough}>
          Strike
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
        <span>{message}</span>
      </div>
      <textarea
        ref={textareaRef}
        value={memo.content}
        onChange={(event) => onChange({ ...memo, content: event.target.value, status: 'editing' })}
      />
    </section>
  );
}
