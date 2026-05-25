import { Session, User } from '@supabase/supabase-js';
import { useEffect, useMemo, useState } from 'react';
import { LoginForm } from './components/LoginForm.tsx';
import { MemoEditor } from './components/MemoEditor.tsx';
import { MemoList } from './components/MemoList.tsx';
import { TopBar } from './components/TopBar.tsx';
import { isSupabaseConfigured, supabase } from './lib/supabaseClient.ts';
import { createLocalMemo, type MemoDraft } from './models/memo.ts';
import { insertMemo, loadMemos, softDeleteMemo, updateMemo } from './services/memoService.ts';

const draftKey = 'texttop-web-draft';

export default function App() {
  const [session, setSession] = useState<Session | null>(null);
  const [memos, setMemos] = useState<MemoDraft[]>([]);
  const [selectedId, setSelectedId] = useState<string>();
  const [message, setMessage] = useState('');
  const [online, setOnline] = useState(navigator.onLine);

  const user = session?.user;
  const selectedMemo = useMemo(() => memos.find((memo) => memo.id === selectedId), [memos, selectedId]);

  useEffect(() => {
    void supabase.auth.getSession().then(({ data }) => setSession(data.session));
    const { data } = supabase.auth.onAuthStateChange((_event, nextSession) => setSession(nextSession));
    return () => data.subscription.unsubscribe();
  }, []);

  useEffect(() => {
    const update = () => setOnline(navigator.onLine);
    window.addEventListener('online', update);
    window.addEventListener('offline', update);
    return () => {
      window.removeEventListener('online', update);
      window.removeEventListener('offline', update);
    };
  }, []);

  useEffect(() => {
    if (!user) {
      setMemos([]);
      setSelectedId(undefined);
      return;
    }

    void refreshMemos();
  }, [user?.id]);

  useEffect(() => {
    if (selectedMemo) {
      localStorage.setItem(draftKey, JSON.stringify(selectedMemo));
    }
  }, [selectedMemo]);

  async function refreshMemos() {
    try {
      const loaded = await loadMemos();
      setMemos(loaded);
      setSelectedId((current) => current ?? loaded[0]?.id);
      setMessage('Loaded');
    } catch (error) {
      setMessage(error instanceof Error ? error.message : '메모를 불러오지 못했습니다.');
    }
  }

  function updateSelected(next: MemoDraft) {
    setMemos((current) => current.map((memo) => (memo.id === next.id ? next : memo)));
  }

  function createMemo() {
    if (!user) {
      return;
    }

    const memo = createLocalMemo(user.id);
    setMemos((current) => [memo, ...current]);
    setSelectedId(memo.id);
    setMessage('새 메모는 SAVE를 눌러야 서버에 저장됩니다.');
  }

  async function saveSelected() {
    if (!selectedMemo) {
      return;
    }

    if (!online) {
      localStorage.setItem(draftKey, JSON.stringify(selectedMemo));
      updateSelected({ ...selectedMemo, status: 'offline' });
      setMessage('현재 오프라인입니다. 웹에서는 온라인 상태에서만 저장됩니다. 현재 내용은 브라우저 임시 draft에 보관됩니다.');
      return;
    }

    const result = selectedMemo.isLocalOnly ? await insertMemo(selectedMemo) : await updateMemo(selectedMemo);
    if (result.ok && result.memo) {
      setMemos((current) => [result.memo!, ...current.filter((memo) => memo.id !== selectedMemo.id)]);
      setSelectedId(result.memo.id);
      setMessage('Saved');
      return;
    }

    const status = result.conflict ? 'conflict' : 'editing';
    updateSelected({ ...selectedMemo, status });
    setMessage(result.message ?? '저장하지 못했습니다.');
  }

  async function deleteSelected() {
    if (!selectedMemo || selectedMemo.isLocalOnly) {
      return;
    }

    const result = await softDeleteMemo(selectedMemo);
    if (result.ok) {
      setMemos((current) => current.filter((memo) => memo.id !== selectedMemo.id));
      setSelectedId(undefined);
      setMessage('Deleted');
      return;
    }

    updateSelected({ ...selectedMemo, status: result.conflict ? 'conflict' : selectedMemo.status });
    setMessage(result.message ?? '삭제하지 못했습니다.');
  }

  if (!user) {
    return <LoginForm configured={isSupabaseConfigured} />;
  }

  return (
    <div className="app-shell">
      <TopBar user={user as User} online={online} />
      <main className="workspace">
        <MemoList memos={memos} selectedId={selectedId} onSelect={(memo) => setSelectedId(memo.id)} onNew={createMemo} />
        <MemoEditor memo={selectedMemo} message={message} onChange={updateSelected} onSave={saveSelected} onDelete={deleteSelected} />
      </main>
    </div>
  );
}
