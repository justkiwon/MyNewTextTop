# Supabase Setup

1. Create a Supabase project at https://supabase.com and open the project dashboard.
2. In Authentication > Providers, enable Email. For quick testing you can disable email confirmation, or keep it enabled and confirm the user before login.
3. Create a test account in Authentication > Users, or sign up through your own flow and confirm the email.
4. Open SQL Editor, paste `supabase_schema.sql`, and run it once.
5. Open Project Settings > API and copy the Project URL.
6. Copy the anon/publishable key only. Do not use the service_role or secret key in WPF or React.

The service_role key bypasses Row Level Security and can read or write every user's data. A desktop app or browser bundle can be inspected by users, so a secret key placed there is effectively public. TextTop clients use anon/publishable keys and Supabase Auth tokens; RLS then allows each user to access only rows where `owner_id = auth.uid()`.

TextTop prevents silent overwrites with `version`. When a memo is loaded, the app remembers that value as `baseVersion`. Saving runs an update with `id = memo.id AND version = baseVersion`, then writes `version = baseVersion + 1`. If no row is returned, another client saved first and TextTop shows a conflict instead of overwriting.

WPF config example at `%AppData%\TextTop\config.json`:

```json
{
  "supabaseUrl": "https://YOUR_PROJECT_REF.supabase.co",
  "supabaseAnonKey": "YOUR_SUPABASE_ANON_OR_PUBLISHABLE_KEY"
}
```

React config example at `web/texttop-web/.env`:

```env
VITE_SUPABASE_URL=https://YOUR_PROJECT_REF.supabase.co
VITE_SUPABASE_ANON_KEY=YOUR_SUPABASE_ANON_OR_PUBLISHABLE_KEY
```

Connection test:

1. Run the SQL schema.
2. Put the same URL and anon key into WPF and Web config.
3. Log in with the same Supabase email account in both clients.
4. Create a memo in WPF and press `SAVE`.
5. Refresh the web app; the memo should appear because both clients read `public.memos`.

The WPF app also keeps `%AppData%\TextTop\MemosCache.json`. If Supabase is unreachable, WPF writes pending changes there and retries on the next startup. The web app is online-first; while offline it stores the current draft in `localStorage` and asks the user to save again after reconnecting.
