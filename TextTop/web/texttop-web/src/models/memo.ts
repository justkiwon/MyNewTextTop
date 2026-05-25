export type MemoStatus = 'synced' | 'editing' | 'saved' | 'conflict' | 'offline';

export interface MemoRow {
  id: string;
  owner_id: string;
  title: string;
  content: string;
  is_topmost: boolean;
  left_pos: number;
  top_pos: number;
  width: number;
  height: number;
  is_open: boolean;
  version: number;
  is_deleted: boolean;
  created_at: string;
  updated_at: string;
}

export interface MemoDraft extends MemoRow {
  baseVersion: number;
  isLocalOnly: boolean;
  status: MemoStatus;
}

export function createLocalMemo(ownerId: string): MemoDraft {
  const now = new Date().toISOString();
  return {
    id: crypto.randomUUID(),
    owner_id: ownerId,
    title: 'Untitled',
    content: '',
    is_topmost: true,
    left_pos: 100,
    top_pos: 100,
    width: 260,
    height: 380,
    is_open: true,
    version: 1,
    baseVersion: 1,
    is_deleted: false,
    isLocalOnly: true,
    status: 'editing',
    created_at: now,
    updated_at: now,
  };
}

export function rowToDraft(row: MemoRow): MemoDraft {
  return {
    ...row,
    baseVersion: row.version,
    isLocalOnly: false,
    status: 'synced',
  };
}
