-- TextTop Supabase schema
-- Run this entire script in the Supabase SQL Editor for the project that both
-- the WPF desktop app and React web app will use.

-- gen_random_uuid() is provided by pgcrypto. Supabase usually has this
-- extension available, but this statement makes the schema self-contained.
create extension if not exists "pgcrypto";

-- The memos table stores the visible memo content plus desktop window metadata.
-- owner_id points at auth.users so each memo belongs to one authenticated user.
create table if not exists public.memos (
  id uuid primary key default gen_random_uuid(),
  owner_id uuid not null references auth.users(id) on delete cascade,
  title text not null default 'Memo',
  content text not null default '',
  is_topmost boolean not null default true,
  left_pos double precision not null default 100,
  top_pos double precision not null default 100,
  width double precision not null default 260,
  height double precision not null default 380,
  is_open boolean not null default true,
  version integer not null default 1,
  is_deleted boolean not null default false,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);

-- Helpful indexes for the common list query:
-- "my non-deleted memos, newest updated first".
create index if not exists memos_owner_deleted_updated_idx
  on public.memos (owner_id, is_deleted, updated_at desc);

-- Keep updated_at current whenever a row changes. Clients do not need to trust
-- their local clock for the final server timestamp.
create or replace function public.set_updated_at()
returns trigger
language plpgsql
as $$
begin
  new.updated_at = now();
  return new;
end;
$$;

drop trigger if exists set_memos_updated_at on public.memos;

create trigger set_memos_updated_at
before update on public.memos
for each row
execute function public.set_updated_at();

-- Row Level Security is mandatory because WPF and Web use only the public
-- anon/publishable key. The policies below restrict authenticated users to
-- rows where owner_id matches auth.uid().
alter table public.memos enable row level security;

drop policy if exists "Users can select their own memos" on public.memos;
create policy "Users can select their own memos"
on public.memos
for select
to authenticated
using (owner_id = auth.uid());

drop policy if exists "Users can insert their own memos" on public.memos;
create policy "Users can insert their own memos"
on public.memos
for insert
to authenticated
with check (owner_id = auth.uid());

drop policy if exists "Users can update their own memos" on public.memos;
create policy "Users can update their own memos"
on public.memos
for update
to authenticated
using (owner_id = auth.uid())
with check (owner_id = auth.uid());

-- No delete policy is created on purpose. TextTop performs soft delete by
-- setting is_deleted = true with the same version check used for normal saves.
