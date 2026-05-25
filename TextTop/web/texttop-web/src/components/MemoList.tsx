import type { MemoDraft } from '../models/memo.ts';

interface MemoListProps {
  memos: MemoDraft[];
  selectedId?: string;
  onSelect: (memo: MemoDraft) => void;
  onNew: () => void;
}

export function MemoList({ memos, selectedId, onSelect, onNew }: MemoListProps) {
  return (
    <aside className="memo-list">
      <button className="new-button" onClick={onNew}>
        New Memo
      </button>
      <div className="memo-list-scroll">
        {memos.map((memo) => (
          <button
            key={memo.id}
            className={`memo-card ${memo.id === selectedId ? 'selected' : ''}`}
            onClick={() => onSelect(memo)}
          >
            <strong>{memo.title || 'Untitled'}</strong>
            <span>{new Date(memo.updated_at).toLocaleString()}</span>
            <small>version {memo.version}</small>
          </button>
        ))}
      </div>
    </aside>
  );
}
