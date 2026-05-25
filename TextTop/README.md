# TextTop

TextTop is a small cloud memo project with two clients sharing one Supabase database:

- `desktop/TextTop.Desktop`: .NET 8 WPF app with AppData JSON offline cache.
- `web/texttop-web`: React + TypeScript + Vite app using `@supabase/supabase-js`.
- `supabase`: SQL schema and setup notes.

Both clients use Supabase Auth email/password login, the same `public.memos` table, and the public anon/publishable key only. Supabase Row Level Security keeps users isolated by `owner_id = auth.uid()`.

## Structure

```text
TextTop
├─ supabase
├─ desktop/TextTop.Desktop
└─ web/texttop-web
```

## Supabase Setup

1. Create a Supabase project.
2. Enable Authentication > Email provider.
3. Create or invite a user account.
4. Run `supabase/supabase_schema.sql` in SQL Editor.
5. Copy Project URL and anon/publishable key from Project Settings > API.
6. Read `supabase/README_SUPABASE_SETUP.md` for details.

Never place a secret/service_role key in WPF or the browser. Those keys bypass RLS and would expose all data if copied from the app.

## WPF Config

Copy `desktop/TextTop.Desktop/appsettings.example.json` to one of these locations:

1. `%AppData%\TextTop\config.json`
2. executable folder `appsettings.json`

Example:

```json
{
  "supabaseUrl": "https://YOUR_PROJECT_REF.supabase.co",
  "supabaseAnonKey": "YOUR_SUPABASE_ANON_OR_PUBLISHABLE_KEY"
}
```

Run:

```powershell
cd desktop/TextTop.Desktop
dotnet restore
dotnet build
dotnet run
```

Publish for a PC with .NET runtime:

```powershell
dotnet publish -c Release -r win-x64 --self-contained false
```

Publish for a PC without .NET runtime:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true
```

WPF stores local files under `%AppData%\TextTop`: `config.json`, `AuthToken.json`, and `MemosCache.json`. Offline saves become `PendingInsert` or `PendingUpdate`; conflicts become `Conflict`. Text input, title edits, window moves, size changes, and Topmost toggles do not auto-save. Only `SAVE` and window close save.

## Web Config

Create `web/texttop-web/.env` from `.env.example`:

```env
VITE_SUPABASE_URL=https://YOUR_PROJECT_REF.supabase.co
VITE_SUPABASE_ANON_KEY=YOUR_SUPABASE_ANON_OR_PUBLISHABLE_KEY
```

Run:

```powershell
cd web/texttop-web
npm install
npm run dev
```

Build:

```powershell
npm run build
```

Deploy to Vercel or Cloudflare Pages by setting the same two environment variables and using `npm run build`. The output folder is `dist`.

## Conflict Control

Each memo has `version`. When a client loads a memo, it remembers `baseVersion`. Saving an existing memo updates with:

```text
id = memo.id AND version = baseVersion
```

The client writes `version = baseVersion + 1`. If Supabase returns zero rows, another client saved first. TextTop marks the memo as conflict and does not overwrite.

## Implemented

- Email/password login in WPF and Web.
- Shared Supabase `memos` table with RLS.
- Manual save only.
- WPF multiple memo windows, Topmost, position/size persistence.
- WPF offline JSON cache and pending sync on startup.
- Web list/editor layout, soft delete, offline draft in `localStorage`.
- Optimistic concurrency in both clients.

## Future Improvements

- Token refresh before expiry in WPF.
- Manual sync button and richer conflict resolution UI.
- Encrypted token storage with Windows DPAPI or Credential Manager.
- Optional setting for saving closed windows as `is_open = false`.
