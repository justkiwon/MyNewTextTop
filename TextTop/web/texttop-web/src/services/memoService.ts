import { supabase } from '../lib/supabaseClient.ts';
import { type MemoDraft, type MemoRow, rowToDraft } from '../models/memo.ts';

export interface SaveOutcome {
  ok: boolean;
  conflict?: boolean;
  message?: string;
  memo?: MemoDraft;
}

export async function loadMemos(): Promise<MemoDraft[]> {
  const { data, error } = await supabase
    .from('memos')
    .select('*')
    .eq('is_deleted', false)
    .order('updated_at', { ascending: false });

  if (error) {
    throw error;
  }

  return ((data ?? []) as MemoRow[]).map(rowToDraft);
}

export async function insertMemo(memo: MemoDraft): Promise<SaveOutcome> {
  const { id: _localId, baseVersion: _base, isLocalOnly: _local, status: _status, ...row } = memo;

  const { data, error } = await supabase
    .from('memos')
    .insert({
      ...row,
      title: row.title.trim() || 'Untitled',
      version: 1,
      is_deleted: false,
    })
    .select()
    .single();

  if (error) {
    return { ok: false, message: error.message };
  }

  return { ok: true, memo: rowToDraft(data as MemoRow) };
}

export async function updateMemo(memo: MemoDraft): Promise<SaveOutcome> {
  const { data, error } = await supabase
    .from('memos')
    .update({
      title: memo.title.trim() || 'Untitled',
      content: memo.content,
      is_topmost: memo.is_topmost,
      left_pos: memo.left_pos,
      top_pos: memo.top_pos,
      width: memo.width,
      height: memo.height,
      is_open: true,
      version: memo.baseVersion + 1,
    })
    .eq('id', memo.id)
    .eq('version', memo.baseVersion)
    .select();

  if (error) {
    return { ok: false, message: error.message };
  }

  if (!data || data.length === 0) {
    return {
      ok: false,
      conflict: true,
      message: '다른 곳에서 먼저 수정된 메모입니다. 최신 내용을 다시 불러오세요.',
    };
  }

  return { ok: true, memo: rowToDraft(data[0] as MemoRow) };
}

export async function softDeleteMemo(memo: MemoDraft): Promise<SaveOutcome> {
  const { data, error } = await supabase
    .from('memos')
    .update({
      is_deleted: true,
      version: memo.baseVersion + 1,
    })
    .eq('id', memo.id)
    .eq('version', memo.baseVersion)
    .select();

  if (error) {
    return { ok: false, message: error.message };
  }

  if (!data || data.length === 0) {
    return {
      ok: false,
      conflict: true,
      message: '삭제하려는 메모가 다른 곳에서 먼저 수정되었습니다. 최신 내용을 다시 불러오세요.',
    };
  }

  return { ok: true };
}
