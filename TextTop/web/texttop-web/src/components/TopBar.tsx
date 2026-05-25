import { User } from '@supabase/supabase-js';
import { supabase } from '../lib/supabaseClient.ts';
import { StatusBadge } from './StatusBadge.tsx';

interface TopBarProps {
  user: User;
  online: boolean;
}

export function TopBar({ user, online }: TopBarProps) {
  return (
    <header className="topbar">
      <div>
        <strong>TextTop</strong>
        <span>{user.email}</span>
      </div>
      <nav>
        <StatusBadge status={online ? 'online' : 'offline'} />
        <button className="secondary" onClick={() => void supabase.auth.signOut()}>
          Logout
        </button>
      </nav>
    </header>
  );
}
