import { FormEvent, useState } from 'react';
import { supabase } from '../lib/supabaseClient.ts';

interface LoginFormProps {
  configured: boolean;
}

export function LoginForm({ configured }: LoginFormProps) {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [message, setMessage] = useState(
    configured ? 'Supabase 계정으로 로그인하세요.' : '.env에 Supabase URL과 anon key를 설정하세요.',
  );

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (!configured) {
      setMessage('VITE_SUPABASE_URL과 VITE_SUPABASE_ANON_KEY가 필요합니다.');
      return;
    }

    const { error } = await supabase.auth.signInWithPassword({ email, password });
    setMessage(error ? error.message : '로그인 성공');
  }

  return (
    <main className="login-page">
      <form className="login-card" onSubmit={submit}>
        <h1>TextTop</h1>
        <p>Cloud Memo</p>
        <label>
          Email
          <input value={email} onChange={(event) => setEmail(event.target.value)} type="email" />
        </label>
        <label>
          Password
          <input value={password} onChange={(event) => setPassword(event.target.value)} type="password" />
        </label>
        <button type="submit">Login</button>
        <span className="login-message">{message}</span>
      </form>
    </main>
  );
}
